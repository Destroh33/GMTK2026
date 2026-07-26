using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Boss : MonoBehaviour
{
    public enum BossAction { Teleport, SpawnBats, Dash, ShootProjectiles, SpawnSkeleton }

    public BossAction CurrentAction { get; private set; }
    public event System.Action<BossAction> OnActionChanged;

    void SetAction(BossAction action)
    {
        CurrentAction = action;
        OnActionChanged?.Invoke(action);
    }

    public float HealthFraction => maxHealthPoints > 0f ? Mathf.Clamp01(healthPoints / maxHealthPoints) : 0f;
    public bool PastHalfway => HealthFraction <= 0.5f;

    // Ramps the cooldown multiplier from cooldownMultiplierAtFullHealth down to
    // cooldownMultiplierAtHalfHealth as health drops from 100% to 50% (and stays
    // there below 50%). The ramp itself follows a log curve, so it moves quickly
    // at first and levels off (is "basically maxed") well before half health.
    float CooldownMultiplier
    {
        get
        {
            float t = Mathf.Clamp01((1f - HealthFraction) / 0.5f);
            float curved = Mathf.Log10(1f + t * 9f); // 0 -> 1, fast then flattening
            return Mathf.Lerp(cooldownMultiplierAtFullHealth, cooldownMultiplierAtHalfHealth, curved);
        }
    }

    float ScaledCooldown(float baseCooldown) => Mathf.Max(0f, baseCooldown * CooldownMultiplier);

    [Header("Base Stats")]
    [SerializeField] public float healthPoints;
    [SerializeField] float contactDamage = 1f;
    [SerializeField] float contactDamageCooldown;

    [Header("UI")]
    [SerializeField] private GameObject healthBar;
    [SerializeField] private Image healthFill;

    private Rigidbody2D rb;
    GameManager gameManager;
    private float maxHealthPoints;
    private float timeSinceDamage;

    [Header("Stage Setup")]
    [SerializeField] private Transform stageCenter;
    [SerializeField] private int radiusOfStage;
    [SerializeField] private float bossFightMaxTime;
    private float bossFightTimer;
    private bool canBeHit;
    private float nextContactDamageTime;

    [Header("=====Difficulty Scaling (health-based)=====")]
    [Tooltip("Cooldown multiplier applied while the boss is at full health (1 = unscaled).")]
    [SerializeField] private float cooldownMultiplierAtFullHealth = 1f;
    [Tooltip("Cooldown multiplier the ramp reaches by half health, and stays at for the rest of the fight - the fastest cooldowns get.")]
    [SerializeField] private float cooldownMultiplierAtHalfHealth = 0.4f;

    [Header("=====Boss Attack Values=====")]
    [Header("case 1: teleport")]
    [SerializeField] private float timeBetweenTeleports;

    [Header("Delay between spawn trigger and spawning")]
    [SerializeField] private float spawnDelayAfterSelection = 0.5f;

    [Header("case 2: spawn bats")]
    [SerializeField] private GameObject batEnemyPrefab;
    [SerializeField] int numBatsSpawned;
    [Tooltip("Bats spawned once past the halfway health mark.")]
    [SerializeField] int numBatsSpawnedPostHalf = 4;
    [SerializeField] float cooldownAfterBatSpawn;

    [Header("case 3: dash attack")]
    [SerializeField] float dashSpeed;
    [SerializeField] float dashRadius;
    [SerializeField] float cooldownAfterDash;
    [Tooltip("Safety timeout - the dash always ends after this long even if it never gets close to its target or slows down (rb has zero linear damping, so an overshot dash can otherwise never resolve on its own).")]
    [SerializeField] float maxDashDuration = 1.5f;

    [Header("case 4: shoot projectiles")]
    [SerializeField] GameObject bullet;
    [SerializeField] float bulletForce;
    [SerializeField] int bulletsPerCircle;
    [Tooltip("Bullets per circle once past the halfway health mark.")]
    [SerializeField] int bulletsPerCirclePostHalf = 45;
    [SerializeField] int numberOfTurns;
    [Tooltip("Number of turns once past the halfway health mark.")]
    [SerializeField] int numberOfTurnsPostHalf = 2;
    [SerializeField] float timeBetweenShots;
    [SerializeField] float cooldownAfterBullets;

    [Header("case 5: spawn shield skeleton")]
    [SerializeField] private GameObject skeletonEnemyPrefab;
    [SerializeField] float cooldownAfterSkeletonSpawn;

    //[Header("case 6: slash attack")] //implement if theres time, change the state machine
    //[SerializeField] GameObject slashAttack;
    //[SerializeField] float timeSlashIsActive;
    //[SerializeField] float cooldownAfterSlash;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        maxHealthPoints = Mathf.Max(healthPoints, 1f);
        timeSinceDamage = 0f;
        StartCoroutine(StateMachine());
        bossFightTimer = bossFightMaxTime;

        if (healthBar != null) healthBar.SetActive(false);
        if (healthFill != null) healthFill.fillAmount = 1f;
    }

    private void Start()
    {
        gameManager = GameManager.Instance;
        AudioManager.Instance?.PlayBossMusic();
    }

    private void Update()
    {
        timeSinceDamage += Time.deltaTime;

        if (healthBar != null && timeSinceDamage > 3f && healthBar.activeSelf)
        {
            healthBar.SetActive(false);
        }
    }

    public IEnumerator StateMachine()
    {
        while (true)
        {
            int choice = Random.Range(0, 5);
            switch (choice)
            {
                case 0: //teleport to another spot in the stage - chains 3x after halfway
                    int teleportCount = PastHalfway ? 3 : 1;
                    for (int t = 0; t < teleportCount; t++)
                    {
                        SetAction(BossAction.Teleport);
                        AudioManager.Instance?.PlayBossTeleportSFX();
                        Vector2 teleportPosition = Random.insideUnitCircle * radiusOfStage + (Vector2)stageCenter.position;
                        transform.position = teleportPosition;
                        rb.linearVelocity = Vector2.zero;
                        yield return new WaitForSeconds(ScaledCooldown(timeBetweenTeleports));
                    }
                    break;
                case 1: //spawn bats
                    SetAction(BossAction.SpawnBats);
                    yield return new WaitForSeconds(spawnDelayAfterSelection);
                    AudioManager.Instance?.PlayBossSpawnBatsSFX();
                    int batsToSpawn = PastHalfway ? numBatsSpawnedPostHalf : numBatsSpawned;
                    for (int i = 0; i < batsToSpawn; i++)
                    {
                        Instantiate(batEnemyPrefab, (Vector2)stageCenter.position + Random.insideUnitCircle * radiusOfStage, Quaternion.identity);
                    }
                    yield return new WaitForSeconds(ScaledCooldown(cooldownAfterBatSpawn));
                    break;
                case 2: //dash - chains 3x after halfway
                    int dashCount = PastHalfway ? 3 : 1;
                    for (int d = 0; d < dashCount; d++)
                    {
                        SetAction(BossAction.Dash);
                        rb.linearVelocity = Vector2.zero;
                        Vector2 posToDashTo = (Vector2)FindAnyObjectByType<PlayerMovement>().transform.position + Random.insideUnitCircle * dashRadius;
                        rb.AddForce(dashSpeed * (posToDashTo - (Vector2)transform.position).normalized, ForceMode2D.Impulse);
                        AudioManager.Instance?.PlayBossDashSFX();

                        float dashElapsed = 0f;
                        while (dashElapsed < maxDashDuration
                            && Vector2.Distance(posToDashTo, (Vector2)transform.position) >= 0.5f
                            && rb.linearVelocity.magnitude >= 0.1f)
                        {
                            dashElapsed += Time.deltaTime;
                            yield return null;
                        }

                        rb.linearVelocity = Vector2.zero;
                    }
                    yield return new WaitForSeconds(ScaledCooldown(cooldownAfterDash));
                    break;
                case 3: //shoot projectiles - spiral pattern
                    SetAction(BossAction.ShootProjectiles);
                    rb.linearVelocity = Vector2.zero;
                    int bulletsThisCircle = PastHalfway ? bulletsPerCirclePostHalf : bulletsPerCircle;
                    int turnsThisTime = PastHalfway ? numberOfTurnsPostHalf : numberOfTurns;
                    float angleBetweenShots = 360 / bulletsThisCircle;
                    for (int _ = 0; _ < turnsThisTime; _++)
                    {
                        for (int i = 0; i < bulletsThisCircle; i++)
                        {
                            float angleToShoot = angleBetweenShots * i;
                            EnemyProjectile b = Instantiate(bullet, transform.position, Quaternion.identity).GetComponent<EnemyProjectile>();
                            b.Launch(new Vector2(Mathf.Cos(angleToShoot), Mathf.Sin(angleToShoot)).normalized, bulletForce);
                            yield return new WaitForSeconds(ScaledCooldown(timeBetweenShots));
                        }
                    }
                    yield return new WaitForSeconds(ScaledCooldown(cooldownAfterBullets));
                    break;
                case 4: //spawn shield skeleton
                    SetAction(BossAction.SpawnSkeleton);
                    yield return new WaitForSeconds(spawnDelayAfterSelection);
                    AudioManager.Instance?.PlayBossSpawnSkeletonSFX();
                    Instantiate(skeletonEnemyPrefab, (Vector2)stageCenter.position + Random.insideUnitCircle * radiusOfStage, Quaternion.identity);
                    yield return new WaitForSeconds(ScaledCooldown(cooldownAfterSkeletonSpawn));
                    break;
                //case 4: //frontal slash attack
                //    PlayerHealth player = FindAnyObjectByType<PlayerHealth>();
                //    if (player != null)
                //    {
                //        Vector2 dirToPlayer = (player.transform.position - transform.position).normalized;
                //        float angle = Mathf.Atan2(dirToPlayer.y, dirToPlayer.x) * Mathf.Rad2Deg;
                //        slashAttack.transform.rotation = Quaternion.Euler(0, 0, angle);
                //    }
                //    slashAttack.SetActive(true);
                //    yield return new WaitForSeconds(timeSlashIsActive);
                //    slashAttack.SetActive(false);
                //    yield return new WaitForSeconds(cooldownAfterSlash);
                //    break;
                default:
                    break;
            }

        }

    }

    public virtual void TakeDamage(float amount)
    {
        TakeDamage(amount, Vector2.zero);
    }

    public virtual void TakeDamage(float amount, Vector2 knockbackImpulse)
    {
        if (healthPoints <= 0f) return;

        AudioManager.Instance?.PlayBossHitSFX();

        healthPoints -= amount;
        timeSinceDamage = 0f;

        if (knockbackImpulse.sqrMagnitude > 0.0001f && rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.AddForce(knockbackImpulse, ForceMode2D.Impulse);
        }

        if (healthPoints <= 0f) 
        {
            Die();
            return;
        }

        UpdateHealthBar();
    }

    void Die()
    {
        AudioManager.Instance?.PlayBossDeathSFX();

        if (healthBar != null)
        {
            healthBar.SetActive(false);
        }

        Destroy(gameObject);
    }

    void UpdateHealthBar()
    {
        if (healthBar != null) healthBar.SetActive(true);
        if (healthFill != null) healthFill.fillAmount = maxHealthPoints > 0f ? healthPoints / maxHealthPoints : 0f;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryDealContactDamage(collision);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        TryDealContactDamage(collision);
    }

    private void TryDealContactDamage(Collision2D collision)
    {
        if (Time.time < nextContactDamageTime) return;

        PlayerHealth player = collision.collider.GetComponentInParent<PlayerHealth>();
        if (player == null) return;

        player.TakeDamage(contactDamage);
        nextContactDamageTime = Time.time + contactDamageCooldown;
    }

}
