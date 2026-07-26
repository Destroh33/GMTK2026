using UnityEngine;

public class ShieldEnemy : FollowerEnemy
{
    private float timeSinceBullet = 0f;
    private bool shieldUp = false;

    private Animator animator;

    protected override void Awake()
    {
        base.Awake();
        animator = GetComponent<Animator>();
    }

    public override void TakeDamage(float amount, Vector2 knockbackImpulse)
    {
        if (shieldUp) return;
        base.TakeDamage(amount, knockbackImpulse);
    }

    protected override void Move()
    {
        if (shieldUp)
        {
            rb.linearVelocity = Vector2.zero;
        }

        base.Move();
    }

    protected void SetShield(bool state)
    {
        if (state == shieldUp) return;

        shieldUp = state;

        animator.SetBool("shield_up", shieldUp);
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate();
        
        timeSinceBullet += Time.fixedDeltaTime;

        if (timeSinceBullet > 0.5)
        {
            SetShield(false);
        }

        spriteRenderer.flipX = rb.linearVelocity.x > 0;

    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("PlayerProjectile"))
        {
            // Bullet detected, raise shield
            timeSinceBullet = 0;
            SetShield(true);
        }
    }

    public void OnChildTrigger(Collider2D other)
    {
        if (other.TryGetComponent<Projectile>(out var p))
        {
            // Bullet hits shield, reflect
            p.Reflect((Vector2)other.transform.position - rb.position);
        }
    }
}