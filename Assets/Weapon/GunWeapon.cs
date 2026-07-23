using UnityEngine;

public class GunWeapon : WeaponBase
{
    [Header("Gun")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float projectileSpeed = 12f;
    [SerializeField] private Transform firePoint;

    protected override void Use(Vector2 aimDir)
    {
        if (projectilePrefab == null) return;

        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;
        Vector2 dir = firePoint != null ? ((Vector2)firePoint.right).normalized : aimDir.normalized;

        GameObject ball = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
        if (ball.TryGetComponent(out Rigidbody2D ballRb))
            ballRb.linearVelocity = dir * projectileSpeed;
        else if (ball.TryGetComponent(out Projectile proj))
            proj.Launch(dir, projectileSpeed);
    }
}
