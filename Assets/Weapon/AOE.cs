using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Internal;

public class AOE : MonoBehaviour
{

    [SerializeField] private float lifetime = 1.5f;
    [SerializeField] private float size = 0.8f;
    [SerializeField] private float tickInterval = 1.2f;
    [SerializeField] private float damage = 1f;
    [SerializeField] private CircleCollider2D col;

    [Header("Tar")]
    [SerializeField] private float tarSlowPerLevel = 0.2f;
    [SerializeField] private float tarMinSpeedMultiplier = 0.35f;
    [SerializeField] private float tarAmpPerLevel = 0.15f;
    [SerializeField] private float tarLinger = 1.25f;
    [SerializeField] private SpriteRenderer tintTarget;
    [SerializeField] private Color tarColor = new Color(0.25f, 0.2f, 0.3f, 1f);

    private float lifeTimer;
    private int tarLevel;
    private float tarSlow = 1f;
    private float tarAmp = 1f;

    // enemies in garlic AOE, float is their garlic dmg aoe tick timer
    private readonly Dictionary<EnemyBase, float> enemies = new Dictionary<EnemyBase, float>();

    private void Awake()
    {
        if (col == null) col = GetComponent<CircleCollider2D>();
        if (tintTarget == null) tintTarget = GetComponentInChildren<SpriteRenderer>();
    }

    public void Init(EnemyBase hitEnemy = null)
    {
        damage = PlayerStats.Damage(damage, StatId.GunBlastDamage);
        size *= PlayerStats.Mult(StatId.GunBlastSize);
        lifetime *= PlayerStats.Mult(StatId.GunBlastLifetime);
        tickInterval *= PlayerStats.Mult(StatId.GunBlastTickRate);

        tarLevel = PlayerStats.Rare(UpgradePath.Gun);
        if (tarLevel > 0)
        {
            tarSlow = Mathf.Max(tarMinSpeedMultiplier, 1f - tarSlowPerLevel * tarLevel);
            tarAmp = 1f + tarAmpPerLevel * tarLevel;
            if (tintTarget != null) tintTarget.color = tarColor;
        }

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
                Tar(e);
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
            Tar(e);
        }
    }

    void Tar(EnemyBase e)
    {
        if (tarLevel <= 0 || e == null) return;

        e.ApplyTar(tarSlow, tarAmp, tarLinger);
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

            Tar(e);

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
