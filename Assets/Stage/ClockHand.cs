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
    [SerializeField] private int damageDealt = 1;

    public event Action<ClockHand, float> OnStruck;

    public bool IsReversed => reverseTimer > 0f;
    public float AngularVelocity => rb != null ? rb.angularVelocity : 0f;

    Rigidbody2D rb;
    HingeJoint2D hinge;
    float reverseTimer;
    float cooldownTimer;

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
        float dt = Time.fixedDeltaTime;
        if (cooldownTimer > 0f) cooldownTimer -= dt;
        if (reverseTimer > 0f) reverseTimer -= dt;
    }

    public bool TryStrike(Vector2 hitPoint, Vector2 playerPos)
    {
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
        rb.angularVelocity = 0f;
        rb.rotation = rb.rotation + sign * strikeJumpDegrees;
        reverseTimer = reverseDuration;

        OnStruck?.Invoke(this, sign);
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
