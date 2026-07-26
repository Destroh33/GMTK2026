using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerHealth : PlayerBehaviors
{
    public static PlayerHealth Instance { get; private set; }

    [Header("Health")]
    [SerializeField] float maxHealth = 8f;

    [Header("Stun")]
    [SerializeField] float hitStunTime = 0.16f;
    [SerializeField] float hitStunStrength = 0.35f;

    [Header("Invulnerability")]
    [SerializeField] float invulnerabilityTime = 0.8f;
    [SerializeField] float blinkInterval = 0.07f;

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

    float snapshotHealth;
    float snapshotMax;
    bool hasBossSnapshot;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        movement = GetComponent<PlayerMovement>();
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
        SceneManager.sceneLoaded += HandleSceneLoaded;

        if (deathScreen == null)
            deathScreen = FindAnyObjectByType<DeathScreen>(FindObjectsInactive.Include);
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;

        if (subscribed && PlayerStats.Instance != null)
            PlayerStats.Instance.OnPathUpgraded -= HandlePathUpgraded;

        subscribed = false;
    }

    void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        subscribed = false;
        TrySubscribe();

        deathScreen = FindAnyObjectByType<DeathScreen>(FindObjectsInactive.Include);
    }

    void TrySubscribe()
    {
        if (subscribed) return;
        if (PlayerStats.Instance == null) return;

        PlayerStats.Instance.OnPathUpgraded += HandlePathUpgraded;
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

    public void ResetForNewRun()
    {
        currentMax = maxHealth;
        health = currentMax;
        isDead = false;

        NotifyHealthChanged();
    }

    public void SnapshotForBoss()
    {
        snapshotHealth = health;
        snapshotMax = currentMax;
        hasBossSnapshot = true;
    }

    void RestoreBossSnapshot()
    {
        if (!hasBossSnapshot) return;

        currentMax = snapshotMax;
        health = snapshotHealth;
        isDead = false;

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

        if (SceneManager.GetActiveScene().name == "BossScene")
        {
            RestoreBossSnapshot();
            PlayerStats.Instance?.RestoreBossSnapshot();
        }

        deathScreen.gameOver();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
