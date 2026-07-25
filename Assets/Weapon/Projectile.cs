using NUnit.Framework;
using UnityEngine;
public class Projectile : MonoBehaviour
{
    [SerializeField] private float lifetime = 2.5f;
    [SerializeField] private int damage = 1;
    [SerializeField] private float knockbackForce = 3f;
    [SerializeField] private GameObject aoePrefab;

    private Rigidbody2D rb;
    private Vector2 velocity;
    private bool launched;

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
        // projectile hit enemy
        EnemyBase hitEnemy = null;
        if (col.collider.TryGetComponent<EnemyBase>(out var e))
        {
            e.TakeDamage(damage, velocity.normalized * knockbackForce);
            hitEnemy = e;
        }

        // spawn AOE
        GameObject aoeObj = Instantiate(aoePrefab, transform.position, Quaternion.identity);
        aoeObj.GetComponent<AOE>().Init(hitEnemy);

        Destroy(gameObject);
    }
}
