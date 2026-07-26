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
    [SerializeField] private float drainInterval = 1.5f;

    private float drainTimer;

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
        drainTimer = 0f;

        gameManager = GameManager.Instance;
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate();

        if (drainTimer > 0f)
        {
            drainTimer -= Time.fixedDeltaTime;
            if (drainTimer < 0f) drainTimer = 0f;
        }
    }

    protected override void Move()
    {

        MoveInDirection(steering.GetDirection(rb.position, target.position));

    }

    protected override void OnCollisionEnter2D(Collision2D col)
    {
        TryDrain(col);
    }

    protected override void OnCollisionStay2D(Collision2D col)
    {
        TryDrain(col);
    }

    private void TryDrain(Collision2D col)
    {
        if (drainTimer > 0f) return;
        if (!col.collider.TryGetComponent<ClockHand>(out ClockHand hand)) return;

        drainTimer = Mathf.Max(0.05f, drainInterval);
        gameManager.AddTime(-timeLostPerHit);
    }

}
