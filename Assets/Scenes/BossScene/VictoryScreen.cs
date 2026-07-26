using UnityEngine;
using UnityEngine.SceneManagement;

public class VictoryScreen : MonoBehaviour
{
    public GameObject VictoryUI;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    void Awake()
    {
        Boss.OnBossDefeated += ShowVictoryUI;
    }

    void OnDestroy()
    {
        Boss.OnBossDefeated -= ShowVictoryUI;
    }

    void ShowVictoryUI()
    {
        VictoryUI.SetActive(true);
        Time.timeScale = 0f;
    }

    public void PlayAgain()
    {
        PlayerStats.Instance?.ResetForNewRun();
        PlayerHealth.Instance?.ResetForNewRun();
        Time.timeScale = 1f;

        SceneManager.LoadScene("SampleScene");
    }

    public void MainMenu()
    {
        PlayerStats.Instance?.ResetForNewRun();
        PlayerHealth.Instance?.ResetForNewRun();
        Time.timeScale = 1f;

        SceneManager.LoadScene("Title");
    }
}
