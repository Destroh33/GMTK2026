using UnityEngine;
using UnityEngine.SceneManagement;

public class DeathScreen : MonoBehaviour
{
    public GameObject DeathScreenUI;

    public void gameOver()
    {
        DeathScreenUI.SetActive(true);
        Time.timeScale = 0f;
    }

    public void Retry()
    {
        Time.timeScale = 1f;

        if (SceneManager.GetActiveScene().name != "BossScene")
        {
            PlayerStats.Instance?.ResetForNewRun();
            PlayerHealth.Instance?.ResetForNewRun();
        }

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void MainMenu()
    {
        Time.timeScale = 1f;

        PlayerStats.Instance?.ResetForNewRun();
        PlayerHealth.Instance?.ResetForNewRun();

        SceneManager.LoadScene("Title");
    }
}