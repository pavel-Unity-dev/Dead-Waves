using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.AI;

public sealed class WaveManager : MonoBehaviour
{
    [Header("Spawn Points")]
    [SerializeField] private Transform[] _spawnPoints;

    [Header("Wave Rules")]
    [SerializeField] private float _timeBetweenWaves = 8f;
    [SerializeField] private int _startEnemies = 10;
    [SerializeField] private int _addPerWave = 5;
    [SerializeField] private int _maxAlive = 25;

    [Header("Spawn")]
    [SerializeField] private float _spawnInterval = 0.35f;
    [SerializeField] private float _navSampleDistance = 2f;

    [Header("Pools")]
    [SerializeField] private EnemyPool _enemyPool;

    [Header("UI (Top Center)")]
    [SerializeField] private TMP_Text _waveText;
    [SerializeField] private TMP_Text _timerText;

    [Header("Shop & Rewards")]
    [SerializeField] private ShopUI _shopUI;
    [SerializeField] private int _coinsPerWave = 50;

    [Header("Anti-overlap spawn")]
    [SerializeField] private LayerMask _enemyLayer;
    [SerializeField] private float _minSpawnSeparation = 1.2f;
    [SerializeField] private int _spawnTries = 12;
    [SerializeField] private float _spawnJitterRadius = 3f;
    [SerializeField] private LayerMask _blockedMask;
    [SerializeField] private float _blockedCheckRadius = 0.45f;

    public static WaveManager Instance { get; private set; }

    public int CurrentWave { get; private set; }
    public bool IsWaveRunning { get; private set; }

    private int _alive;
    private int _spawnedThisWave;
    private int _toSpawnThisWave;

    private WaitForSeconds _waitSpawn;

    private void Awake()
    {
        SetupSingleton();
        _waitSpawn = new WaitForSeconds(_spawnInterval);
    }

    private void Start()
    {
        ApplyUI_Prepare(_timeBetweenWaves);
        StartCoroutine(WaveLoop());
    }

    private void SetupSingleton()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private IEnumerator WaveLoop()
    {
        while (true)
        {
            // PREPARE
            IsWaveRunning = false;

            float t = _timeBetweenWaves;
            while (t > 0f)
            {
                ApplyUI_Prepare(t);
                t -= Time.deltaTime;
                yield return null;
            }

            // START WAVE
            StartWave();

            // SPAWN LOOP
            while (IsWaveRunning)
            {
                bool canSpawnMore = _spawnedThisWave < _toSpawnThisWave && _alive < _maxAlive;

                if (canSpawnMore)
                {
                    SpawnOne();
                    _spawnedThisWave++;

                    // если ты мен€ешь _spawnInterval в рантайме Ч кеш нужно обновл€ть
                    yield return _waitSpawn;
                    continue;
                }

                // END WAVE CONDITION
                if (_spawnedThisWave >= _toSpawnThisWave && _alive <= 0)
                {
                    EndWave();
                    break;
                }

                yield return null;
            }
        }
    }

    private void StartWave()
    {
        CurrentWave++;

        _toSpawnThisWave = _startEnemies + (CurrentWave - 1) * _addPerWave;
        _spawnedThisWave = 0;

        IsWaveRunning = true;
        ApplyUI_WaveStart();
    }

    private void EndWave()
    {
        IsWaveRunning = false;
        ApplyUI_WaveEnd();
    }

    private void SpawnOne()
    {
        if (_spawnPoints == null || _spawnPoints.Length == 0)
        {
            return;
        }

        if (_enemyPool == null)
        {
            _enemyPool = EnemyPool.I;
        }

        if (_enemyPool == null)
        {
            return;
        }

        string poolId = ChooseEnemyPoolId();

        Transform sp = _spawnPoints[Random.Range(0, _spawnPoints.Length)];

        for (int i = 0; i < _spawnTries; i++)
        {
            Vector3 candidate = GetJitteredSpawnPoint(sp.position);

            if (!TryGetNavPos(candidate, out Vector3 navPos))
            {
                continue;
            }

            if (IsBlocked(navPos))
            {
                continue;
            }

            if (IsEnemyTooClose(navPos))
            {
                continue;
            }

            GameObject enemyGO = _enemyPool.Get(poolId, navPos, sp.rotation);
            if (enemyGO == null)
            {
                return;
            }

            FinalizeSpawn(enemyGO, navPos);

            _alive++;
            return;
        }

        // если место не нашли Ч пропускаем спавн (ок)
    }

    private Vector3 GetJitteredSpawnPoint(Vector3 basePos)
    {
        Vector3 offset = Random.insideUnitSphere * _spawnJitterRadius;
        offset.y = 0f;
        return basePos + offset;
    }

    private bool TryGetNavPos(Vector3 pos, out Vector3 navPos)
    {
        if (NavMesh.SamplePosition(pos, out NavMeshHit hit, _navSampleDistance, NavMesh.AllAreas))
        {
            navPos = hit.position;
            return true;
        }

        navPos = pos;
        return false;
    }

    private bool IsBlocked(Vector3 navPos)
    {
        Vector3 checkPos = navPos + Vector3.up * 0.5f;
        return Physics.CheckSphere(checkPos, _blockedCheckRadius, _blockedMask, QueryTriggerInteraction.Ignore);
    }

    private bool IsEnemyTooClose(Vector3 navPos)
    {
        return Physics.CheckSphere(navPos, _minSpawnSeparation, _enemyLayer, QueryTriggerInteraction.Ignore);
    }

    private void FinalizeSpawn(GameObject enemyGO, Vector3 navPos)
    {
        var agent = enemyGO.GetComponent<NavMeshAgent>();
        if (agent != null && agent.isActiveAndEnabled)
        {
            agent.Warp(navPos);
        }

        enemyGO.GetComponent<EnemyMeleeAI>()?.OnSpawnedOnNavMesh();
        enemyGO.GetComponent<EnemyAI>()?.OnSpawnedOnNavMesh();

        EnemyDeathReporter reporter = enemyGO.GetComponent<EnemyDeathReporter>();
        if (reporter == null)
        {
            reporter = enemyGO.AddComponent<EnemyDeathReporter>();
        }

        reporter.Init(this);
    }

    private string ChooseEnemyPoolId()
    {
        if (CurrentWave <= 2)
        {
            return "melee";
        }

        if (CurrentWave <= 4)
        {
            return Random.value < 0.7f ? "melee" : "ranged";
        }

        return Random.value < 0.5f ? "melee" : "ranged";
    }

    public void NotifyEnemyDied()
    {
        _alive = Mathf.Max(0, _alive - 1);
    }

    private void ApplyUI_Prepare(float timeLeft)
    {
        if (_waveText != null)
        {
            int nextWave = Mathf.Max(1, CurrentWave + 1);
            _waveText.text = $"WAVE {nextWave}";
        }

        if (_timerText != null)
        {
            int sec = Mathf.CeilToInt(Mathf.Max(0f, timeLeft));
            _timerText.text = $"NEXT WAVE IN: {sec}";
        }
    }

    private void ApplyUI_WaveStart()
    {
        if (_waveText != null)
        {
            _waveText.text = $"WAVE {CurrentWave}";
        }

        if (_timerText != null)
        {
            _timerText.text = string.Empty;
        }

        _shopUI?.Hide();
        CursorManager.LockCursor();
    }

    private void ApplyUI_WaveEnd()
    {
        CurrencyManager.Instance?.AddCoins(_coinsPerWave);

        _shopUI?.Show();
        CursorManager.UnlockCursor();

        if (_timerText != null)
        {
            _timerText.text = "CLEARED!";
        }
    }
}