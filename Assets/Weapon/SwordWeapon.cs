using System.Collections.Generic;
using UnityEngine;

public class SwordWeapon : WeaponBase
{
    [Header("Sword")]
    [SerializeField] private Animator swingAnimator;
    [SerializeField] private Transform playerCenter;
    [SerializeField] private SwordSlash slash;
    [SerializeField] private float swingClipLength = 0.42f;
    [SerializeField] private Transform bladeVisual;
    [Range(0f, 1f)][SerializeField] private float reachVisualFactor = 0.5f;

    [Header("Hitbox")]
    [SerializeField] private float hitLength = 1.35f;
    [SerializeField] private float hitWidth = 1.05f;
    [SerializeField] private float hitDuration = 0.3f;
    [SerializeField] private bool hitboxFollowsAim = true;

    [Header("Damage")]
    [SerializeField] private float damage = 3f;
    [SerializeField] private float knockbackForce = 4f;
    [SerializeField] private LayerMask hittableLayers;

    [Header("Riposte")]
    [SerializeField] private LayerMask riposteLayers;
    [SerializeField] private float riposteSizeBonusPerLevel = 0.3f;
    [SerializeField] private float riposteBaseDamage = 2f;
    [SerializeField] private float riposteDamagePerLevel = 2f;

    private static readonly int SwingHash = Animator.StringToHash("swing");

    private readonly HashSet<Object> hitThisSwing = new HashSet<Object>();

    private Vector2 swingDir = Vector2.right;
    private float activeTimer;
    private bool swinging;
    private Vector3 bladeBaseScale = Vector3.one;
    private bool hasBladeBaseScale;

    void Awake()
    {
        if (swingAnimator == null)
            swingAnimator = GetComponent<Animator>();
        if (playerCenter == null)
            playerCenter = transform.parent != null ? transform.parent.parent : null;

        if (bladeVisual != null)
        {
            bladeBaseScale = bladeVisual.localScale;
            hasBladeBaseScale = true;
        }
    }

    void ApplyReachVisual()
    {
        if (bladeVisual == null || !hasBladeBaseScale) return;

        float reach = PlayerStats.Mult(StatId.SwordReach);
        float k = 1f + (reach - 1f) * reachVisualFactor;

        bladeVisual.localScale = bladeBaseScale * k;
    }

    protected override float CooldownMultiplier() => PlayerStats.Mult(StatId.SwordCooldown);

    protected override void Update()
    {
        base.Update();

        ApplyReachVisual();

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
        float currentCooldown = cooldown * CooldownMultiplier();
        float swingTime = Mathf.Min(hitDuration, currentCooldown);

        if (swingAnimator != null)
        {
            swingAnimator.speed = swingClipLength > 0f && swingTime > 0f
                ? swingClipLength / swingTime
                : 1f;
            swingAnimator.SetTrigger(SwingHash);
        }

        if (aimDir.sqrMagnitude > 0.0001f)
            swingDir = aimDir.normalized;

        hitThisSwing.Clear();
        activeTimer = swingTime;
        swinging = true;

        if (slash != null) slash.Play(swingTime);

        SweepHitbox();
    }

    void SweepHitbox()
    {
        float reach = PlayerStats.Mult(StatId.SwordReach);
        float length = hitLength * reach;
        float width = hitWidth * reach;
        float scaledDamage = PlayerStats.Damage(damage, StatId.SwordDamage);
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

        RiposteSweep(origin, dir, length, width, angle);
    }

    void RiposteSweep(Vector2 origin, Vector2 dir, float length, float width, float angle)
    {
        int level = PlayerStats.Rare(UpgradePath.Sword);
        if (level <= 0 || riposteLayers.value == 0) return;

        float scale = 1f + riposteSizeBonusPerLevel * (level - 1);
        float boxLength = length * scale;
        float boxWidth = width * scale;

        Vector2 center = origin + dir * (boxLength * 0.5f);
        Collider2D[] hits = Physics2D.OverlapBoxAll(center, new Vector2(boxLength, boxWidth), angle, riposteLayers);

        float reflectDamage = PlayerStats.Damage(
            riposteBaseDamage + riposteDamagePerLevel * (level - 1), StatId.SwordDamage);

        foreach (Collider2D hit in hits)
        {
            if (!hit.TryGetComponent<EnemyProjectile>(out EnemyProjectile bullet)) continue;
            if (bullet.IsReflected) continue;
            if (!hitThisSwing.Add(bullet)) continue;

            bullet.Reflect(reflectDamage);
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
