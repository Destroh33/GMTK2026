using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class TimerUI : MonoBehaviour
{
    [SerializeField] private TMP_Text tmpLabel;

    [SerializeField] private bool showFractionUnderMinute = false;
    [SerializeField] private float lowTimeThreshold = 10f;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color lowColor = new Color(0.9f, 0.2f, 0.2f);

    void Reset()
    {
        tmpLabel = GetComponent<TMP_Text>();
    }

    void Update()
    {
        float time = GameManager.Instance != null ? GameManager.Instance.TimeRemaining : 0f;
        if (time < 0f) time = 0f;

        string text = Format(time);

        Color color = (lowTimeThreshold > 0f && time <= lowTimeThreshold) ? lowColor : normalColor;

        if (tmpLabel != null)
        {
            tmpLabel.text = text;
            tmpLabel.color = color;
        }
    }

    string Format(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60f);

        if (showFractionUnderMinute && minutes == 0)
        {
            return time.ToString("00.00");
        }

        int seconds = Mathf.FloorToInt(time % 60f);
        return $"{minutes:00}:{seconds:00}";
    }
}
