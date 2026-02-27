using System.Collections;
using UnityEngine;

public sealed class Gun : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField] private float _damage = 10f;
    [SerializeField] private float _damageMultiplier = 1f;

    [Header("Shooting")]
    [SerializeField] private float _range = 100f;
    [SerializeField] private float _fireRate = 10f;

    [Header("Ammo")]
    [SerializeField] private int _maxAmmo = 10;
    [SerializeField] private float _reloadTime = 1.5f;

    [Header("Recoil")]
    [SerializeField] private MouseLook _mouseLook;
    [SerializeField] private float _recoilKick = 1.5f;

    [Header("References")]
    [SerializeField] private Camera _fpsCam;
    [SerializeField] private ParticleSystem _muzzleFlash;
    [SerializeField] private Animator _animator;
    [SerializeField] private Transform _muzzlePoint;

    [Header("FX Pools")]
    [SerializeField] private float _trailSpeed = 30f;

    private PlayerSFX _sfx;

    private int _currentAmmo;
    private bool _isReloading;

    private float _nextFireTime;
    private float _nextEmptyTime;

    // Для мобильной кнопки (UI)
    private bool _isFiringFromButton;

    private void Awake()
    {
        _sfx = GetComponent<PlayerSFX>();
    }

    private void Start()
    {
        _currentAmmo = _maxAmmo;
    }

    private void OnEnable()
    {
        _isReloading = false;

        if (_animator != null)
        {
            _animator.SetBool("Reloading", false);
        }
    }

    private void Update()
    {
        bool firePressed = GetFirePressed();

        // 1) Во время перезарядки: "empty" звук только если игрок жмёт
        if (_isReloading)
        {
            PlayEmptyClickIfNeeded(firePressed);
            return;
        }

        // 2) Если патроны закончились — запускаем перезарядку
        if (_currentAmmo <= 0)
        {
            StartCoroutine(Reload());
            return;
        }

        // 3) Обычная стрельба
        if (firePressed && Time.time >= _nextFireTime)
        {
            _nextFireTime = Time.time + (1f / _fireRate);
            Shoot();
        }
    }

    // --- Mobile UI hooks ---
    public void FireDown() => _isFiringFromButton = true;
    public void FireUp() => _isFiringFromButton = false;

    public void AddDamageMultiplier(float value)
    {
        _damageMultiplier += value;
    }

    private bool GetFirePressed()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        return Input.GetMouseButton(0);
#else
        return _isFiringFromButton;
#endif
    }

    private void PlayEmptyClickIfNeeded(bool firePressed)
    {
        if (!firePressed)
        {
            return;
        }

        if (Time.time < _nextEmptyTime)
        {
            return;
        }

        _sfx?.PlayEmpty();
        _nextEmptyTime = Time.time + 0.3f;
    }

    private IEnumerator Reload()
    {
        _isReloading = true;

        _sfx?.PlayReload();

        if (_animator != null)
        {
            _animator.SetBool("Reloading", true);
        }

        // Твоё поведение: сначала часть времени, потом выключаем анимацию, потом ещё 1 сек.
        yield return new WaitForSeconds(_reloadTime - 0.25f);

        if (_animator != null)
        {
            _animator.SetBool("Reloading", false);
        }

        yield return new WaitForSeconds(1f);

        _currentAmmo = _maxAmmo;
        _isReloading = false;
    }

    private void Shoot()
    {
        if (_fpsCam == null || _muzzlePoint == null)
        {
            return;
        }

        _currentAmmo--;

        _muzzleFlash?.Play();
        _sfx?.PlayShot();

        Vector3 dir = _fpsCam.transform.forward;
        Vector3 endPoint = _muzzlePoint.position + dir * _range;

        if (Physics.Raycast(_fpsCam.transform.position, dir, out RaycastHit hit, _range))
        {
            endPoint = hit.point;

            _mouseLook?.AddRecoil(_recoilKick);

            EnemyHP enemyHp = hit.transform.GetComponentInParent<EnemyHP>();
            if (enemyHp != null)
            {
                int finalDamage = Mathf.RoundToInt(_damage * _damageMultiplier);
                enemyHp.TakeDamage(finalDamage);
            }

            if (ImpactPool.I != null)
            {
                ImpactPool.I.Spawn(hit.point, Quaternion.LookRotation(hit.normal));
            }
        }

        if (FastTrailPool.I != null)
        {
            FastTrailPool.I.Spawn(_muzzlePoint.position, endPoint, _trailSpeed);
        }
    }
}