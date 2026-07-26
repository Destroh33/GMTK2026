using UnityEngine;

public class StageClock : MonoBehaviour
{
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

    bool wasFrozen;

    void Start()
    {
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

    void HandleSecondHandStruck(ClockHand hand, float againstSweep) => ApplyStrikeTime(secondHandBonus, againstSweep);

    void HandleMinuteHandStruck(ClockHand hand, float againstSweep) => ApplyStrikeTime(minuteHandBonus, againstSweep);

    void ApplyStrikeTime(float baseAmount, float againstSweep)
    {
        if (GameManager.Instance == null) return;

        if (!GameManager.Instance.TimerRunning) return;

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
