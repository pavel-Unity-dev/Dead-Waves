using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public sealed class EnemyMeleeAI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator _animator;
    [SerializeField] private EnemySFX _sfx;

    [Header("Distances")]
    [SerializeField] private float _lookRadius = 15f;
    [SerializeField] private float _attackDistance = 2f;

    [Header("Attack")]
    [SerializeField] private float _damage = 1f;
    [SerializeField] private float _attackRate = 1f;

    [Header("AI Timers")]
    [SerializeField] private float _thinkInterval = 0.15f;
    [SerializeField] private float _repathInterval = 0.25f;

    [Header("Flank")]
    [Range(0f, 1f)]
    [SerializeField] private float _flankChance = 0.6f;
    [SerializeField] private float _flankOffset = 2.5f;
    [SerializeField] private float _flankMinDistance = 4.5f;

    [Header("Rage (Speed Boost On Hit)")]
    [SerializeField] private float _rageSpeedMultiplier = 1.5f;
    [SerializeField] private float _rageDuration = 1.2f;

    private Transform _target;
    private NavMeshAgent _agent;

    private float _nextThink;
    private float _nextRepath;

    private float _nextAttackTime;
    private bool _isAttacking;

    // Чтобы враг не “ехал” во время удара
    private Vector3 _attackLockPos;

    // Flank
    private bool _flankSideRight;

    // Rage
    private float _baseSpeed;
    private Coroutine _rageRoutine;

    private const float LookSlerpSpeed = 10f;
    private const float GroundLockYVelocity = -2f;
    private const float AttackHitExtraRange = 0.5f;
    private const float FlankSampleRadius = 2f;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();

        if (_animator == null)
        {
            _animator = GetComponent<Animator>();
        }

        if (_sfx == null)
        {
            _sfx = GetComponent<EnemySFX>();
        }

        _agent.updateRotation = false;
        _baseSpeed = _agent.speed;
    }

    private void OnEnable()
    {
        CacheTarget();

        _isAttacking = false;
        _nextAttackTime = 0f;

        _nextThink = 0f;
        _nextRepath = 0f;

        _flankSideRight = Random.value > 0.5f;

        StopRageIfNeeded();
        _agent.speed = _baseSpeed;

        if (!_agent.enabled)
        {
            _agent.enabled = true;
        }

        TryResetAgentOnNavMesh();

        if (_animator != null)
        {
            _animator.Rebind();
            _animator.Update(0f);
            _animator.SetBool("isRunning", false);
        }
    }

    private void Update()
    {
        if (Time.time < _nextThink)
        {
            return;
        }

        _nextThink = Time.time + _thinkInterval;

        if (_target == null)
        {
            return;
        }

        float distance = Vector3.Distance(transform.position, _target.position);

        // Во время атаки: стоим на месте и смотрим на игрока
        if (_isAttacking)
        {
            transform.position = _attackLockPos;
            LookTarget();
            return;
        }

        // Вне агро
        if (distance > _lookRadius)
        {
            SetRunning(false);

            if (_agent.isActiveAndEnabled)
            {
                _agent.isStopped = true;
            }

            return;
        }

        LookTarget();

        // Бежим
        if (distance > _attackDistance)
        {
            SetRunning(true);

            if (_agent.isActiveAndEnabled && _agent.isOnNavMesh)
            {
                _agent.isStopped = false;
                Chase(distance);
            }

            return;
        }

        // Атакуем
        SetRunning(false);

        if (Time.time >= _nextAttackTime)
        {
            _nextAttackTime = Time.time + (1f / _attackRate);
            StartAttack();
        }
    }

    public void OnSpawnedOnNavMesh()
    {
        TryResetAgentOnNavMesh();
    }

    private void StartAttack()
    {
        _isAttacking = true;
        _attackLockPos = transform.position;

        if (_agent.isActiveAndEnabled)
        {
            _agent.isStopped = true;
            _agent.velocity = Vector3.zero;
        }

        if (_animator != null)
        {
            _animator.ResetTrigger("attack");
            _animator.SetTrigger("attack");
        }
    }

    private void LookTarget()
    {
        Vector3 direction = (_target.position - transform.position);
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
        {
            return;
        }

        Quaternion lookRotation = Quaternion.LookRotation(direction.normalized);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, LookSlerpSpeed * Time.deltaTime);
    }

    // Animation Event #1 (момент удара)
    public void DealDamage()
    {
        if (!_isAttacking || _target == null)
        {
            return;
        }

        _sfx?.PlayAttack();

        float distance = Vector3.Distance(transform.position, _target.position);
        if (distance > _attackDistance + AttackHitExtraRange)
        {
            return;
        }

        PlayerHealth ph = _target.GetComponentInParent<PlayerHealth>();
        if (ph != null)
        {
            ph.TakeDamage(_damage);
        }
    }

    // Animation Event #2 (конец анимации)
    public void EndAttack()
    {
        _isAttacking = false;

        if (_agent.isActiveAndEnabled)
        {
            _agent.isStopped = false;
        }
    }

    public void OnShotHit()
    {
        if (_rageRoutine != null)
        {
            StopCoroutine(_rageRoutine);
        }

        _rageRoutine = StartCoroutine(RageCoroutine());

        // Иногда меняем сторону фланга после попадания
        if (Random.value < 0.35f)
        {
            _flankSideRight = !_flankSideRight;
        }
    }

    private IEnumerator RageCoroutine()
    {
        _agent.speed = _baseSpeed * _rageSpeedMultiplier;
        yield return new WaitForSeconds(_rageDuration);
        _agent.speed = _baseSpeed;

        _rageRoutine = null;
    }

    private void Chase(float distanceToPlayer)
    {
        if (Time.time < _nextRepath)
        {
            return;
        }

        _nextRepath = Time.time + _repathInterval;

        if (!_agent.isActiveAndEnabled || !_agent.isOnNavMesh)
        {
            return;
        }

        Vector3 destination = _target.position;

        bool canFlank = distanceToPlayer >= _flankMinDistance;
        if (canFlank && Random.value < _flankChance)
        {
            destination = GetFlankDestination();
        }

        _agent.SetDestination(destination);
    }

    private Vector3 GetFlankDestination()
    {
        Vector3 toPlayer = _target.position - transform.position;
        toPlayer.y = 0f;

        if (toPlayer.sqrMagnitude < 0.001f)
        {
            return _target.position;
        }

        toPlayer.Normalize();

        Vector3 side = Vector3.Cross(toPlayer, Vector3.up);
        if (!_flankSideRight)
        {
            side = -side;
        }

        Vector3 flankPoint = _target.position + side * _flankOffset;

        if (NavMesh.SamplePosition(flankPoint, out NavMeshHit hit, FlankSampleRadius, NavMesh.AllAreas))
        {
            return hit.position;
        }

        return _target.position;
    }

    private void CacheTarget()
    {
        if (PlayerManager.instance != null && PlayerManager.instance.player != null)
        {
            _target = PlayerManager.instance.player.transform;
        }
        else
        {
            _target = null;
        }
    }

    private void TryResetAgentOnNavMesh()
    {
        if (_agent.isActiveAndEnabled && _agent.isOnNavMesh)
        {
            _agent.isStopped = false;
            _agent.ResetPath();
            _agent.velocity = Vector3.zero;
        }
    }

    private void StopRageIfNeeded()
    {
        if (_rageRoutine == null)
        {
            return;
        }

        StopCoroutine(_rageRoutine);
        _rageRoutine = null;
    }

    private void SetRunning(bool isRunning)
    {
        if (_animator != null)
        {
            _animator.SetBool("isRunning", isRunning);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _lookRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _attackDistance);
    }
}