using System;
using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    [SerializeField] PlayerHealth playerHealth;
    [SerializeField] Image healthImage;

    [Header("Index 0 = Empty, Index 8 = Full")]
    [SerializeField] Sprite[] healthSprites;

    private void OnEnable()
    {
        playerHealth.OnHealthChanged += UpdateHealthBar;

        UpdateHealthBar(playerHealth.Health, playerHealth.MaxHealth);
    }

    private void OnDisable()
    {
        playerHealth.OnHealthChanged -= UpdateHealthBar;
    }

    void UpdateHealthBar(float health, float maxHealth)
    {
        int i = Mathf.RoundToInt(health);
        i = Mathf.Clamp(i, 0, healthSprites.Length-1);
        healthImage.sprite = healthSprites[i];
    }

}
