using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyProjectile : MonoBehaviour
{
    [SerializeField] private float lifetime = 4f;
    [SerializeField] private float damage = 1f;
    [SerializeField] private bool destroyOnAnyHit = true;

    [Header("Riposte")]
    [SerializeField] private float reflectSpeedBonus = 1.4f;
    [SerializeField] private float reflectKnockback = 3f;
    [SerializeField] private Color reflectTint = new Color(1f, 0.85f, 0.25f, 1f);

    Vector2 velocity;
    bool launched;
    bool reflected;
    float reflectedDamage;

    public Vector2 Velocity => velocity;
    public bool IsReflected => reflected;

    public void Launch(Vector2 direction, float speed)
    {
        AudioManager.Instance?.PlayEnemyShootSFX();

        velocity = direction.normalized * speed;
        launched = true;

        FaceVelocity();
    }

    public bool Reflect(float newDamage)
    {
        if (velocity.sqrMagnitude < 0.0001f) return false;

        return Reflect(-velocity.normalized, newDamage);
    }

    public bool Reflect(Vector2 newDirection, float newDamage)
    {
        if (reflected) return false;
        if (newDirection.sqrMagnitude < 0.0001f) return false;

        AudioManager.Instance?.PlayReflectSFX();

        reflected = true;
        reflectedDamage = Mathf.Max(0f, newDamage);

        float speed = velocity.magnitude * reflectSpeedBonus;
        velocity = newDirection.normalized * speed;
        launched = true;

        int playerProjectileLayer = LayerMask.NameToLayer("PlayerProjectile");
        if (playerProjectileLayer >= 0) gameObject.layer = playerProjectileLayer;

        if (TryGetComponent(out SpriteRenderer sr)) sr.color = reflectTint;

        FaceVelocity();
        return true;
    }

    void FaceVelocity()
    {
        if (velocity.sqrMagnitude < 0.0001f) return;

        float angle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    void Start()
    {
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        if (launched)
            transform.position += (Vector3)(velocity * Time.deltaTime);
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        if (reflected)
        {
            if (col.collider.TryGetComponent<EnemyBase>(out EnemyBase e))
            {
                e.TakeDamage(reflectedDamage, velocity.normalized * reflectKnockback);
                Destroy(gameObject);
                return;
            }

            if (col.collider.GetComponent<PlayerHealth>() != null) return;

            if (destroyOnAnyHit) Destroy(gameObject);
            return;
        }

        if (col.collider.TryGetComponent<PlayerHealth>(out PlayerHealth p))
        {
            p.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        if (destroyOnAnyHit)
            Destroy(gameObject);
    }
}
