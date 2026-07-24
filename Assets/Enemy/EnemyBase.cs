using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody2D))]
public abstract class EnemyBase : MonoBehaviour
{
    public enum State { Chasing, Knockback, Stunned }

    [Header("Enemy Stats")]
    [SerializeField] protected int maxHealth = 10;
    [SerializeField] protected float moveSpeed = 2f;
    [SerializeField] protected int contactDamage = 1;
    [SerializeField] protected float attackCooldown = 1f;

    [Header("Movement Feel")]
    [SerializeField] protected float acceleration = 30f;

    [Header("Knockback")]
    [SerializeField] protected float knockbackExitSpeed = 1.5f;
    [SerializeField] protected float maxKnockbackDuration = 0.6f;
    [SerializeField] protected float knockbackDrag = 6f;

    [Header("UI")]
    [SerializeField] protected GameObject healthBar;
    [SerializeField] protected Image healthFill;

    protected int health;
    protected float timeSinceDamage;
    protected Rigidbody2D rb;
    protected Transform target;
    protected float currAttackCooldown;

    protected State state = State.Chasing;
    float stateTimer;
    bool died;

    public State CurrentState => state;
    public bool IsAlive => health > 0;

    public event System.Action<EnemyBase> OnDied;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        health = maxHealth;
        currAttackCooldown = 0f;
    }

    protected virtual void OnEnable()
    {
        health = maxHealth;
        target = PlayerRef.Instance != null ? PlayerRef.Instance.transform : null;
        currAttackCooldown = 0f;
        state = State.Chasing;
        stateTimer = 0f;
        died = false;
    }

    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        timeSinceDamage += dt;

        if (currAttackCooldown > 0f)
        {
            currAttackCooldown -= dt;
            if (currAttackCooldown < 0f) currAttackCooldown = 0f;
        }

        switch (state)
        {
            case State.Knockback:
                stateTimer -= dt;
                rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, Vector2.zero, knockbackDrag * dt);
                if (stateTimer <= 0f || rb.linearVelocity.magnitude <= knockbackExitSpeed)
                    state = State.Chasing;
                return;

            case State.Stunned:
                stateTimer -= dt;
                rb.linearVelocity = Vector2.zero;
                if (stateTimer <= 0f) state = State.Chasing;
                return;
        }

        if (target == null)
        {
            target = PlayerRef.Instance != null ? PlayerRef.Instance.transform : null;
            if (target == null)
            {
                rb.linearVelocity = Vector2.zero;
                return;
            }
        }

        Move();

        // Turn off health bar after 3 seconds
        if (timeSinceDamage > 3 && healthBar.activeSelf)
        {
            healthBar.SetActive(false);
        }
    }

    protected abstract void Move();

    protected void MoveInDirection(Vector2 direction)
    {
        Vector2 desired = direction * moveSpeed;
        rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, desired, acceleration * Time.fixedDeltaTime);
    }

    public virtual void TakeDamage(int amount)
    {
        TakeDamage(amount, Vector2.zero);
        timeSinceDamage = 0;
        
        // Turn on and update health bar
        healthBar.SetActive(true);
        healthFill.fillAmount = (float)health / maxHealth;
    }

    public virtual void TakeDamage(int amount, Vector2 knockbackImpulse)
    {
        if (!IsAlive) return;

        health -= amount;

        if (health <= 0)
        {
            Die();
            return;
        }

        if (knockbackImpulse.sqrMagnitude > 0.0001f)
            ApplyKnockback(knockbackImpulse);
    }

    public virtual void ApplyKnockback(Vector2 impulse)
    {
        if (!IsAlive) return;

        rb.linearVelocity = Vector2.zero;
        rb.AddForce(impulse, ForceMode2D.Impulse);

        state = State.Knockback;
        stateTimer = maxKnockbackDuration;
    }

    public virtual void ApplyStun(float duration)
    {
        if (!IsAlive || duration <= 0f) return;

        rb.linearVelocity = Vector2.zero;
        state = State.Stunned;
        stateTimer = Mathf.Max(stateTimer, duration);
    }

    public virtual void Die()
    {
        if (died) return;
        died = true;

        OnDied?.Invoke(this);

        // TODO: death VFX / drops
        Destroy(gameObject);
    }

    protected virtual void AttackPlayer(PlayerHealth p)
    {
        if (currAttackCooldown > 0f) return;

        p.TakeDamage(contactDamage);
        currAttackCooldown = attackCooldown;
    }

    protected virtual void OnCollisionEnter2D(Collision2D col)
    {
        if (col.collider.TryGetComponent<PlayerHealth>(out PlayerHealth p))
            AttackPlayer(p);
    }

    protected virtual void OnCollisionStay2D(Collision2D col)
    {
        if (col.collider.TryGetComponent<PlayerHealth>(out PlayerHealth p))
            AttackPlayer(p);
    }
}
