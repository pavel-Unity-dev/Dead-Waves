using UnityEngine;
using UnityEngine.UI;

public class EnemyHPUI : MonoBehaviour
{
    public EnemyHP enemyHP;
    public Slider sliderHP;

    private void Start()
    {
        sliderHP.maxValue = enemyHP.maxHP;
        sliderHP.value = enemyHP.CurrentHP;
    }

    private void Update()
    {
        sliderHP.value = enemyHP.CurrentHP;
    }

}
