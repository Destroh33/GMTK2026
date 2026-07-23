using UnityEngine;

public class SwordWeapon : WeaponBase
{
    [Header("Sword")]
    [SerializeField] private Animator swingAnimator;
    [SerializeField] private Transform playerCenter;
    [SerializeField] private float hitRange = 1.2f;
    [SerializeField] private float hitRadius = 1.1f;
    [SerializeField] private int damage = 1;
    [SerializeField] private float knockbackForce = 10f;
    [SerializeField] private LayerMask hittableLayers;

    private static readonly int SwingHash = Animator.StringToHash("swing");

    void Awake()
    {
        if (swingAnimator == null)
            swingAnimator = GetComponent<Animator>();
        if (playerCenter == null)
            playerCenter = transform.parent != null ? transform.parent.parent : null;
    }

    protected override void Use(Vector2 aimDir)
    {
        if (swingAnimator != null)
            swingAnimator.SetTrigger(SwingHash);

        Vector2 origin = playerCenter != null ? (Vector2)playerCenter.position : (Vector2)transform.position;
        Vector2 center = origin + aimDir.normalized * hitRange;

        Collider2D[] hits = Physics2D.OverlapCircleAll(center, hitRadius, hittableLayers);
        foreach (Collider2D hit in hits)
        {
            if (hit.attachedRigidbody == null) continue;
            Vector2 away = ((Vector2)hit.transform.position - origin).normalized;
            if (away.sqrMagnitude < 0.0001f) away = aimDir.normalized;
            hit.attachedRigidbody.AddForce(away * knockbackForce, ForceMode2D.Impulse);

            if (hit.TryGetComponent<EnemyBase>(out EnemyBase e))
            {
                e.TakeDamage(damage);
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Transform c = playerCenter != null ? playerCenter : (transform.parent != null ? transform.parent.parent : transform);
        if (c == null) c = transform;
        Vector2 origin = c.position;
        Vector2 dir = transform.parent != null ? (Vector2)transform.parent.right : (Vector2)transform.right;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(origin + dir * hitRange, hitRadius);
    }
}
