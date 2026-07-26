using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    [SerializeField] Image healthImage;

    [Header("Index 0 = Empty, Index 8 = Full")]
    [SerializeField] Sprite[] healthSprites;

    PlayerHealth playerHealth;
    Coroutine waitRoutine;

    private void OnEnable()
    {
        if (!TrySubscribe())
            waitRoutine = StartCoroutine(WaitForPlayer());
    }

    private void OnDisable()
    {
        if (waitRoutine != null)
        {
            StopCoroutine(waitRoutine);
            waitRoutine = null;
        }

        if (playerHealth != null)
            playerHealth.OnHealthChanged -= UpdateHealthBar;
        playerHealth = null;
    }

    IEnumerator WaitForPlayer()
    {
        while (PlayerHealth.Instance == null)
            yield return null;

        TrySubscribe();
        waitRoutine = null;
    }

    bool TrySubscribe()
    {
        playerHealth = PlayerHealth.Instance;
        if (playerHealth == null) return false;

        playerHealth.OnHealthChanged += UpdateHealthBar;
        UpdateHealthBar(playerHealth.Health, playerHealth.MaxHealth);
        return true;
    }

    void UpdateHealthBar(float health, float maxHealth)
    {
        int i = Mathf.RoundToInt(health);
        i = Mathf.Clamp(i, 0, healthSprites.Length - 1);
        healthImage.sprite = healthSprites[i];
    }
}