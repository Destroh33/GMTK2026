using UnityEngine;

public class StageClock : MonoBehaviour
{
    [Header("Hands")]
    [SerializeField] private ClockHand secondHand;
    private HingeJoint2D secondHandJoint;

    [SerializeField] private ClockHand minuteHand;
    private HingeJoint2D minuteHandJoint;

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

    void Start()
    {
        if (secondHand != null)
        {
            secondHandJoint = secondHand.GetComponent<HingeJoint2D>();
            if (secondHandJoint != null) secondHandJoint.useMotor = true;
        }

        if (minuteHand != null)
        {
            minuteHandJoint = minuteHand.GetComponent<HingeJoint2D>();
            if (minuteHandJoint != null) minuteHandJoint.useMotor = true;
        }

        if (defaultHandSpeed <= 0f)
            defaultHandSpeed = 30f;
    }

    bool Frozen => GameManager.Instance != null && GameManager.Instance.AwaitingPowerupChoice;

    void FixedUpdate()
    {
        if (Frozen)
        {
            DriveHand(secondHandJoint, 0f);
            DriveHand(minuteHandJoint, 0f);

            if (secondHand != null) secondHand.Freeze();
            if (minuteHand != null) minuteHand.Freeze();
            return;
        }

        float sweepSign = clockwiseIsPositiveMotor ? 1f : -1f;
        float clockwiseSpeed = sweepSign * defaultHandSpeed * timeScale;

        DriveHand(secondHandJoint, clockwiseSpeed);
        DriveHand(minuteHandJoint, clockwiseSpeed * minuteHandSpeedRatio);
    }

    void DriveHand(HingeJoint2D joint, float speed)
    {
        if (joint == null) return;

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

    void HandleSecondHandStruck(ClockHand hand, float againstSweep) => ApplyStrikeTime(secondHandBonus, againstSweep);

    void HandleMinuteHandStruck(ClockHand hand, float againstSweep) => ApplyStrikeTime(minuteHandBonus, againstSweep);

    void ApplyStrikeTime(float baseAmount, float againstSweep)
    {
        if (GameManager.Instance == null) return;

        float amount = baseAmount * (againstSweep > 0f ? 1f : withSweepMultiplier);

        if (scaleBonusByCountdownSpeed)
            amount *= Mathf.Max(1f, GameManager.Instance.CountdownSpeed);

        GameManager.Instance.AddTime(amount);
    }

    public void SetTimeScale(float newTimeScale) => timeScale = newTimeScale;

    public bool IsTimeLeft()
    {
        return GameManager.Instance != null && GameManager.Instance.TimeRemaining > 0f;
    }
}
