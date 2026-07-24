using System.Collections.Generic;
using UnityEngine;

public class StageClock : MonoBehaviour
{
    [Header("Hands")]
    [SerializeField] private ClockHand secondHand;
    [SerializeField] private ClockHand minuteHand;

    [Header("Time Restored Per Strike")]
    [SerializeField] private float secondHandBonus = 15f;
    [SerializeField] private float minuteHandBonus = 45f;

    [Header("Diminishing Returns")]
    [SerializeField] private int maxBonusesPerWave = 6;
    [SerializeField] private float falloffPerBonus = 0.85f;
    [SerializeField] private float minBonusMultiplier = 0.25f;

    int bonusesAwarded;

    void OnEnable()
    {
        if (secondHand != null) secondHand.OnStruckBackwards += HandleSecondHandStruck;
        if (minuteHand != null) minuteHand.OnStruckBackwards += HandleMinuteHandStruck;

        if (GameManager.Instance != null)
            GameManager.Instance.OnWaveStarted += HandleWaveStarted;
    }

    void OnDisable()
    {
        if (secondHand != null) secondHand.OnStruckBackwards -= HandleSecondHandStruck;
        if (minuteHand != null) minuteHand.OnStruckBackwards -= HandleMinuteHandStruck;

        if (GameManager.Instance != null)
            GameManager.Instance.OnWaveStarted -= HandleWaveStarted;
    }

    void HandleWaveStarted(int waveIndex)
    {
        bonusesAwarded = 0;
    }

    void HandleSecondHandStruck(ClockHand hand) => AwardTime(secondHandBonus);

    void HandleMinuteHandStruck(ClockHand hand) => AwardTime(minuteHandBonus);

    void AwardTime(float baseAmount)
    {
        if (GameManager.Instance == null) return;
        if (maxBonusesPerWave > 0 && bonusesAwarded >= maxBonusesPerWave) return;

        float multiplier = Mathf.Max(minBonusMultiplier, Mathf.Pow(falloffPerBonus, bonusesAwarded));
        GameManager.Instance.AddTime(baseAmount * multiplier);

        bonusesAwarded++;
    }

    public bool IsTimeLeft()
    {
        return GameManager.Instance != null && GameManager.Instance.TimeRemaining > 0f;
    }
}
