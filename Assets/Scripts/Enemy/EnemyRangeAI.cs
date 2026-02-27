using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public sealed class EnemyAI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform _firePoint;
    [SerializeField] private Animator _animator;

    [Header("AI Sleep")]
    [SerializeField] private float _sleepDistance = 30f;
    [SerializeField] private float _sleepCheckInterval = 0.5f;

    [Header("AI")]
    [SerializeField] private float _thinkInterval = 0.15f;
    [SerializeField] private float _repathInterval = 0.25f;

    [Header("Distances")]
    [SerializeField] private float _lookRadius = 15f;
    [SerializeField] private float _shootingDistance = 10f;

    [Header("Rotation")]
    [SerializeField] private float _lookSlerpSpeed = 10f;

    [Header("Shooting")]
    [SerializeField] private float _fireRate = 10f;
    [SerializeField] private float _damage = 1f;
    [SerializeField] private float _windupTime = 0.35f;

    [Header("Trail")]
    [SerializeField] private float _trailSpeed = 60f;
    [SerializeField] private float _trailDistance = 50f;

    [Header("Line of sight / muzzle fix")]
    [SerializeField] private float _muzzleCheckRadius = 0.12f; // 0.08Ц0.15 норм
    [SerializeField] private float _muzzlePushOut = 0.25f;
    [SerializeField] private float _muzzleSkin = 0.02f;
    [SerializeField] private LayerMask _obstacleMask; // стены/преп€тстви€ (Ќ≈ Player)

    private Transform _target;
    private NavMeshAgent _agent;
    private EnemySFX _sfx;
    private float _nextRetargetTime;


    private float _nextThink;
    private float _nextRepath;

    private float _nextSleepCheck;
    private bool _sleeping;

    private bool _isWindingUp;
    private float _nextFireTime;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _sfx = GetComponent<EnemySFX>();

        if (_animator == null)
        {
            _animator = GetComponent<Animator>();
        }

        if (_agent != null)
        {
            _agent.updateRotation = false;
        }
    }

    private void OnEnable()
    {
        CacheTarget();

        _sleeping = false;
        _nextSleepCheck = 0f;

        _nextThink = 0f;
        _nextRepath = 0f;

        _nextFireTime = 0f;
        _isWindingUp = false;

        if (_animator != null)
        {
            _animator.enabled = true;
            _animator.Rebind();
            _animator.Update(0f);
            _animator.SetBool("isRunning", false);
            _animator.SetBool("isShooting", false);
        }

        if (_agent != null && !_agent.enabled)
        {
            _agent.enabled = true;
        }

        // ¬ј∆Ќќ: здесь не ResetPath (агент может быть ещЄ не на NavMesh)
    }

    public void OnSpawnedOnNavMesh()
    {
        TryResetAgentOnNavMesh();
    }

    private void Update()
    {
        if (_target == null && Time.time >= _nextRetargetTime)
        {
            _nextRetargetTime = Time.time + 1f;
            CacheTarget();
        }

        if (_target == null) return;

        if (_target != null && !_sleeping)
        {
            LookTarget();
        }
        if (Time.time < _nextThink)
        {
            return;
        }

        _nextThink = Time.time + _thinkInterval;

        if (_target == null || _agent == null || _firePoint == null)
        {
            return;
        }

        float distance = Vector3.Distance(_target.position, transform.position);

        UpdateSleep(distance);
        if (_sleeping)
        {
            return;
        }

        if (!_agent.isActiveAndEnabled || !_agent.isOnNavMesh)
        {
            return;
        }

        if (distance > _lookRadius)
        {
            SetRunning(false);
            SetShooting(false);

            _agent.isStopped = true;
            return;
        }



        bool inShootDistance = distance <= _shootingDistance;
        bool canSee = inShootDistance && CanSeePlayer();

        // ≈сли не в дистанции »Ћ» не видит игрока Ч бежим
        if (!canSee)
        {
            SetRunning(true);
            SetShooting(false);

            _agent.isStopped = false;
            Chase();

            return;
        }

        // ≈сли упЄрс€ оружием в стену Ч не стрел€ем, а чуть отходим
        if (IsMuzzleInsideObstacle(_firePoint.position))
        {
            SetShooting(false);
            SetRunning(true);

            _agent.isStopped = false;

            Vector3 backOff = transform.position - transform.forward * 0.6f;
            _agent.SetDestination(backOff);

            return;
        }

        // »наче Ч стрел€ем
        _agent.isStopped = true;

        SetRunning(false);
        SetShooting(true);

        if (Time.time > _nextFireTime && !_isWindingUp)
        {
            _nextFireTime = Time.time + (1f / _fireRate);
            StartCoroutine(ShootDodgeable());
        }
    }

    private void UpdateSleep(float distance)
    {
        if (Time.time < _nextSleepCheck)
        {
            return;
        }

        _nextSleepCheck = Time.time + _sleepCheckInterval;

        bool shouldSleep = distance > _sleepDistance;
        if (shouldSleep == _sleeping)
        {
            return;
        }

        _sleeping = shouldSleep;

        if (_animator != null)
        {
            _animator.enabled = !_sleeping;

            if (_sleeping)
            {
                _animator.SetBool("isRunning", false);
                _animator.SetBool("isShooting", false);
            }
        }

        if (_agent != null)
        {
            _agent.enabled = !_sleeping;
        }
    }

    private void Chase()
    {
        if (Time.time < _nextRepath)
        {
            return;
        }

        _nextRepath = Time.time + _repathInterval;

        if (_agent.isActiveAndEnabled && _agent.isOnNavMesh)
        {
            _agent.SetDestination(_target.position);
        }
    }

    private IEnumerator ShootDodgeable()
    {
        _isWindingUp = true;

        Vector3 aimPoint = _target.position;
        Vector3 desiredDir = (aimPoint - _firePoint.position).normalized;

        if (IsMuzzleInsideObstacle(_firePoint.position))
        {
            _isWindingUp = false;
            yield break;
        }

        Vector3 dir = desiredDir;

        yield return new WaitForSeconds(_windupTime);

        if (IsMuzzleInsideObstacle(_firePoint.position))
        {
            _isWindingUp = false;
            yield break;
        }

        Vector3 muzzle = GetSafeMuzzlePos(dir);

        RaycastHit hit;
        Vector3 endPoint = muzzle + dir * _trailDistance;

        if (Physics.Raycast(muzzle, dir, out hit, _trailDistance, ~0, QueryTriggerInteraction.Ignore))
        {
            endPoint = hit.point;
            _sfx?.PlayShoot();

            PlayerHealth ph = hit.transform.GetComponentInParent<PlayerHealth>();
            if (ph != null)
            {
                ph.TakeDamage(_damage);
            }
        }

        if (FastTrailPool.I != null)
        {
            FastTrailPool.I.Spawn(muzzle, endPoint, _trailSpeed);
        }

        _isWindingUp = false;
    }

    private bool CanSeePlayer()
    {
        if (_target == null || _firePoint == null)
        {
            return false;
        }

        Vector3 desiredDir = (_target.position - _firePoint.position).normalized;

        if (IsMuzzleInsideObstacle(_firePoint.position))
        {
            return false;
        }

        Vector3 origin = GetSafeMuzzlePos(desiredDir);
        Vector3 targetPos = _target.position;

        Vector3 dir = (targetPos - origin).normalized;
        float dist = Vector3.Distance(origin, targetPos);

        if (Physics.Raycast(origin, dir, out RaycastHit hit, dist, ~0, QueryTriggerInteraction.Ignore))
        {
            return hit.transform.CompareTag("Player");
        }

        return false;
    }

    private bool IsMuzzleInsideObstacle(Vector3 pos)
    {
        return Physics.CheckSphere(pos, _muzzleCheckRadius, _obstacleMask, QueryTriggerInteraction.Ignore);
    }

    private Vector3 GetSafeMuzzlePos(Vector3 desiredDir)
    {
        Vector3 p = _firePoint.position;

        if (!IsMuzzleInsideObstacle(p))
        {
            return p;
        }

        Vector3 pushed = p + desiredDir * _muzzlePushOut;

        if (IsMuzzleInsideObstacle(pushed))
        {
            pushed = p - desiredDir * _muzzlePushOut;
        }

        return pushed + desiredDir * _muzzleSkin;
    }

    private void LookTarget()
    {
        if (_target == null)
        {
            return;
        }

        Vector3 direction = _target.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
        {
            return;
        }

        Quaternion lookRotation = Quaternion.LookRotation(direction.normalized);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, _lookSlerpSpeed * Time.deltaTime);
    }

    private void CacheTarget()
    {
        if (PlayerManager.instance != null && PlayerManager.instance.player != null)
        {
            _target = PlayerManager.instance.player.transform;
            return;
        }

        GameObject p = GameObject.FindGameObjectWithTag("PlayerGoal");
        if (p != null)
        {
            _target = p.transform;
            return;
        }

        _target = null;
    }

    private void TryResetAgentOnNavMesh()
    {
        if (_agent != null && _agent.isActiveAndEnabled && _agent.isOnNavMesh)
        {
            _agent.isStopped = false;
            _agent.ResetPath();
            _agent.velocity = Vector3.zero;
        }
    }

    private void SetRunning(bool value)
    {
        if (_animator != null)
        {
            _animator.SetBool("isRunning", value);
        }
    }

    private void SetShooting(bool value)
    {
        if (_animator != null)
        {
            _animator.SetBool("isShooting", value);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _lookRadius);
    }
}