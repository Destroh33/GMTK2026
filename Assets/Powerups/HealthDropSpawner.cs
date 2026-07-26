using UnityEngine;

public class HealthDropSpawner : MonoBehaviour
{
    [SerializeField] private HealthPickup pickupPrefab;
    [Range(0f, 1f)][SerializeField] private float dropChance = 0.03f;

    void OnEnable()
    {
        EnemyBase.OnAnyDied += HandleEnemyDied;
    }

    void OnDisable()
    {
        EnemyBase.OnAnyDied -= HandleEnemyDied;
    }

    void HandleEnemyDied(EnemyBase enemy)
    {
        if (pickupPrefab == null || enemy == null) return;
        if (Random.value > dropChance) return;

        Instantiate(pickupPrefab, enemy.transform.position, Quaternion.identity);
    }
}
