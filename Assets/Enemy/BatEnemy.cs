using NUnit.Framework;
using UnityEngine;

public class BatEnemy : EnemyBase
{
    [Header("Steering")]
    [SerializeField] private EnemySteering steering = new EnemySteering();

    [Header("Heading Smoothing")]
    [SerializeField] private float turnRateDegrees = 220f;
    [SerializeField, UnityEngine.Range(0f, 1f)] private float driftMult = 0.3f;

    Vector2 currentDirection;

    protected override void OnEnable()
    {
        base.OnEnable();
        currentDirection = target != null ? ((Vector2)target.position - rb.position).normalized : Vector2.zero;
    }

    protected override void Move()
    {
        Vector2 targetDir = steering.GetDirection(rb.position, target.position);
        if (targetDir.sqrMagnitude < 0.0001f)
            targetDir = currentDirection;

        float angle = Mathf.Atan2(currentDirection.y, currentDirection.x) * Mathf.Rad2Deg;
        float targetAngle = Mathf.Atan2(targetDir.y, targetDir.x) * Mathf.Rad2Deg;

        float newAngle = Mathf.MoveTowardsAngle(angle, targetAngle, turnRateDegrees * Time.fixedDeltaTime);
        float rad = newAngle * Mathf.Deg2Rad;
        currentDirection = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

        float alignment = Vector2.Dot(currentDirection, targetDir);
        float speedMult = Mathf.Lerp(driftMult, 1f, (alignment + 1f) * 0.5f);

        MoveInDirection(currentDirection);
    }
}