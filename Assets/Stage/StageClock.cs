using System;
using UnityEngine;

public class StageClock : MonoBehaviour
{
    public static event Action<float> OnStrikeTimeGained;
    public static event Action OnAnyHandStruck;

    [Header("Hands")]
    [SerializeField] private ClockHand secondHand;
    private HingeJoint2D secondHandJoint;
    private Rigidbody2D secondHandRb;

    [SerializeField] private ClockHand minuteHand;
    private HingeJoint2D minuteHandJoint;
    private Rigidbody2D minuteHandRb;

    [Header("Rotation Settings")]
    [SerializeField] private float defaultHandSpeed = 30f;
    [SerializeField] private float timeScale = 1f;
    [SerializeField] private float motorTorque = 1000f;
    [SerializeField] private bool clockwiseIsPositiveMotor = true;
    [SerializeField] private float minuteHandSpeedRatio = 1f / 12f;

    [Header("Time Per Strike")]
    [SerializeField] private float secondHandBonus = 15f;
    [SerializeField] private float minuteHandBonus = 45f;
    [SerializeField] private float withSweepMultiplier = -1f;
    [SerializeField] private bool scaleBonusByCountdownSpeed = true;

    [Header("Diminishing Returns")]
    [SerializeField] private int strikesToZero = 7;
    [SerializeField] private float decayExponent = 2.5f;
    [SerializeField] private int freeStrikesPerFloor = 1;

    [Header("Minute Hand Coupling")]
    [Tooltip("Every time the minute hand is struck, the second hand also gets knocked (same strike-tween mechanic) in the same direction, by the minute hand's own StrikeJumpDegrees times this multiplier.")]
    [SerializeField] private float secondHandKickMultiplier = 5f;

    [Header("Second Hand Coupling")]
    [Tooltip("Every time the second hand is struck, the minute hand also gets knocked (same strike-tween mechanic) in the same direction, by the second hand's own StrikeJumpDegrees times this multiplier - keep this small, the minute hand should barely move.")]
    [SerializeField] private float minuteHandKickMultiplier = 0.1f;

    bool wasFrozen;
    int strikesThisFloor;
    int lastFloorIndex = int.MinValue;

    void Start()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnRunReset += ResetStrikeCount;

        if (secondHand != null)
        {
            secondHandJoint = secondHand.GetComponent<HingeJoint2D>();
            secondHandRb = secondHand.GetComponent<Rigidbody2D>();
            if (secondHandJoint != null) secondHandJoint.useMotor = true;
        }

        if (minuteHand != null)
        {
            minuteHandJoint = minuteHand.GetComponent<HingeJoint2D>();
            minuteHandRb = minuteHand.GetComponent<Rigidbody2D>();
            if (minuteHandJoint != null) minuteHandJoint.useMotor = true;
        }

        if (defaultHandSpeed <= 0f)
            defaultHandSpeed = 30f;
    }

    void FixedUpdate()
    {
        bool running = GameManager.Instance == null || !GameManager.Instance.ClockFrozen;
        bool frozen = !running;

        float sweepSign = clockwiseIsPositiveMotor ? 1f : -1f;
        float clockwiseSpeed = running ? sweepSign * defaultHandSpeed * timeScale : 0;

        DriveHand(secondHand, secondHandJoint, clockwiseSpeed);
        DriveHand(minuteHand, minuteHandJoint, clockwiseSpeed * minuteHandSpeedRatio);

        TrackFloorChange();

        if (frozen != wasFrozen)
        {
            SetHandFrozen(secondHandRb, frozen);
            SetHandFrozen(minuteHandRb, frozen);
            wasFrozen = frozen;
        }
    }

    void SetHandFrozen(Rigidbody2D rb, bool frozen)
    {
        if (rb == null) return;
        rb.constraints = frozen ? RigidbodyConstraints2D.FreezeAll : RigidbodyConstraints2D.None;

        if (frozen)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }

    void DriveHand(ClockHand hand, HingeJoint2D joint, float speed)
    {
        if (joint == null) return;

        if (hand != null && hand.IsStriking)
        {
            joint.useMotor = false;
            return;
        }

        joint.useMotor = true;

        JointMotor2D m = joint.motor;
        m.motorSpeed = speed;
        m.maxMotorTorque = motorTorque;
        joint.motor = m;
    }

    void OnEnable()
    {
        if (secondHand != null) secondHand.OnStruck += HandleSecondHandStruck;
        if (minuteHand != null) minuteHand.OnStruck += HandleMinuteHandStruck;
    }

    void OnDisable()
    {
        if (secondHand != null) secondHand.OnStruck -= HandleSecondHandStruck;
        if (minuteHand != null) minuteHand.OnStruck -= HandleMinuteHandStruck;
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnRunReset -= ResetStrikeCount;
    }

    void HandleSecondHandStruck(ClockHand hand, float strikeDirectionSign)
    {
        float kick = ApplyStrikeTime(secondHandBonus, strikeDirectionSign);

        if (secondHand == null) return;

        float degrees = secondHand.StrikeJumpDegrees * kick;
        secondHand.Knock(strikeDirectionSign, degrees);

        if (minuteHand != null)
            minuteHand.Knock(strikeDirectionSign, degrees * minuteHandKickMultiplier);
    }

    void HandleMinuteHandStruck(ClockHand hand, float strikeDirectionSign)
    {
        float kick = ApplyStrikeTime(minuteHandBonus, strikeDirectionSign);

        if (minuteHand == null) return;

        float degrees = minuteHand.StrikeJumpDegrees * kick;
        minuteHand.Knock(strikeDirectionSign, degrees);

        if (secondHand != null)
            secondHand.Knock(strikeDirectionSign, degrees * secondHandKickMultiplier);
    }

    float ApplyStrikeTime(float baseAmount, float strikeDirectionSign)
    {
        if (GameManager.Instance == null) return 1f;

        if (!GameManager.Instance.TimerRunning) return 1f;

        OnAnyHandStruck?.Invoke();

        bool hitClockwise = clockwiseIsPositiveMotor ? strikeDirectionSign > 0f : strikeDirectionSign < 0f;
        float amount = baseAmount * (hitClockwise ? -1f : 1f);

        if (scaleBonusByCountdownSpeed)
            amount *= Mathf.Max(1f, GameManager.Instance.CountdownSpeed);

        float kick = 1f;

        if (amount > 0f)
        {
            kick = StrikeGainMultiplier();
            amount *= kick;
            strikesThisFloor++;
            OnStrikeTimeGained?.Invoke(amount);
        }

        GameManager.Instance.AddTime(amount);

        return kick;
    }

    float StrikeGainMultiplier()
    {
        int charged = Mathf.Max(0, strikesThisFloor - Mathf.Max(0, freeStrikesPerFloor - 1));
        int zeroAt = Mathf.Max(1, strikesToZero);

        if (charged >= zeroAt) return 0f;

        float t = (float)charged / zeroAt;
        return Mathf.Pow(1f - t, Mathf.Max(0.01f, decayExponent));
    }

    void TrackFloorChange()
    {
        int floorIndex = GameManager.Instance != null ? GameManager.Instance.CurrentFloorIndex : 0;
        if (floorIndex == lastFloorIndex) return;

        lastFloorIndex = floorIndex;
        strikesThisFloor = 0;
    }

    void ResetStrikeCount() => strikesThisFloor = 0;

    public void SetTimeScale(float newTimeScale) => timeScale = newTimeScale;

    public bool IsTimeLeft()
    {
        return GameManager.Instance != null && GameManager.Instance.TimeRemaining > 0f;
    }
}
