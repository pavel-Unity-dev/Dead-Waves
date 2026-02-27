using UnityEngine;
using TMPro;

public class ShopUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TMP_Text coinsText;

    [Header("Prices")]
    [SerializeField] private int damagePrice = 50;
    [SerializeField] private int healPrice = 30;

    [Header("Links")]
    [SerializeField] private Gun gun;                 // твой Gun
    [SerializeField] private PlayerHealth playerHP;   // хп игрока

    private void OnEnable()
    {
        Refresh();
    }

    public void Show()
    {
        gameObject.SetActive(true);
        Refresh();
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private void Refresh()
    {
        coinsText.text = $"COINS: {CurrencyManager.Instance.Coins}";
    }

    public void BuyDamage()
    {
        if (!CurrencyManager.Instance.TrySpend(damagePrice)) return;

        gun.AddDamageMultiplier(0.1f); // +10%
        Refresh();
    }

    public void BuyHeal()
    {
        if (!CurrencyManager.Instance.TrySpend(healPrice)) return;

        playerHP.Heal(25);
        Refresh();
    }
}
