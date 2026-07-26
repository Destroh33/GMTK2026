using System;
using UnityEngine;

public class PlayerHealth : PlayerBehaviors
{
    [Header("Health")]
    [SerializeField] float maxHealth = 8f;

    [Header("Stun")]
    [SerializeField] float hitStunTime = 0.16f;
    [SerializeField] float hitStunStrength = 0.35f;

    [Header("Invulnerability")]
    [SerializeField] float invulnerabilityTime = 0.8f;
    [SerializeField] float blinkInterval = 0.07f;

    [Header("Particle Effect")]
    [SerializeField] ParticleSystem onDamageParticles;

    float health;
    float currentMax;
    bool subscribed;
    float invulnTimer;
    PlayerMovement movement;

    public bool IsInvulnerable => invulnTimer > 0f || (movement != null && movement.IsInvulnerable);

    public float Health => health;
    public float MaxHealth => currentMax;

    public event Action<float, float> OnHealthChanged;

    public DeathScreen deathScreen;
    private bool isDead = false;

    void Awake()
    {
        movement = GetComponent<PlayerMovement>();
        currentMax = maxHealth;
        health = currentMax;
        NotifyHealthChanged();
        SetFlashInfo();
        SetVignetteInfo();

        if (deathScreen == null)
            deathScreen = FindAnyObjectByType<DeathScreen>();
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

    void Update()
    {
        if (invulnTimer <= 0f) return;

        invulnTimer -= Time.deltaTime;

        if (spriteRenderer == null) return;

        if (invulnTimer <= 0f)
            spriteRenderer.enabled = true;
        else
            spriteRenderer.enabled = Mathf.FloorToInt(invulnTimer / blinkInterval) % 2 == 0;
    }

    public bool Heal(float amount)
    {
        if (amount <= 0f || health >= currentMax) return false;

        health = Mathf.Min(health + amount, currentMax);
        NotifyHealthChanged();
        return true;
    }

    public void TakeDamage(float amount)
    {
        if (IsInvulnerable) return;

        AudioManager.Instance?.PlayHitSFX();

        invulnTimer = invulnerabilityTime;
        health -= amount;

        health = Mathf.Max(health, 0f);

        NotifyHealthChanged();

        GameManager.Instance.GameSpeed(hitStunStrength, hitStunTime, true);

        onDamageParticles.Play();

        if (health <= 0f && !isDead)
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
        isDead = true;

        if (deathScreen != null)
            deathScreen.gameOver();
        else
            Debug.LogWarning("PlayerHealth: no DeathScreen found in scene - death screen UI will not show.");

        //GameManager.Instance?.HandlePlayerDied();
    }
}
