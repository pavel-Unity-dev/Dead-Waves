using UnityEngine;
using System.Collections;
using UnityEngine.AI;

public class EnemyHP : MonoBehaviour
{
    public float CurrentHP { get; private set; }
    public float maxHP = 50f;

    Animator animator;
    NavMeshAgent agent;

    public float destroyDelay = 3f;

    bool isDead = false;
    Coroutine despawnRoutine;

    void Awake()
    {
        animator = GetComponentInChildren<Animator>();
        agent = GetComponent<NavMeshAgent>();
    }

    // ВАЖНО: при каждом взятии из пула объект включается -> тут мы "оживляем" врага
    void OnEnable()
    {
        // стопаем старую корутину, если вдруг объект вернули/взяли быстро
        if (despawnRoutine != null)
        {
            StopCoroutine(despawnRoutine);
            despawnRoutine = null;
        }

        isDead = false;
        CurrentHP = maxHP;

        // включаем коллайдеры обратно
        foreach (Collider c in GetComponentsInChildren<Collider>(true))
            c.enabled = true;

        // включаем AI обратно
        EnemyAI ranged = GetComponent<EnemyAI>();
        if (ranged != null) ranged.enabled = true;

        EnemyMeleeAI melee = GetComponent<EnemyMeleeAI>();
        if (melee != null) melee.enabled = true;

        // включаем NavMeshAgent обратно
        if (agent != null)
            agent.enabled = true;

        // сброс анимации (важно для пула)
        if (animator != null)
        {
            animator.Rebind();
            animator.Update(0f);
        }

        // сброс репортера (чтобы на следующей смерти снова засчиталось)
        GetComponent<EnemyDeathReporter>()?.ResetReporter();
    }

    public void TakeDamage(float dmg)
    {
        if (isDead) return;

        GetComponent<EnemySFX>()?.PlayHit();
        GetComponent<EnemyMeleeAI>()?.OnShotHit();

        CurrentHP -= dmg;
        CurrentHP = Mathf.Clamp(CurrentHP, 0, maxHP);

        if (CurrentHP <= 0)
        {
            HitmarkerUI.Instance?.ShowKill();
            Die();
        }
        else
        {
            HitmarkerUI.Instance?.ShowHit();
        }
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        GetComponent<EnemySFX>()?.PlayDeath();

        // анимация смерти
        if (animator != null)
            animator.SetTrigger("die");

        // выключаем навмеш
        if (agent != null)
            agent.enabled = false;

        // выключаем AI
        EnemyAI ranged = GetComponent<EnemyAI>();
        if (ranged != null) ranged.enabled = false;

        EnemyMeleeAI melee = GetComponent<EnemyMeleeAI>();
        if (melee != null) melee.enabled = false;

        // выключаем коллайдеры
        foreach (Collider c in GetComponentsInChildren<Collider>())
            c.enabled = false;

        // сообщаем WaveManager, что враг умер
        GetComponent<EnemyDeathReporter>()?.NotifyDeath();
        PlayerStats.Kills++;

        // вместо Destroy — возвращаем в пул после задержки
        despawnRoutine = StartCoroutine(DespawnAfterDelay());
    }

    IEnumerator DespawnAfterDelay()
    {
        yield return new WaitForSeconds(destroyDelay);

        var pooled = GetComponent<PooledEnemy>();
        if (pooled != null)
        {
            pooled.Despawn();   // вернёт в пул
        }
        else
        {
            Destroy(gameObject); // на всякий случай, если не из пула
        }
    }
}