using System;
using UnityEngine;

public class PlayerHealth : PlayerBehaviors
{
    [Header("Health")]
    [SerializeField] float maxHealth = 8f;

    [Header("Stun")]
    [SerializeField] float hitStunTime = 0.5f;
    [SerializeField] float hitStunStrength = 0.1f;

    float health;
    float currentMax;
    bool subscribed;

    public float Health => health;
    public float MaxHealth => currentMax;

    public event Action<float, float> OnHealthChanged;

    void Awake()
    {
        currentMax = maxHealth;
        health = currentMax;
        NotifyHealthChanged();
        SetFlashInfo();
        SetVignetteInfo();
    }

    void NotifyHealthChanged()
    {
        OnHealthChanged?.Invoke(health, currentMax);
    }
    protected override void SetFlashInfo()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        baseColor = spriteRenderer.color;
        originalMaterial = spriteRenderer.material;
        flashMaterial = Resources.Load<Material>("Materials/WhiteFlash");
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

        if (subscribed && GameManager.Instance != null)
            GameManager.Instance.OnRunReset -= HandleRunReset;

        subscribed = false;
    }

    void TrySubscribe()
    {
        if (subscribed) return;
        if (PlayerStats.Instance == null || GameManager.Instance == null) return;

        PlayerStats.Instance.OnPathUpgraded += HandlePathUpgraded;
        GameManager.Instance.OnRunReset += HandleRunReset;
        subscribed = true;
    }

    void HandlePathUpgraded(UpgradePath path, int level, bool rare)
    {
        if (rare) return;

        float newMax = Mathf.Max(1f, maxHealth * PlayerStats.Mult(StatId.BodyMaxHealth));
        float delta = newMax - currentMax;

        currentMax = newMax;

        if (delta > 0f) health += delta;
        if (health > currentMax) health = currentMax;
    }

    void HandleRunReset()
    {
        currentMax = maxHealth;
        health = currentMax;

        NotifyHealthChanged();
    }

    public void TakeDamage(float amount)
    {
        health -= amount;

        health = Mathf.Max(health, 0f);

        NotifyHealthChanged();

        GameManager.Instance.GameSpeed(hitStunStrength, hitStunTime, true);

        if (health <= 0f)
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
        GameManager.Instance?.HandlePlayerDied();
    }
}
