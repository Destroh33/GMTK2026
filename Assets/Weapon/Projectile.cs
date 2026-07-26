using NUnit.Framework;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    public enum BulletType
    {
        garlic,
        piercing
    };

    [SerializeField] BulletType type;
    [SerializeField] private float lifetime = 2.5f;
    [SerializeField] private float damage = 1f;
    [SerializeField] private float knockbackForce = 3f;
    [SerializeField] private GameObject aoePrefab;
    [SerializeField] private LayerMask staticWallLayer;
    [SerializeField] private LayerMask clockHandLayer;

    private Rigidbody2D rb;
    private Collider2D myCollider;

    StatId DamageStat => type == BulletType.piercing ? StatId.PierceDamage : StatId.GunProjectileDamage;
    StatId KnockbackStat => type == BulletType.piercing ? StatId.PierceKnockback : StatId.GunProjectileKnockback;
    StatId LifetimeStat => type == BulletType.piercing ? StatId.PierceLifetime : StatId.GunProjectileLifetime;

    public void Reflect(Vector2 newDir)
    {
        // Update the rigidbody velocity directly
        rb.linearVelocity = newDir.normalized * rb.linearVelocity.magnitude;
    }

    public void Launch(Vector2 direction, float speed)
    {
        // Safety check: if Launch is called immediately after Instantiate, 
        // Awake might not have fired yet.
        if (rb == null) rb = GetComponent<Rigidbody2D>();

        rb.linearVelocity = direction * speed;
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        myCollider = GetComponent<Collider2D>();
    }

    void Start()
    {
        damage = PlayerStats.Damage(damage, DamageStat);
        knockbackForce *= PlayerStats.Mult(KnockbackStat);

        Destroy(gameObject, lifetime * PlayerStats.Mult(LifetimeStat));
    }

    // Update() has been completely removed. 
    // The Rigidbody2D handles movement automatically now.

    void OnCollisionEnter2D(Collision2D col)
    {
        if (type == BulletType.garlic)
        {
            // projectile hit enemy
            EnemyBase hitEnemy = null;
            Boss hitBoss = null;
            if (col.collider.TryGetComponent<EnemyBase>(out var e))
            {
                e.TakeDamage(damage, rb.linearVelocity.normalized * knockbackForce);
                hitEnemy = e;
            }
            else if ((hitBoss = col.collider.GetComponentInParent<Boss>()) != null)
            {
                hitBoss.TakeDamage(damage, rb.linearVelocity.normalized * knockbackForce);
            }

            if (aoePrefab != null)
            {
                // spawn AOE
                GameObject aoeObj = Instantiate(aoePrefab, transform.position, Quaternion.identity);
                aoeObj.GetComponent<AOE>().Init(hitEnemy);
            }

            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (type == BulletType.piercing)
        {
            // for piercing bullets
            EnemyBase hitEnemy = null;
            Boss hitBoss = null;
            if (collision.TryGetComponent<EnemyBase>(out var e))
            {
                e.TakeDamage(damage, rb.linearVelocity.normalized * knockbackForce);
                hitEnemy = e;
            }
            else if ((hitBoss = collision.GetComponentInParent<Boss>()) != null)
            {
                hitBoss.TakeDamage(damage, rb.linearVelocity.normalized * knockbackForce);
            }

            int bounceLayers = staticWallLayer.value | clockHandLayer.value;

            if (((1 << collision.gameObject.layer) & bounceLayers) != 0)
            {
                Vector2 currentVel = rb.linearVelocity;
                Vector2 currentDir = currentVel.normalized;
                Vector2 normal;

                // 1. Raycast backward to find the exact wall surface hit
                float rayDistance = currentVel.magnitude * Time.fixedDeltaTime * 3f;
                Vector2 startPos = (Vector2)transform.position - (currentDir * rayDistance);

                RaycastHit2D hit = Physics2D.Raycast(startPos, currentDir, rayDistance * 2f, bounceLayers);

                if (hit.collider != null)
                {
                    normal = hit.normal;
                }
                else
                {
                    // Fallback just in case
                    ColliderDistance2D distanceInfo = myCollider.Distance(collision);
                    normal = -distanceInfo.normal;
                }

                // 2. Reflect velocity off the surface
                Vector2 newVelocity = Vector2.Reflect(currentVel, normal);

                // 3. Optional: add slight randomness to direction (±10 degrees)
                float randomAngle = Random.Range(-10f, 10f) * Mathf.Deg2Rad;
                float cos = Mathf.Cos(randomAngle);
                float sin = Mathf.Sin(randomAngle);

                // 4. Apply back to Rigidbody
                rb.linearVelocity = new Vector2(
                    newVelocity.x * cos - newVelocity.y * sin,
                    newVelocity.x * sin + newVelocity.y * cos
                );
            }
        }
    }
}