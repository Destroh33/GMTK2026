using System.Security.Cryptography;
using UnityEngine;

public class AdultVampireEnemy : RangedEnemy
{
    [Header("Cardinal Positioning")]
    [SerializeField] private EnemySteering positionSteering = new EnemySteering();
    [SerializeField] private float minStandoffDistance = 3f;
    [SerializeField] private float maxStandoffDistance = 6f;
    [SerializeField] private float arriveThreshold = 0.3f;
    [SerializeField] private float slowRadius = 1.5f;

    static readonly Vector2[] CardinalDirs = { Vector2.up, Vector2.down, Vector2.left, Vector2.right, };

    protected override Vector2 GetMoveDirection()
    {
        Vector2 selfPos = rb.position;
        Vector2 playerPos = target.position;
        Vector2 fromPlayer = selfPos - playerPos;

        // calculates closest player cardinal axis 
        Vector2 cardinal = Mathf.Abs(fromPlayer.x) >= Mathf.Abs(fromPlayer.y) 
            ? new Vector2(Mathf.Sign(fromPlayer.x == 0f ? 1f : fromPlayer.x), 0f)
            : new Vector2(0f, Mathf.Sign(fromPlayer.y == 0f ? 1f : fromPlayer.y));

        // calculates closest point on that axis
        float projected = Vector2.Dot(fromPlayer, cardinal);
        float clamped = Mathf.Clamp(projected, minStandoffDistance, maxStandoffDistance);

        Vector2 desiredPos = playerPos + cardinal * clamped;
        Vector2 toTarget = desiredPos - selfPos;
        float distance = toTarget.magnitude;

        //stand still if at desiredPos
        if (distance <= arriveThreshold)
            return Vector2.zero;

        Vector2 dir = positionSteering.GetDirection(selfPos, desiredPos);

        if (distance < slowRadius)
        {
            float speedFactor = Mathf.InverseLerp(arriveThreshold, slowRadius, distance);
            dir *= speedFactor;
        }

        return dir;
    }

    protected override void Fire()
    {
        if (projectilePrefab == null) return;

        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;

        foreach (Vector2 dir in CardinalDirs)
        {
            GameObject shot = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);

            if (shot.TryGetComponent(out EnemyProjectile ep))
                ep.Launch(dir, projectileSpeed);
            else if (shot.TryGetComponent(out Rigidbody2D shotRb))
                shotRb.linearVelocity = dir * projectileSpeed;
        }

        if (animator != null) animator.SetTrigger(ShootHash);
    }
}
