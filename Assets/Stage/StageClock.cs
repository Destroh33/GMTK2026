using System.Collections.Generic;
using UnityEngine;

public class StageClock : MonoBehaviour
{
    [Header("Hands")]
    [SerializeField] private ClockHand secondHand;
    private Rigidbody2D secondHandRB;
    private HingeJoint2D secondHandJoint;
    
    [SerializeField] private ClockHand minuteHand;
    private Rigidbody2D minuteHandRB;
    private HingeJoint2D minuteHandJoint;

    [Header("Rotation Settings")]
    [SerializeField] float timeScale = 1f;
    [SerializeField] private float currentSecSpeed;
    [SerializeField] private float currentMinSpeed;
    [SerializeField] private float defaultHandSpeed;

    private bool secondHandRotatingClockwise = true;
    private bool minuteHandRotatingClockwise = true;

    [Header("Time Restored Per Strike")]
    [SerializeField] private float secondHandBonus = 15f;
    [SerializeField] private float minuteHandBonus = 45f;

    [Header("Diminishing Returns")]
    [SerializeField] private int maxBonusesPerWave = 6;
    [SerializeField] private float falloffPerBonus = 0.85f;
    [SerializeField] private float minBonusMultiplier = 0.25f;

    int bonusesAwarded;

    void Start()
    {
        currentSecSpeed = defaultHandSpeed;
        currentMinSpeed = defaultHandSpeed;

        if (secondHand != null)
        {
            secondHandRB = secondHand.GetComponent<Rigidbody2D>();
            secondHandJoint = secondHand.GetComponent<HingeJoint2D>();
            if (secondHandJoint != null) secondHandJoint.useMotor = true;
        }

        if (minuteHand != null)
        {
            minuteHandRB = minuteHand.GetComponent<Rigidbody2D>();
            minuteHandJoint = minuteHand.GetComponent<HingeJoint2D>();
            if (minuteHandJoint != null) minuteHandJoint.useMotor = true;
        }
    }

    private void FixedUpdate()
    {
        // Use Check against IsReversed directly from the hand scripts to know exactly when a strike's reverse duration is active
        if (secondHand != null) secondHandRotatingClockwise = !secondHand.IsReversed;
        if (minuteHand != null) minuteHandRotatingClockwise = !minuteHand.IsReversed;

        // In Unity 2D, negative angular velocity is clockwise.
        int dirSec = secondHandRotatingClockwise ? -1 : 1;
        int dirMin = minuteHandRotatingClockwise ? -1 : 1;

        if (secondHandJoint != null)
        {
            JointMotor2D secMotor = secondHandJoint.motor;
            secMotor.motorSpeed = currentSecSpeed * timeScale * dirSec;
            secMotor.maxMotorTorque = 1000f; // Adjusted so it can potentially be reversed by hits
            secondHandJoint.motor = secMotor;
        }

        if (minuteHandJoint != null)
        {
            JointMotor2D minMotor = minuteHandJoint.motor;
            minMotor.motorSpeed = currentMinSpeed * timeScale * dirMin;
            minMotor.maxMotorTorque = 1000f;
            minuteHandJoint.motor = minMotor;
        }
    }

    public void SetTimeScale(float newTimeScale) 
    {
        timeScale = newTimeScale;
    }

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
