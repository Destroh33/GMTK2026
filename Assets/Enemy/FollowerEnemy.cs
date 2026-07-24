using UnityEngine;

public class FollowerEnemy : EnemyBase
{
    [Header("Steering")]
    [SerializeField] private EnemySteering steering = new EnemySteering();

    protected override void Move()
    {
        MoveInDirection(steering.GetDirection(rb.position, target.position));
    }
}
