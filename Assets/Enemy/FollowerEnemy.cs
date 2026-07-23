using UnityEngine;

public class FollowerEnemy : EnemyBase
{
    protected override void Move()
    {
        /*Vector2 diff = (Vector2)target.position - rb.position;
        if (diff.magnitude < 0.1)
        {
            return;
        }

        rb.MovePosition(rb.position + diff.normalized * moveSpeed * Time.fixedDeltaTime);*/
        if (target != null) {
            agent.SetDestination(target.position);
        }

        //if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
    }
}
