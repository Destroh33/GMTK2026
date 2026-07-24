using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyProjectile : MonoBehaviour
{
    [SerializeField] private float lifetime = 4f;
    [SerializeField] private int damage = 1;
    [SerializeField] private bool destroyOnAnyHit = true;

    Vector2 velocity;
    bool launched;

    public void Launch(Vector2 direction, float speed)
    {
        velocity = direction.normalized * speed;
        launched = true;

        if (velocity.sqrMagnitude > 0.0001f)
        {
            float angle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
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
