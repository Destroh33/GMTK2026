using UnityEngine;

public class Bloodlust : MonoBehaviour
{
    [Header("Stacks")]
    [SerializeField] private int baseMaxStacks = 5;
    [SerializeField] private int maxStacksPerLevel = 2;

    [Header("Duration")]
    [SerializeField] private float baseDuration = 3f;
    [SerializeField] private float durationPerLevel = 1.5f;

    [Header("Power Per Stack")]
    [SerializeField] private float damagePerStack = 0.08f;
    [SerializeField] private float damagePerStackPerLevel = 0.04f;
    [SerializeField] private float moveSpeedPerStack = 0.04f;
    [SerializeField] private float moveSpeedPerStackPerLevel = 0.02f;

    [Header("Feedback")]
    [SerializeField] private SpriteRenderer tintTarget;
    [SerializeField] private Color fullStackTint = new Color(1f, 0.5f, 0.5f, 1f);

    int stacks;
    float timer;
    Color baseTint;
    bool hasBaseTint;

    public int Stacks => stacks;
    public float TimeRemaining => timer;

    void Awake()
    {
        if (tintTarget != null)
        {
            baseTint = tintTarget.color;
            hasBaseTint = true;
        }
    }

    void OnEnable()
    {
        EnemyBase.OnAnyDied += HandleEnemyDied;
    }

    void OnDisable()
    {
        EnemyBase.OnAnyDied -= HandleEnemyDied;

        stacks = 0;
        timer = 0f;
        Apply();
    }

    void Update()
    {
        if (stacks <= 0) return;

        timer -= Time.deltaTime;

        if (timer <= 0f)
        {
            stacks = 0;
            timer = 0f;
            Apply();
        }
    }

    void HandleEnemyDied(EnemyBase enemy)
    {
        int level = PlayerStats.Rare(UpgradePath.Body);
        if (level <= 0) return;

        stacks = Mathf.Min(stacks + 1, baseMaxStacks + maxStacksPerLevel * (level - 1));
        timer = baseDuration + durationPerLevel * (level - 1);

        Apply();
    }

    void Apply()
    {
        int level = PlayerStats.Rare(UpgradePath.Body);

        float damageStep = damagePerStack + damagePerStackPerLevel * Mathf.Max(0, level - 1);
        float speedStep = moveSpeedPerStack + moveSpeedPerStackPerLevel * Mathf.Max(0, level - 1);

        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.BuffDamageMult = 1f + damageStep * stacks;
            PlayerStats.Instance.BuffMoveSpeedMult = 1f + speedStep * stacks;
        }

        if (tintTarget != null && hasBaseTint)
        {
            int max = Mathf.Max(1, baseMaxStacks + maxStacksPerLevel * Mathf.Max(0, level - 1));
            tintTarget.color = Color.Lerp(baseTint, fullStackTint, stacks / (float)max);
        }
    }
}
