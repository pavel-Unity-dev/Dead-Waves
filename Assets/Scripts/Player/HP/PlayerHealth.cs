using UnityEngine;

public sealed class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private float _maxHealth = 100f;

    public float CurrentHealth { get; private set; }
    public float MaxHealth => _maxHealth;

    private void Awake()
    {
        CurrentHealth = _maxHealth;
    }

    public void TakeDamage(float damage)
    {
        CurrentHealth -= damage;
        CurrentHealth = Mathf.Clamp(CurrentHealth, 0f, _maxHealth);

        DamageFlash.Instance?.Flash();
        CameraShake.Instance?.Shake(0.15f, 0.08f);

        if (CurrentHealth <= 0f)
        {
            HandleDeath();
        }
    }

    public void Heal(float amount)
    {
        CurrentHealth = Mathf.Min(_maxHealth, CurrentHealth + amount);
    }

    private void HandleDeath()
    {
        GameOverManager.Instance?.ShowGameOver(
            WaveManager.Instance?.CurrentWave ?? 0,
            PlayerStats.Kills,
            CurrencyManager.Instance?.Coins ?? 0
        );
    }
}