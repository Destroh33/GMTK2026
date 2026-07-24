using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class ClockHand : MonoBehaviour
{
    [Header("Rotation")]
    [SerializeField] private float baseSpeed = 30f;
    [SerializeField] private float maxSpeed = 220f;
    [SerializeField] private float driveTorque = 400f;

    [Header("Strike Response")]
    [SerializeField] private float strikeImpulse = 260f;
    [SerializeField] private float reverseDuration = 2.5f;
    [SerializeField] private float strikeCooldown = 0.25f;
    [SerializeField] private float minTangentDot = 0.25f;

    [Header("Player Push")]
    [SerializeField] private float pushForce = 14f;
    [SerializeField] private float pushRadiusScale = 1f;

    [Header("Player Damage")]
    [SerializeField] private int damageDealt = 1;

    public event Action<ClockHand> OnStruckBackwards;

    public bool IsReversed => reverseTimer > 0f;
    public float AngularVelocity => rb != null ? rb.angularVelocity : 0f;

    Rigidbody2D rb;
    float reverseTimer;
    float cooldownTimer;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = false;
    }

    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;

        if (cooldownTimer > 0f) cooldownTimer -= dt;

        if (reverseTimer > 0f)
        {
            reverseTimer -= dt;
        }
        
        // Removed manual AddTorque and speed clamping here 
        // because StageClock now manages continuous rotation via HingeJoint2D motors.
    }

    public bool TryStrike(Vector2 hitPoint, Vector2 attackDir)
    {
        if (cooldownTimer > 0f) return false;

        Vector2 pivot = rb.position;
        Vector2 arm = hitPoint - pivot;
        if (arm.sqrMagnitude < 0.0001f) return false;

        Vector2 tangent = new Vector2(-arm.y, arm.x).normalized;
        float alongTangent = Vector2.Dot(attackDir.normalized, tangent);

        rb.AddTorque(alongTangent * strikeImpulse, ForceMode2D.Impulse);
        cooldownTimer = strikeCooldown;

        bool backwards = alongTangent > minTangentDot;
        if (backwards)
        {
            reverseTimer = reverseDuration;
            OnStruckBackwards?.Invoke(this);
        }

        return backwards;
    }

    void OnCollisionStay2D(Collision2D col)
    {
        if (pushForce <= 0f) return;
        if (!col.collider.TryGetComponent<PlayerHealth>(out _)) return;

        Rigidbody2D other = col.collider.attachedRigidbody;
        if (other == null) return;

        Vector2 pivot = rb.position;
        Vector2 arm = other.position - pivot;
        if (arm.sqrMagnitude < 0.0001f) return;

        float sign = Mathf.Sign(rb.angularVelocity);
        Vector2 tangent = new Vector2(-arm.y, arm.x).normalized * sign;
        float speedFactor = Mathf.Clamp01(Mathf.Abs(rb.angularVelocity) / Mathf.Max(1f, maxSpeed));

        other.AddForce(tangent * (pushForce * pushRadiusScale * speedFactor), ForceMode2D.Impulse);
    }

    private void OnCollisionEnter(Collision collision)
    {
        PlayerHealth p = collision.gameObject.GetComponent<PlayerHealth>();
        if (p != null) 
        {
            p.TakeDamage(damageDealt);
        }
    }
}
