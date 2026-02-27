using UnityEngine;
using UnityEngine.UI;

public sealed class PlayerHPUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerHealth _playerHealth;
    [SerializeField] private Slider _hpSlider;

    private void Start()
    {
        if (_playerHealth == null || _hpSlider == null)
        {
            Debug.LogWarning("PlayerHPUI: Missing references.");
            return;
        }

        _hpSlider.maxValue = _playerHealth.MaxHealth;
        _hpSlider.value = _playerHealth.CurrentHealth;
    }

    private void Update()
    {
        if (_playerHealth == null) return;

        _hpSlider.value = _playerHealth.CurrentHealth;
    }
}