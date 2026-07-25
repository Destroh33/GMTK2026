using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance { get; private set; }

    [SerializeField] private List<UpgradePathData> paths = new List<UpgradePathData>();

    public event Action<UpgradePath, int, bool> OnPathUpgraded;

    readonly Dictionary<UpgradePath, int> levels = new Dictionary<UpgradePath, int>();
    readonly Dictionary<UpgradePath, int> rareLevels = new Dictionary<UpgradePath, int>();
    readonly Dictionary<StatId, float> cache = new Dictionary<StatId, float>();

    static readonly HashSet<StatId> damageStats = new HashSet<StatId>
    {
        StatId.SwordDamage,
        StatId.GunProjectileDamage,
        StatId.GunBlastDamage,
        StatId.PierceDamage,
    };

    public float BuffDamageMult { get; set; } = 1f;
    public float BuffMoveSpeedMult { get; set; } = 1f;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        Recalculate();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public int GetLevel(UpgradePath path)
    {
        return levels.TryGetValue(path, out int level) ? level : 0;
    }

    public int GetRareLevel(UpgradePath path)
    {
        return rareLevels.TryGetValue(path, out int level) ? level : 0;
    }

    public bool CanUpgrade(UpgradePathData data)
    {
        if (data == null) return false;
        return GetLevel(data.path) < data.maxLevel;
    }

    public bool CanUpgradeRare(UpgradePathData data)
    {
        if (data == null) return false;
        return GetRareLevel(data.path) < data.rareMaxLevel;
    }

    public bool CanOffer(UpgradePathData data)
    {
        return CanUpgrade(data) || CanUpgradeRare(data);
    }

    public void Upgrade(UpgradePathData data, bool rare)
    {
        if (data == null) return;

        if (!paths.Contains(data)) paths.Add(data);

        if (rare)
        {
            rareLevels[data.path] = Mathf.Min(GetRareLevel(data.path) + 1, data.rareMaxLevel);
        }
        else
        {
            levels[data.path] = Mathf.Min(GetLevel(data.path) + 1, data.maxLevel);
            Recalculate();
        }

        OnPathUpgraded?.Invoke(data.path, rare ? GetRareLevel(data.path) : GetLevel(data.path), rare);
    }

    public float Multiplier(StatId stat)
    {
        float m = cache.TryGetValue(stat, out float cached) ? cached : 1f;

        if (damageStats.Contains(stat)) m *= BuffDamageMult;
        else if (stat == StatId.BodyMoveSpeed) m *= BuffMoveSpeedMult;

        return m;
    }

    public float ScaleDamage(float baseDamage, StatId stat)
    {
        return baseDamage * Multiplier(stat);
    }

    void Recalculate()
    {
        cache.Clear();

        foreach (UpgradePathData data in paths)
        {
            if (data == null) continue;

            int level = GetLevel(data.path);

            foreach (StatGrowth growth in data.growths)
            {
                if (growth == null) continue;

                float m = growth.Multiplier(level);
                cache[growth.stat] = cache.TryGetValue(growth.stat, out float existing) ? existing * m : m;
            }
        }
    }

    public static float Mult(StatId stat)
    {
        return Instance != null ? Instance.Multiplier(stat) : 1f;
    }

    public static float Damage(float baseDamage, StatId stat)
    {
        return Instance != null ? Instance.ScaleDamage(baseDamage, stat) : baseDamage;
    }

    public static int Rare(UpgradePath path)
    {
        return Instance != null ? Instance.GetRareLevel(path) : 0;
    }
}
