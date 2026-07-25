using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] int maxHealth = 5;

    int health;
    int currentMax;
    bool subscribed;

    public int Health => health;
    public int MaxHealth => currentMax;

    void Awake()
    {
        currentMax = maxHealth;
        health = currentMax;
    }

    void Start()
    {
        TrySubscribe();
    }

    void OnEnable()
    {
        TrySubscribe();
    }

    void OnDisable()
    {
        if (subscribed && PlayerStats.Instance != null)
            PlayerStats.Instance.OnPathUpgraded -= HandlePathUpgraded;

        subscribed = false;
    }

    void TrySubscribe()
    {
        if (subscribed || PlayerStats.Instance == null) return;

        PlayerStats.Instance.OnPathUpgraded += HandlePathUpgraded;
        subscribed = true;
    }

    void HandlePathUpgraded(UpgradePath path, int level)
    {
        int newMax = Mathf.Max(1, Mathf.FloorToInt(maxHealth * PlayerStats.Mult(StatId.BodyMaxHealth) + 0.5f));
        int delta = newMax - currentMax;

        currentMax = newMax;

        if (delta > 0) health += delta;
        if (health > currentMax) health = currentMax;
    }

    public void TakeDamage(int amount)
    {
        health -= amount;

        if (health <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        // TODO
    }
}
