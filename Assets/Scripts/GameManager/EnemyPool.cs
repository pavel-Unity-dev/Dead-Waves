using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class EnemyPool : MonoBehaviour
{
    [Serializable]
    public sealed class Pool
    {
        [Tooltip("Example: melee / ranged")]
        public string id;

        public GameObject prefab;
        public int prewarmCount = 20;
    }

    [Header("Pools")]
    [SerializeField] private Pool[] _pools;

    public static EnemyPool I { get; private set; }

    // id -> queue
    private readonly Dictionary<string, Queue<GameObject>> _poolMap = new();
    // instance -> id (чтобы понимать куда возвращать)
    private readonly Dictionary<GameObject, string> _instanceToId = new();

    private void Awake()
    {
        SetupSingleton();
        BuildPools();
    }

    public GameObject Get(string id, Vector3 position, Quaternion rotation)
    {
        if (!TryGetQueue(id, out Queue<GameObject> queue))
        {
            Debug.LogError($"EnemyPool: Pool with id '{id}' not found!");
            return null;
        }

        GameObject go = (queue.Count > 0) ? queue.Dequeue() : null;

        if (go == null)
        {
            Pool pool = FindPool(id);
            if (pool == null || pool.prefab == null)
            {
                Debug.LogError($"EnemyPool: No prefab for pool id '{id}'.");
                return null;
            }

            go = CreateNew(id, pool.prefab);
        }

        go.transform.SetPositionAndRotation(position, rotation);
        go.SetActive(true);

        return go;
    }

    public void Return(GameObject go)
    {
        if (go == null)
        {
            return;
        }

        go.SetActive(false);
        go.transform.SetParent(transform);

        if (_instanceToId.TryGetValue(go, out string id) && _poolMap.TryGetValue(id, out Queue<GameObject> queue))
        {
            queue.Enqueue(go);
            return;
        }

        // Если что-то пошло не так (например, объект был создан вне пула)
        Destroy(go);
    }

    private void SetupSingleton()
    {
        if (I != null && I != this)
        {
            Destroy(gameObject);
            return;
        }

        I = this;
    }

    private void BuildPools()
    {
        if (_pools == null || _pools.Length == 0)
        {
            Debug.LogWarning("EnemyPool: No pools configured.");
            return;
        }

        foreach (Pool pool in _pools)
        {
            if (pool == null)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(pool.id))
            {
                Debug.LogWarning("EnemyPool: Pool id is empty. Skipping.");
                continue;
            }

            if (pool.prefab == null)
            {
                Debug.LogWarning($"EnemyPool: Pool '{pool.id}' has no prefab. Skipping.");
                continue;
            }

            if (_poolMap.ContainsKey(pool.id))
            {
                Debug.LogWarning($"EnemyPool: Duplicate pool id '{pool.id}'. Skipping duplicate.");
                continue;
            }

            var queue = new Queue<GameObject>(Mathf.Max(0, pool.prewarmCount));
            _poolMap.Add(pool.id, queue);

            int count = Mathf.Max(0, pool.prewarmCount);
            for (int i = 0; i < count; i++)
            {
                GameObject go = CreateNew(pool.id, pool.prefab);
                Return(go);
            }
        }
    }

    private GameObject CreateNew(string id, GameObject prefab)
    {
        GameObject go = Instantiate(prefab, transform);
        go.SetActive(false);

        _instanceToId[go] = id;

        // Компонент, который умеет возвращать врага в пул
        PooledEnemy pooledEnemy = go.GetComponent<PooledEnemy>();
        if (pooledEnemy == null)
        {
            pooledEnemy = go.AddComponent<PooledEnemy>();
        }

        pooledEnemy.Init(this);

        return go;
    }

    private bool TryGetQueue(string id, out Queue<GameObject> queue)
    {
        queue = null;

        if (string.IsNullOrWhiteSpace(id))
        {
            return false;
        }

        return _poolMap.TryGetValue(id, out queue);
    }

    private Pool FindPool(string id)
    {
        if (_pools == null)
        {
            return null;
        }

        foreach (Pool pool in _pools)
        {
            if (pool != null && pool.id == id)
            {
                return pool;
            }
        }

        return null;
    }
}