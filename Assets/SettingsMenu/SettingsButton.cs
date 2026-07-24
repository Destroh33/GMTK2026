using UnityEngine;
using UnityEngine.UI;

public class SettingsButton : MonoBehaviour
{
    [SerializeField] GameObject settingsPanel;

    public void SetSettingsPanelActive()
    {
        settingsPanel.SetActive(!settingsPanel.activeSelf);
    }
}
