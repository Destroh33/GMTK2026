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
    [SerializeField] private int damage = 1;
    [SerializeField] private float knockbackForce = 3f;
    [SerializeField] private GameObject aoePrefab;
    [SerializeField] private LayerMask staticWallLayer;
    [SerializeField] private LayerMask clockHandLayer;

    private Rigidbody2D rb;
    private Vector2 velocity;
    private bool launched;
    private Collider2D myCollider;

    public void Reflect(Vector2 newDir)
    {
        velocity = newDir.normalized * velocity.magnitude;

        rb.linearVelocity = newDir.normalized * rb.linearVelocity.magnitude;
    }

    public void Launch(Vector2 direction, float speed)
    {
        velocity = direction * speed;
        launched = true;
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        // Cache the bullet's own collider
        myCollider = GetComponent<Collider2D>();
    }

    void Start()
    {
        damage = PlayerStats.Damage(damage, StatId.GunProjectileDamage);
        knockbackForce *= PlayerStats.Mult(StatId.GunProjectileKnockback);

        Destroy(gameObject, lifetime * PlayerStats.Mult(StatId.GunProjectileLifetime));
    }

    void Update()
    {
        if (launched)
            transform.position += (Vector3)(velocity * Time.deltaTime);
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        if (type == BulletType.garlic) 
        {
            // projectile hit enemy
            EnemyBase hitEnemy = null;
            if (col.collider.TryGetComponent<EnemyBase>(out var e))
            {
                e.TakeDamage(damage, velocity.normalized * knockbackForce);
                hitEnemy = e;
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
            if (collision.TryGetComponent<EnemyBase>(out var e))
            {
                e.TakeDamage(damage, velocity.normalized * knockbackForce);
                hitEnemy = e;
            }

            if (((1 << collision.gameObject.layer) & staticWallLayer.value) != 0 ||
    ((1 << collision.gameObject.layer) & clockHandLayer.value) != 0)
            {
                // Debug.Log("I wanna bounce");
                // 1. Calculate the physical relationship between the bullet and the wall
                ColliderDistance2D distanceInfo = myCollider.Distance(collision);

                // 2. Get the normal. We negate it (-) so it points AWAY from the wall, 
                // which is required for Vector2.Reflect to bounce it correctly.
                Vector2 normal = -distanceInfo.normal;

                // 3. Reflect velocity off the surface
                velocity = Vector2.Reflect(velocity, normal);

                // Optional: add slight randomness to direction (�10 degrees)
                float randomAngle = Random.Range(-10f, 10f) * Mathf.Deg2Rad;
                float cos = Mathf.Cos(randomAngle);
                float sin = Mathf.Sin(randomAngle);
                velocity = new Vector2(
                    velocity.x * cos - velocity.y * sin,
                    velocity.x * sin + velocity.y * cos
                );
            }
        }
    }
}
