using UnityEngine;
using UnityEngine.UI;

public class SettingsButton : MonoBehaviour
{
    public static SettingsButton Instance { get; private set; }
    public bool gamePaused = false;
    [SerializeField] GameObject settingsPanel;
    private float currentScale = 1f;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void SetSettingsPanelActive()
    {
        settingsPanel.SetActive(!settingsPanel.activeSelf);
        if(settingsPanel.activeSelf)
        {
            gamePaused = true;
            currentScale = Time.timeScale;
            Time.timeScale = 0f;
        }
        else
        {
            gamePaused = false;
            Time.timeScale = currentScale;
        }
    }
}
