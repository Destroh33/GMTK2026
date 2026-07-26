using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleManager : MonoBehaviour
{
    public GameObject TitleUI;
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
        TitleUI.SetActive(true);
    }

    public void StartGame()
    {
        SceneManager.LoadScene("Comic");
    }

    public void Settings()
    {
        
    }

    public void Quit()
    {
        Application.Quit();
    }
}
