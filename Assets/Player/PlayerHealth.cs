using UnityEngine;

public class PlayerHealth : PlayerBehaviors
{
    [Header("Health")]
    [SerializeField] int maxHealth = 8;
    [Header("Stun")]
    [SerializeField] float hitStunTime = 0.5f;
    [SerializeField] float hitStunStrength = 0.1f;

    int health;

    void Awake()
    {
        health = maxHealth;
        SetFlashInfo();
    }

    protected override void SetFlashInfo()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        baseColor = spriteRenderer.color;
    }

    public void TakeDamage(int amount) {
        health -= amount;
        GameManager.Instance.GameSpeed(hitStunStrength, hitStunTime, true);
        FlashEntity();

        if (health <= 0)
        {
            Die();
        } 
        else
        {
            FlashEntity();
        }
    }

    public void Heal(int amount)
    {
        if (health > maxHealth) health = maxHealth;
    }

    private void Die()
    {
        // TODO
    }

}
