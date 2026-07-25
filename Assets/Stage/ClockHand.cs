using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class ClockHand : MonoBehaviour
{
    [Header("Strike Response")]
    [SerializeField] private float strikeJumpDegrees = 40f;
    [SerializeField] private float reverseDuration = 0.4f;
    [SerializeField] private float strikeCooldown = 0.25f;

    [Header("Player Interaction")]
    [SerializeField] private float damageDealt = 1f;

    public event Action<ClockHand, float> OnStruck;

    public bool IsReversed => reverseTimer > 0f;
    public float AngularVelocity => rb != null ? rb.angularVelocity : 0f;
    public float SweepSign => sweepSign;

    Rigidbody2D rb;
    HingeJoint2D hinge;
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
        if (GameManager.Instance != null && !GameManager.Instance.TimerRunning) return false;

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

        rb.angularVelocity = 0f;
        rb.rotation = rb.rotation + sign * strikeJumpDegrees;
        reverseTimer = reverseDuration;

        OnStruck?.Invoke(this, againstSweep);
        return true;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        GameObject other = collision.gameObject;

        if (other.TryGetComponent<PlayerHealth>(out PlayerHealth p))
            p.TakeDamage(damageDealt);

        if (IsReversed && other.TryGetComponent<EnemyBase>(out EnemyBase e))
            e.TakeDamage(damageDealt);
    }
}
