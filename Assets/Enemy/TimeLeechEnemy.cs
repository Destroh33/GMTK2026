using System;
using Unity.VisualScripting;
using UnityEngine;

public class TimeLeechEnemy : EnemyBase
{
    [Header("Steering")]
    [SerializeField] private EnemySteering steering = new EnemySteering();
    private GameManager gameManager;

    [Header("Attacks")]
    [SerializeField] private float timeLostPerHit;

    private void Start()
    {
        gameManager = GameManager.Instance;
    }

    protected override void OnEnable()
    {
        health = maxHealth;
        target = FindAnyObjectByType<ClockHand>().gameObject.transform;
        currAttackCooldown = 0f;
        state = State.Chasing;

        gameManager = GameManager.Instance;
    }

    protected override void Move()
    {

        MoveInDirection(steering.GetDirection(rb.position, target.position));

    }

    protected override void OnCollisionEnter2D(Collision2D col)
    {
        if (col.collider.TryGetComponent<ClockHand>(out ClockHand p))
        {
            gameManager.AddTime(-timeLostPerHit);
        }
    }

    protected override void OnCollisionStay2D(Collision2D col)
    {
        if (col.collider.TryGetComponent<ClockHand>(out ClockHand p)) 
        {
            gameManager.AddTime(-timeLostPerHit);
        }
    }

}
