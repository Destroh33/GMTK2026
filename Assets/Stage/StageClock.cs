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

    [Header("Diminishing Returns")]
    [SerializeField] private int maxBonusesPerWave = 6;
    [SerializeField] private float falloffPerBonus = 0.85f;
    [SerializeField] private float minBonusMultiplier = 0.25f;

    int bonusesAwarded;

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

    void FixedUpdate()
    {
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

        if (GameManager.Instance != null)
            GameManager.Instance.OnWaveStarted += HandleWaveStarted;
    }

    void OnDisable()
    {
        if (secondHand != null) secondHand.OnStruck -= HandleSecondHandStruck;
        if (minuteHand != null) minuteHand.OnStruck -= HandleMinuteHandStruck;

        if (GameManager.Instance != null)
            GameManager.Instance.OnWaveStarted -= HandleWaveStarted;
    }

    void HandleWaveStarted(int waveIndex) => bonusesAwarded = 0;

    void HandleSecondHandStruck(ClockHand hand, float kick) => ApplyStrikeTime(secondHandBonus);

    void HandleMinuteHandStruck(ClockHand hand, float kick) => ApplyStrikeTime(minuteHandBonus);

    void ApplyStrikeTime(float baseAmount)
    {
        if (GameManager.Instance == null) return;
        if (maxBonusesPerWave > 0 && bonusesAwarded >= maxBonusesPerWave) return;

        float multiplier = Mathf.Max(minBonusMultiplier, Mathf.Pow(falloffPerBonus, bonusesAwarded));
        GameManager.Instance.AddTime(baseAmount * multiplier);
        bonusesAwarded++;
    }

    public void SetTimeScale(float newTimeScale) => timeScale = newTimeScale;

    public bool IsTimeLeft()
    {
        return GameManager.Instance != null && GameManager.Instance.TimeRemaining > 0f;
    }
}
