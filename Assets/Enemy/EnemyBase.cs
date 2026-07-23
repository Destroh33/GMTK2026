using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public abstract class EnemyBase : MonoBehaviour
{
    [Header("Enemy Stats")]
    [SerializeField] protected int maxHealth = 10;
    [SerializeField] protected float moveSpeed = 0.5f;
    [SerializeField] protected int contactDamage = 1;

    protected int health;
    protected Rigidbody2D rb;
    protected Transform target;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        health = maxHealth;
    }

    void OnEnable()
    {
        health = maxHealth;
        target = PlayerRef.Instance?.transform;
    }

    void FixedUpdate()
    {
        // Try to fetch PlayerRef if null
        if (target == null) {
            target = PlayerRef.Instance != null ? PlayerRef.Instance.transform : null;
            
            if (target == null) return;
        }

        Move();
    }

    protected abstract void Move();

    public virtual void TakeDamage(int amount)
    {
        health -= amount;

        if (health <= 0) Die();
    }

    public virtual void Die()
    {
        // TODO
        Destroy(gameObject);
    }

    protected virtual void OnCollisionEnter2D(Collision2D col)
    {
        // Collision with player
        if (col.collider.TryGetComponent<PlayerHealth>(out PlayerHealth p))
        {
            p.TakeDamage(contactDamage);
        }
    }

}
