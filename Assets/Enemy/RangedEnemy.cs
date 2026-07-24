using UnityEngine;

public class RangedEnemy : EnemyBase
{
    [Header("Steering")]
    [SerializeField] private EnemySteering steering = new EnemySteering();

    [Header("Shooting")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float projectileSpeed = 8f;
    [SerializeField] private float attackRange = 7f;
    [SerializeField] private float shotCooldown = 2f;
    [SerializeField] private float telegraphDuration = 0.45f;
    [SerializeField] private bool stopWhileAiming = true;
    [SerializeField] private float aimTrackingOnFire = 0.5f;

    [Header("Line of Sight")]
    [SerializeField] private LayerMask sightBlockers = 1 << 7;

    [Header("Animation")]
    [SerializeField] private Animator animator;

    static readonly int AimHash = Animator.StringToHash("isAiming");
    static readonly int ShootHash = Animator.StringToHash("shoot");

    float shotTimer;
    float telegraphTimer;
    bool aiming;
    Vector2 lockedAimDir;

    protected override void Awake()
    {
        base.Awake();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        shotTimer = Random.Range(0f, shotCooldown);
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        aiming = false;
        telegraphTimer = 0f;
    }

    protected override void Move()
    {
        float dt = Time.fixedDeltaTime;

        if (shotTimer > 0f) shotTimer -= dt;

        UpdateShooting(dt);

        if (aiming && stopWhileAiming)
            MoveInDirection(Vector2.zero);
        else
            MoveInDirection(steering.GetDirection(rb.position, target.position));
    }

    void UpdateShooting(float dt)
    {
        Vector2 toPlayer = (Vector2)target.position - rb.position;
        bool canShoot = toPlayer.magnitude <= attackRange && HasLineOfSight(toPlayer);

        if (aiming)
        {
            if (!canShoot)
            {
                CancelAim();
                return;
            }

            telegraphTimer -= dt;
            if (telegraphTimer <= 0f)
            {
                Fire();
                CancelAim();
                shotTimer = shotCooldown;
            }
            return;
        }

        if (canShoot && shotTimer <= 0f)
            BeginAim(toPlayer);
    }

    void BeginAim(Vector2 toPlayer)
    {
        aiming = true;
        telegraphTimer = telegraphDuration;
        lockedAimDir = toPlayer.normalized;
        if (animator != null) animator.SetBool(AimHash, true);
    }

    void CancelAim()
    {
        aiming = false;
        telegraphTimer = 0f;
        if (animator != null) animator.SetBool(AimHash, false);
    }

    void Fire()
    {
        if (projectilePrefab == null) return;

        Vector2 currentDir = ((Vector2)target.position - rb.position).normalized;
        Vector2 dir = Vector2.Lerp(lockedAimDir, currentDir, aimTrackingOnFire).normalized;
        if (dir.sqrMagnitude < 0.0001f) dir = lockedAimDir;

        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;
        GameObject shot = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);

        if (shot.TryGetComponent(out EnemyProjectile ep))
            ep.Launch(dir, projectileSpeed);
        else if (shot.TryGetComponent(out Rigidbody2D shotRb))
            shotRb.linearVelocity = dir * projectileSpeed;

        if (animator != null) animator.SetTrigger(ShootHash);
    }

    bool HasLineOfSight(Vector2 toPlayer)
    {
        if (sightBlockers == 0) return true;

        float distance = toPlayer.magnitude;
        if (distance < 0.0001f) return true;

        return !Physics2D.Raycast(rb.position, toPlayer / distance, distance, sightBlockers);
    }

    public override void ApplyKnockback(Vector2 impulse)
    {
        base.ApplyKnockback(impulse);
        CancelAim();
    }

    public override void ApplyStun(float duration)
    {
        base.ApplyStun(duration);
        CancelAim();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
