using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Transform spawnPoint;

    void Awake()
    {
        if (PlayerHealth.Instance != null) return;

        Vector3 pos = spawnPoint != null ? spawnPoint.position : Vector3.zero;
        Instantiate(playerPrefab, pos, Quaternion.identity);
    }
}