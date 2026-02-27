using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Pause : MonoBehaviour
{

    public GameObject pauseMenu;
    public CanvasGroup lookAreaGroup;

    private bool gameIsPause = false;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
        if (gameIsPause)
            Resume();
        else
            PauseGame();
    }


    public void Resume()
    {
        pauseMenu.SetActive(false);
        gameIsPause = false;
        Time.timeScale = 1;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        if (lookAreaGroup != null)
        {
            lookAreaGroup.blocksRaycasts = true;
            lookAreaGroup.interactable = true;
        }

    }

    public void PauseGame()
    {
        if (lookAreaGroup != null)
        {
            lookAreaGroup.blocksRaycasts = false;
            lookAreaGroup.interactable = false;
        }
        pauseMenu.SetActive(true);
        gameIsPause = true;
        Time.timeScale = 0;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void LoadScene()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Menu");
    }

    public void QuitScene()
    {
        Application.Quit();
    }
}
