using UnityEngine;

public class PooledEnemy : MonoBehaviour
{
    private EnemyPool _pool;

    public void Init(EnemyPool pool)
    {
        _pool = pool;
    }

    public void Despawn()
    {
        if (_pool != null) _pool.Return(gameObject);
        else Destroy(gameObject);
    }
}