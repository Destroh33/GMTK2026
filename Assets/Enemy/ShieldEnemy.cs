using UnityEngine;

public class ShieldEnemy : FollowerEnemy
{
    private float timeSinceBullet = 0f;
    private bool shieldUp = false;

    protected void SetShield(bool state)
    {
        if (state == shieldUp) return;

        shieldUp = state;

        if (state)
            spriteRenderer.color = Color.blue;
        else
            spriteRenderer.color = baseColor;
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate();
        
        timeSinceBullet += Time.fixedDeltaTime;

        if (timeSinceBullet > 0.5)
        {
            SetShield(false);
        }

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