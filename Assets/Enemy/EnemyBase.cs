using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody2D))]
public abstract class EnemyBase : EnemyBehaviors
{
    public enum State { Chasing, Knockback, Stunned }

    [Header("Enemy Stats")]
    [SerializeField] protected float maxHealth = 10f;
    [SerializeField] protected float moveSpeed = 2f;
    [SerializeField] protected float contactDamage = 1f;
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

    [Header("Audio")]
    [SerializeField] protected AudioClip deathSFX;

    protected float health;
    protected float timeSinceDamage;
    protected Rigidbody2D rb;
    protected Transform target;
    protected float currAttackCooldown;

    protected State state = State.Chasing;
    float stateTimer;
    bool died;

    public State CurrentState => state;
    public bool IsAlive => health > 0f;

    public event System.Action<EnemyBase> OnDied;

    public static event System.Action<EnemyBase> OnAnyDied;

    float tarTimer;
    float tarSlow = 1f;
    float tarAmp = 1f;

    public virtual bool ImmuneToAreaEffects => false;

    public bool IsTarred => tarTimer > 0f;
    protected float TarSpeedMultiplier => tarTimer > 0f ? tarSlow : 1f;
    public float TarDamageAmp => tarTimer > 0f ? tarAmp : 1f;

    public void ApplyTar(float slowMultiplier, float damageAmp, float duration)
    {
        if (!IsAlive || duration <= 0f || ImmuneToAreaEffects) return;

        if (tarTimer <= 0f)
        {
            tarSlow = slowMultiplier;
            tarAmp = damageAmp;
        }
        else
        {
            tarSlow = Mathf.Min(tarSlow, slowMultiplier);
            tarAmp = Mathf.Max(tarAmp, damageAmp);
        }

        tarTimer = Mathf.Max(tarTimer, duration);
    }

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        health = maxHealth;
        currAttackCooldown = 0f;

        SetFlashInfo();
    }

    protected virtual void OnEnable()
    {
        health = maxHealth;
        target = PlayerRef.Instance != null ? PlayerRef.Instance.transform : null;
        currAttackCooldown = 0f;
        state = State.Chasing;
        stateTimer = 0f;
        died = false;
        tarTimer = 0f;
        tarSlow = 1f;
        tarAmp = 1f;
    }

    protected virtual void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        timeSinceDamage += dt;

        if (tarTimer > 0f) tarTimer -= dt;

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
        if (healthBar != null && timeSinceDamage > 3 && healthBar.activeSelf)
        {
            healthBar.SetActive(false);
        }
    }

    protected abstract void Move();

    protected void MoveInDirection(Vector2 direction)
    {
        Vector2 desired = direction * moveSpeed * TarSpeedMultiplier;
        rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, desired, acceleration * Time.fixedDeltaTime);
    }

    public virtual void TakeDamage(float amount)
    {
        TakeDamage(amount, Vector2.zero);
    }

    public virtual void TakeDamage(float amount, Vector2 knockbackImpulse)
    {
        if (!IsAlive) return;

        AudioManager.Instance?.PlayHitSFX();

        if (tarTimer > 0f && tarAmp > 1f)
            amount *= tarAmp;

        health -= amount;
        timeSinceDamage = 0;

        if (health <= 0f)
        {
            Die();
            return;
        }

        UpdateHealthBar();

        if (knockbackImpulse.sqrMagnitude > 0.0001f)
            ApplyKnockback(knockbackImpulse);
        
        FlashEntity();
    }

    void UpdateHealthBar()
    {
        if (healthBar != null) healthBar.SetActive(true);
        if (healthFill != null) healthFill.fillAmount = maxHealth > 0f ? health / maxHealth : 0f;
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

        AudioManager.Instance?.PlayEnemyDeathSFX(deathSFX);

        OnDied?.Invoke(this);
        OnAnyDied?.Invoke(this);

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
