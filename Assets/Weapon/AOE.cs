using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Internal;

public class AOE : MonoBehaviour
{

    [SerializeField] private float lifetime = 3f;
    [SerializeField] private float size = 1.0f;
    [SerializeField] private float tickInterval = 1.0f;
    [SerializeField] private int damage = 1;
    [SerializeField] private CircleCollider2D col;

    private float lifeTimer;

    // enemies in garlic AOE, float is their garlic dmg aoe tick timer
    private readonly Dictionary<EnemyBase, float> enemies = new Dictionary<EnemyBase, float>();

    private void Awake()
    {
        if (col == null) col = GetComponent<CircleCollider2D>();
    }

    public void Init(EnemyBase hitEnemy = null)
    {
        // change size of aoe
        transform.localScale = Vector3.one * size;

        var filter = new ContactFilter2D { useTriggers = true };
        var overlaps = new List<Collider2D>();
        Physics2D.OverlapCollider(col, filter, overlaps);

        foreach (var hit in overlaps)
        {
            if (hit.TryGetComponent<EnemyBase>(out var e))
            {
                if (e != hitEnemy) // excludes the hit enemy so they only take contact dmg, not aoe dmg on aoe instantiation
                    e.TakeDamage(damage);

                enemies[e] = 0f;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.TryGetComponent<EnemyBase>(out var e))
        {
            if (!enemies.ContainsKey(e)) // excludes enemies already in aoe from taking dmg
                e.TakeDamage(damage);

            enemies[e] = 0f;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.TryGetComponent<EnemyBase>(out var e))
            enemies.Remove(e);
    }

    private void Update()
    {
        lifeTimer += Time.deltaTime;
        if (lifeTimer >= lifetime)
        {
            Destroy(gameObject);
            return;
        }

        TickDmg();
    }

    void TickDmg()
    {
        // remove enemy killed by other means from enemies, create new list of 
        var keys = new List<EnemyBase>(enemies.Keys);

        foreach (var e in keys)
        {
            if (e == null)
            {
                enemies.Remove(e);
                continue;
            }

            if (!enemies.TryGetValue(e, out float timer)) { continue; }

            timer += Time.deltaTime;
            
            if (timer >= tickInterval)
            {
                timer -= tickInterval;
                e.TakeDamage(damage);
            }

            if (enemies.ContainsKey(e))
                enemies[e] = timer;
        }
    }
}
