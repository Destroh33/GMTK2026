using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class ClockHand : MonoBehaviour
{
    [Header("Strike Response")]
    [SerializeField] private float strikeJumpDegrees = 18f;
    [SerializeField] private float reverseDuration = 0.35f;
    [SerializeField] private float strikeCooldown = 0.25f;
    [SerializeField] private float strikeMotionDuration = 0.08f;

    [Header("Player Interaction")]
    [SerializeField] private float damageDealt = 1f;

    public event Action<ClockHand, float> OnStruck;

    public static bool StrikesLocked { get; set; }

    public bool IsReversed => reverseTimer > 0f;
    public bool IsStriking => strikeMotionRoutine != null;
    public float AngularVelocity => rb != null ? rb.angularVelocity : 0f;
    public float SweepSign => sweepSign;
    public float StrikeJumpDegrees => strikeJumpDegrees;

    [Header("Ticking")]
    [SerializeField] private float tickInterval = 1f;
    [SerializeField] private float rotatingAngularVelocityThreshold = 0.01f;

    Rigidbody2D rb;
    HingeJoint2D hinge;
    Coroutine strikeMotionRoutine;
    float reverseTimer;
    float cooldownTimer;
    float sweepSign = 1f;
    float tickTimer;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        hinge = GetComponent<HingeJoint2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = false;
        rb.angularDamping = 0f;
        rb.mass = 100f;
    }

    Vector2 Pivot()
    {
        if (hinge != null)
            return (Vector2)transform.TransformPoint(hinge.anchor);
        return rb.position;
    }

    void FixedUpdate()
    {
        if (GameManager.Instance != null && GameManager.Instance.ClockFrozen) return;

        float dt = Time.fixedDeltaTime;
        if (cooldownTimer > 0f) cooldownTimer -= dt;
        if (reverseTimer > 0f) reverseTimer -= dt;

        if (reverseTimer <= 0f && Mathf.Abs(rb.angularVelocity) > 1f)
            sweepSign = Mathf.Sign(rb.angularVelocity);

        UpdateTicking(dt);
    }

    void UpdateTicking(float dt)
    {
        if (Mathf.Abs(rb.angularVelocity) < rotatingAngularVelocityThreshold)
        {
            tickTimer = 0f;
            return;
        }

        tickTimer += dt;

        if (tickTimer >= tickInterval)
        {
            tickTimer -= tickInterval;
            AudioManager.Instance?.PlayClockTickSFX();
        }
    }

    public bool TryStrike(Vector2 hitPoint, Vector2 playerPos)
    {
        if (StrikesLocked) return false;

        // wont hit if waiting for next floor to start
        if (GameManager.Instance != null && GameManager.Instance.ClockFrozen) return false;

        if (cooldownTimer > 0f) return false;

        Vector2 pivot = Pivot();
        Vector2 arm = hitPoint - pivot;
        if (arm.sqrMagnitude < 0.0001f) return false;

        Vector2 tangent = new Vector2(-arm.y, arm.x).normalized;
        Vector2 awayFromPlayer = hitPoint - playerPos;
        if (awayFromPlayer.sqrMagnitude < 0.0001f) return false;

        float alongTangent = Vector2.Dot(awayFromPlayer.normalized, tangent);
        if (Mathf.Abs(alongTangent) < 0.0001f) return false;

        cooldownTimer = strikeCooldown;

        AudioManager.Instance?.PlayClockHandHitSFX();

        float sign = Mathf.Sign(alongTangent);
        reverseTimer = reverseDuration;

        OnStruck?.Invoke(this, sign);
        return true;
    }

    // for minute hand trigger second hand movement
    public void Knock(float sign, float degrees)
    {
        if (GameManager.Instance != null && GameManager.Instance.ClockFrozen) return;

        PlayStrikeMotion(sign, degrees);
        reverseTimer = reverseDuration;
    }

    void PlayStrikeMotion(float sign, float degrees)
    {
        if (strikeMotionRoutine != null)
            StopCoroutine(strikeMotionRoutine);

        strikeMotionRoutine = StartCoroutine(StrikeMotion(sign, degrees));
    }

    IEnumerator StrikeMotion(float sign, float degrees)
    {
        float duration = Mathf.Max(0.01f, strikeMotionDuration);
        float startRotation = rb.rotation;
        float peakRotation = startRotation + sign * degrees;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            if (GameManager.Instance != null && GameManager.Instance.ClockFrozen)
            {
                yield return new WaitForFixedUpdate();
                continue;
            }

            elapsed += Time.fixedDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            rb.MoveRotation(Mathf.LerpAngle(startRotation, peakRotation, Mathf.SmoothStep(0f, 1f, t)));
            yield return new WaitForFixedUpdate();
        }

        strikeMotionRoutine = null;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (GameManager.Instance != null && GameManager.Instance.ClockFrozen) return;

        GameObject other = collision.gameObject;

        if (other.TryGetComponent<PlayerHealth>(out PlayerHealth p))
            p.TakeDamage(damageDealt);

        if (IsReversed && other.TryGetComponent<EnemyBase>(out EnemyBase e))
            e.TakeDamage(damageDealt);
    }
}
