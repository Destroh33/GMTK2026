using System.Collections.Generic;
using UnityEngine;

public class SwordWeapon : WeaponBase
{
    [Header("Sword")]
    [SerializeField] private Animator swingAnimator;
    [SerializeField] private Transform playerCenter;

    [Header("Hitbox")]
    [SerializeField] private float hitLength = 0.9f;
    [SerializeField] private float hitWidth = 0.7f;
    [SerializeField] private float hitDuration = 0.3f;
    [SerializeField] private bool hitboxFollowsAim = true;

    [Header("Damage")]
    [SerializeField] private int damage = 1;
    [SerializeField] private float knockbackForce = 4f;
    [SerializeField] private LayerMask hittableLayers;

    private static readonly int SwingHash = Animator.StringToHash("swing");

    private readonly HashSet<Object> hitThisSwing = new HashSet<Object>();

    private Vector2 swingDir = Vector2.right;
    private float activeTimer;
    private bool swinging;

    void Awake()
    {
        if (swingAnimator == null)
            swingAnimator = GetComponent<Animator>();
        if (playerCenter == null)
            playerCenter = transform.parent != null ? transform.parent.parent : null;
    }

    protected override float CooldownMultiplier() => PlayerStats.Mult(StatId.SwordCooldown);

    protected override void Update()
    {
        base.Update();

        if (!swinging) return;

        activeTimer -= Time.deltaTime;

        if (activeTimer <= 0f)
        {
            swinging = false;
            hitThisSwing.Clear();
            return;
        }

        SweepHitbox();
    }

    protected override void Use(Vector2 aimDir)
    {
        if (swingAnimator != null)
            swingAnimator.SetTrigger(SwingHash);

        if (aimDir.sqrMagnitude > 0.0001f)
            swingDir = aimDir.normalized;

        hitThisSwing.Clear();
        activeTimer = hitDuration;
        swinging = true;

        SweepHitbox();
    }

    void SweepHitbox()
    {
        float reach = PlayerStats.Mult(StatId.SwordReach);
        float length = hitLength * reach;
        float width = hitWidth * reach;
        int scaledDamage = PlayerStats.Damage(damage, StatId.SwordDamage);
        float scaledKnockback = knockbackForce * PlayerStats.Mult(StatId.SwordKnockback);

        Vector2 origin = Origin();
        Vector2 dir = CurrentDir();
        Vector2 center = origin + dir * (length * 0.5f);
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        Collider2D[] hits = Physics2D.OverlapBoxAll(center, new Vector2(length, width), angle, hittableLayers);

        foreach (Collider2D hit in hits)
        {
            if (hit.attachedRigidbody == null) continue;

            Vector2 away = ((Vector2)hit.transform.position - origin).normalized;
            if (away.sqrMagnitude < 0.0001f) away = dir;

            if (hit.TryGetComponent<EnemyBase>(out EnemyBase e))
            {
                if (!hitThisSwing.Add(e)) continue;
                e.TakeDamage(scaledDamage, away * scaledKnockback);
            }
            else if (hit.attachedRigidbody.TryGetComponent<ClockHand>(out ClockHand hand))
            {
                if (!hitThisSwing.Add(hand)) continue;
                hand.TryStrike(hit.ClosestPoint(center), origin);
            }
            else
            {
                if (!hitThisSwing.Add(hit.attachedRigidbody)) continue;
                hit.attachedRigidbody.AddForce(away * scaledKnockback, ForceMode2D.Impulse);
            }
        }
    }

    Vector2 Origin()
    {
        return playerCenter != null ? (Vector2)playerCenter.position : (Vector2)transform.position;
    }

    Vector2 CurrentDir()
    {
        if (hitboxFollowsAim && transform.parent != null)
        {
            Vector2 aim = transform.parent.right;
            if (aim.sqrMagnitude > 0.0001f) return aim.normalized;
        }

        return swingDir;
    }

    void OnDrawGizmosSelected()
    {
        Transform c = playerCenter != null ? playerCenter : (transform.parent != null ? transform.parent.parent : transform);
        if (c == null) c = transform;

        Vector2 origin = c.position;
        Vector2 dir = transform.parent != null ? (Vector2)transform.parent.right : (Vector2)transform.right;
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;
        dir = dir.normalized;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        Gizmos.color = Color.red;
        Gizmos.matrix = Matrix4x4.TRS(origin + dir * (hitLength * 0.5f), Quaternion.Euler(0f, 0f, angle), Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(hitLength, hitWidth, 0.01f));
    }
}
