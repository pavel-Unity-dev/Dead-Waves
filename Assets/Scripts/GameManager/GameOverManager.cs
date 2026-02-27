using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class GameOverManager : MonoBehaviour
{
    public static GameOverManager Instance;

    [Header("UI")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TMP_Text statsText;

    private bool isGameOver;

    private void Awake()
    {
        Instance = this;
        gameOverPanel.SetActive(false);
    }

    public void ShowGameOver(int waves, int kills, int coins)
    {
        if (isGameOver) return;
        isGameOver = true;

        gameOverPanel.SetActive(true);

        statsText.text =
            $"WAVES: {waves}\n" +
            $"KILLS: {kills}\n" +
            $"COINS: {coins}";

        Time.timeScale = 0f; // стопаем игру

        // для мобилки / UI
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
