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
    }

    public void PlayAgain()
    {
        PlayerStats.Instance?.ResetForNewRun();
        PlayerHealth.Instance?.ResetForNewRun();

        SceneManager.LoadScene("SampleScene");
    }

    public void MainMenu()
    {
        PlayerStats.Instance?.ResetForNewRun();
        PlayerHealth.Instance?.ResetForNewRun();

        SceneManager.LoadScene("Title");
    }
}
