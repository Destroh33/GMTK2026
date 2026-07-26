using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    [SerializeField] Image healthImage;

    [Header("Index 0 = Empty, Index 8 = Full")]
    [SerializeField] Sprite[] healthSprites;

    PlayerHealth playerHealth;

    private void OnEnable()
    {
        TrySubscribe();
    }

    private void OnDisable()
    {
        if (playerHealth != null)
            playerHealth.OnHealthChanged -= UpdateHealthBar;
        playerHealth = null;
    }

    void TrySubscribe()
    {
        playerHealth = PlayerHealth.Instance;
        if (playerHealth == null) return;

        playerHealth.OnHealthChanged += UpdateHealthBar;
        UpdateHealthBar(playerHealth.Health, playerHealth.MaxHealth);
    }

    void UpdateHealthBar(float health, float maxHealth)
    {
        int i = Mathf.RoundToInt(health);
        i = Mathf.Clamp(i, 0, healthSprites.Length - 1);
        healthImage.sprite = healthSprites[i];
    }
}
