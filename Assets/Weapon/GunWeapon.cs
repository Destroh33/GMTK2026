using UnityEngine;

public class GunWeapon : WeaponBase
{
    [Header("Gun")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float projectileSpeed = 9f;
    [SerializeField] private Transform firePoint;

    protected override float CooldownMultiplier() => PlayerStats.Mult(StatId.GunCooldown);

    protected override void Use(Vector2 aimDir)
    {
        if (projectilePrefab == null) return;

        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;
        Vector2 dir = firePoint != null ? ((Vector2)firePoint.right).normalized : aimDir.normalized;
        float speed = projectileSpeed * PlayerStats.Mult(StatId.GunProjectileSpeed);

        GameObject ball = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
        if (ball.TryGetComponent(out Rigidbody2D ballRb))
            ballRb.linearVelocity = dir * speed;
        else if (ball.TryGetComponent(out Projectile proj))
            proj.Launch(dir, speed);
    }
}
