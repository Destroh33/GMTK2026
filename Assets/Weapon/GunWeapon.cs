using UnityEngine;

public class GunWeapon : WeaponBase
{
    public enum GunKind
    {
        Blast = 0,
        Pierce = 1,
    }

    [Header("Gun")]
    [SerializeField] private GunKind kind = GunKind.Blast;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float projectileSpeed = 9f;
    [SerializeField] private Transform firePoint;

    [Header("Cone Spread")]
    [SerializeField] private float spreadStepDegrees = 11f;
    [SerializeField] private int maxExtraPairs = 3;

    StatId CooldownStat => kind == GunKind.Pierce ? StatId.PierceCooldown : StatId.GunCooldown;
    StatId SpeedStat => kind == GunKind.Pierce ? StatId.PierceSpeed : StatId.GunProjectileSpeed;

    protected override float CooldownMultiplier() => PlayerStats.Mult(CooldownStat);

    protected override void Use(Vector2 aimDir)
    {
        if (projectilePrefab == null) return;

        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;
        Vector2 dir = firePoint != null ? ((Vector2)firePoint.right).normalized : aimDir.normalized;
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right;

        float speed = projectileSpeed * PlayerStats.Mult(SpeedStat);
        int shots = ShotCount();

        for (int i = 0; i < shots; i++)
        {
            float offset = (i - (shots - 1) * 0.5f) * spreadStepDegrees;
            Fire(spawnPos, Rotate(dir, offset), speed);
        }
    }

    int ShotCount()
    {
        if (kind != GunKind.Pierce) return 1;

        int pairs = Mathf.Clamp(PlayerStats.Rare(UpgradePath.Pierce), 0, Mathf.Max(0, maxExtraPairs));
        return 1 + pairs * 2;
    }

    void Fire(Vector3 spawnPos, Vector2 dir, float speed)
    {
        GameObject ball = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);

        if (ball.TryGetComponent(out Projectile proj))
            proj.Launch(dir, speed);
        else if (ball.TryGetComponent(out Rigidbody2D ballRb))
            ballRb.linearVelocity = dir * speed;
    }

    static Vector2 Rotate(Vector2 v, float degrees)
    {
        if (Mathf.Approximately(degrees, 0f)) return v;

        float rad = degrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);

        return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
    }
}
