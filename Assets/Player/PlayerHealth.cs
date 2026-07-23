using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] int maxHealth = 8;

    int health;

    void Awake()
    {
        health = maxHealth;
    }

    public void TakeDamage(int amount) {
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
