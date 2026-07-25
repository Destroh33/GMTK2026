using System;
using System.Collections.Generic;
using UnityEngine;

public enum UpgradePath
{
    Sword = 0,
    Gun = 1,
    Body = 2,
}

public enum StatId
{
    SwordDamage = 0,
    SwordCooldown = 1,
    SwordReach = 2,
    SwordKnockback = 3,

    GunCooldown = 10,
    GunProjectileSpeed = 11,
    GunProjectileDamage = 12,
    GunProjectileKnockback = 13,
    GunProjectileLifetime = 14,
    GunBlastDamage = 15,
    GunBlastSize = 16,
    GunBlastLifetime = 17,
    GunBlastTickRate = 18,

    BodyMaxHealth = 20,
    BodyMoveSpeed = 21,
    BodyDashSpeed = 22,
    BodyDashCooldown = 23,
}

[Serializable]
public class StatGrowth
{
    public StatId stat;
    public float perLevel = 0.15f;
    public bool inverse;

    public float Multiplier(int level)
    {
        float raw = 1f + perLevel * level;
        if (raw < 0.01f) raw = 0.01f;
        return inverse ? 1f / raw : raw;
    }
}

[CreateAssetMenu(fileName = "UpgradePathData", menuName = "Scriptable Objects/UpgradePathData")]
public class UpgradePathData : ScriptableObject
{
    public UpgradePath path;
    public string displayName;
    public Color tint = Color.white;
    public int maxLevel = 10;
    public List<StatGrowth> growths = new List<StatGrowth>();
}
