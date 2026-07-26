using UnityEngine;
using UnityEngine.SceneManagement;

public class DeathScreen : MonoBehaviour
{
    public GameObject DeathScreenUI;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void gameOver()
    {
        DeathScreenUI.SetActive(true);
    }

    public void Retry()
    {
        if (SceneManager.GetActiveScene().name != "BossScene")
        {
            PlayerStats.Instance?.ResetForNewRun();
            PlayerHealth.Instance?.ResetForNewRun();
        }

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void MainMenu()
    {
        PlayerStats.Instance?.ResetForNewRun();
        PlayerHealth.Instance?.ResetForNewRun();

        SceneManager.LoadScene("Title");
    }
}
