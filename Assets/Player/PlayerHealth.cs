using UnityEngine;

public class PlayerHealth : PlayerBehaviors
{
    [Header("Health")]
    [SerializeField] int maxHealth = 5;

    [Header("Stun")]
    [SerializeField] float hitStunTime = 0.5f;
    [SerializeField] float hitStunStrength = 0.1f;

    int health;
    int currentMax;
    bool subscribed;

    public int Health => health;
    public int MaxHealth => currentMax;

    void Awake()
    {
        currentMax = maxHealth;
        health = currentMax;
        SetFlashInfo();
        SetVignetteInfo();
    }

    protected override void SetFlashInfo()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        baseColor = spriteRenderer.color;
    }

    void Start()
    {
        TrySubscribe();
    }

    void OnEnable()
    {
        TrySubscribe();
    }

    void OnDisable()
    {
        if (subscribed && PlayerStats.Instance != null)
            PlayerStats.Instance.OnPathUpgraded -= HandlePathUpgraded;

        subscribed = false;
    }

    void TrySubscribe()
    {
        if (subscribed || PlayerStats.Instance == null) return;

        PlayerStats.Instance.OnPathUpgraded += HandlePathUpgraded;
        subscribed = true;
    }

    void HandlePathUpgraded(UpgradePath path, int level)
    {
        int newMax = Mathf.Max(1, Mathf.FloorToInt(maxHealth * PlayerStats.Mult(StatId.BodyMaxHealth) + 0.5f));
        int delta = newMax - currentMax;

        currentMax = newMax;

        if (delta > 0) health += delta;
        if (health > currentMax) health = currentMax;
    }

    public void TakeDamage(int amount)
    {
        health -= amount;
        GameManager.Instance.GameSpeed(hitStunStrength, hitStunTime, true);

        if (health <= 0)
        {
            Die();
        } 
        else
        {
            FlashEntity();
            DoVignette();
        }
    }

    private void Die()
    {
        // TODO
    }
}
