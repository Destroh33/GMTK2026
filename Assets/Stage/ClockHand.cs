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

    public bool IsReversed => reverseTimer > 0f;
    public bool IsStriking => strikeMotionRoutine != null;
    public float AngularVelocity => rb != null ? rb.angularVelocity : 0f;
    public float SweepSign => sweepSign;

    Rigidbody2D rb;
    HingeJoint2D hinge;
    Coroutine strikeMotionRoutine;
    float reverseTimer;
    float cooldownTimer;
    float sweepSign = 1f;

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
    }

    public bool TryStrike(Vector2 hitPoint, Vector2 playerPos)
    {
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

        float sign = Mathf.Sign(alongTangent);
        float againstSweep = sign * sweepSign < 0f ? 1f : -1f;

        PlayStrikeMotion(sign);
        reverseTimer = reverseDuration;

        OnStruck?.Invoke(this, againstSweep);
        return true;
    }

    void PlayStrikeMotion(float sign)
    {
        if (strikeMotionRoutine != null)
            StopCoroutine(strikeMotionRoutine);

        strikeMotionRoutine = StartCoroutine(StrikeMotion(sign));
    }

    IEnumerator StrikeMotion(float sign)
    {
        float duration = Mathf.Max(0.01f, strikeMotionDuration);
        float startRotation = rb.rotation;
        float peakRotation = startRotation + sign * strikeJumpDegrees;
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

        if (!IsStriking && !IsReversed && other.TryGetComponent<PlayerHealth>(out PlayerHealth p))
            p.TakeDamage(damageDealt);

        if (IsReversed && other.TryGetComponent<EnemyBase>(out EnemyBase e))
            e.TakeDamage(damageDealt);
    }
}
