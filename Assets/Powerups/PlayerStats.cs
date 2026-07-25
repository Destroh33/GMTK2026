using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance { get; private set; }

    [SerializeField] private List<UpgradePathData> paths = new List<UpgradePathData>();

    public event Action<UpgradePath, int> OnPathUpgraded;

    readonly Dictionary<UpgradePath, int> levels = new Dictionary<UpgradePath, int>();
    readonly Dictionary<StatId, float> cache = new Dictionary<StatId, float>();

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

    public bool CanUpgrade(UpgradePathData data)
    {
        if (data == null) return false;
        return GetLevel(data.path) < data.maxLevel;
    }

    public void Upgrade(UpgradePathData data)
    {
        if (data == null) return;

        if (!paths.Contains(data)) paths.Add(data);

        int level = Mathf.Min(GetLevel(data.path) + 1, data.maxLevel);
        levels[data.path] = level;

        Recalculate();
        OnPathUpgraded?.Invoke(data.path, level);
    }

    public float Multiplier(StatId stat)
    {
        return cache.TryGetValue(stat, out float m) ? m : 1f;
    }

    public int ScaleDamage(int baseDamage, StatId stat)
    {
        return Mathf.Max(1, Mathf.FloorToInt(baseDamage * Multiplier(stat) + 0.5f));
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

    public static int Damage(int baseDamage, StatId stat)
    {
        return Instance != null ? Instance.ScaleDamage(baseDamage, stat) : baseDamage;
    }
}
