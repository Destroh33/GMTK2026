using UnityEngine;
using UnityEngine.AI;

//[RequireComponent(typeof(Rigidbody2D))]
public abstract class EnemyBase : MonoBehaviour
{
    [Header("Enemy Stats")]
    [SerializeField] protected int maxHealth = 10;
    [SerializeField] protected float moveSpeed = 0.5f;
    [SerializeField] protected int contactDamage = 1;
    [SerializeField] protected float attackCooldown = 1f;

    protected int health;
    //protected Rigidbody2D rb;
    protected NavMeshAgent agent;
    protected Transform target;
    protected float currAttackCooldown;

    void Awake()
    {
        //rb = GetComponent<Rigidbody2D>();
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false;
        agent.updateUpAxis = false;
        agent.speed = moveSpeed;
        agent.stoppingDistance = 0.5f;

        health = maxHealth;
        currAttackCooldown = 0f;
    }

    void OnEnable()
    {
        health = maxHealth;
        target = PlayerRef.Instance?.transform;
        currAttackCooldown = 0f;
    }

    void FixedUpdate()
    {
        // Try to fetch PlayerRef if null
        if (target == null) {
            target = PlayerRef.Instance != null ? PlayerRef.Instance.transform : null;
            
            if (target == null) return;
        }

        // Decrease attack cooldown if present
        if (currAttackCooldown > 0) currAttackCooldown -= Time.deltaTime;
        else if (currAttackCooldown < 0) currAttackCooldown = 0f;

        Move();
    }

    /*void Update() {
        agent.destination = target.position;
    }*/

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

    protected virtual void AttackPlayer(PlayerHealth p)
    {
        // Ignore if on cooldown
        if (currAttackCooldown > 0) return;

        p.TakeDamage(contactDamage);
        currAttackCooldown = attackCooldown;
    }

    protected virtual void OnCollisionEnter2D(Collision2D col)
    {
        // Collision with player
        if (col.collider.TryGetComponent<PlayerHealth>(out PlayerHealth p))
        {
            AttackPlayer(p);
        }
    }

    protected virtual void OnCollisionStay2D(Collision2D col)
    {
        // Collision with player
        if (col.collider.TryGetComponent<PlayerHealth>(out PlayerHealth p))
        {
            AttackPlayer(p);
        }
    }

}
