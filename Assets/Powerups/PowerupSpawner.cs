using System.Collections.Generic;
using UnityEngine;

public class PowerupSpawner : MonoBehaviour
{
    [Header("Spawning")]
    [SerializeField] private Powerup powerupPrefab;
    [SerializeField] private List<UpgradePathData> paths = new List<UpgradePathData>();
    [SerializeField] private Transform spawnAnchor;
    [SerializeField] private float horizontalSpacing = 2f;

    readonly List<Powerup> active = new List<Powerup>();
    bool subscribed;

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
        if (subscribed && GameManager.Instance != null)
            GameManager.Instance.OnWaveCleared -= HandleWaveCleared;

        subscribed = false;
    }

    void TrySubscribe()
    {
        if (subscribed || GameManager.Instance == null) return;

        GameManager.Instance.OnWaveCleared += HandleWaveCleared;
        subscribed = true;
    }

    void HandleWaveCleared(int waveIndex)
    {
        SpawnChoice();
    }

    public void SpawnChoice()
    {
        ClearActive();

        if (powerupPrefab == null)
        {
            Debug.LogWarning("[PowerupSpawner] No powerup prefab assigned.");
            return;
        }

        List<UpgradePathData> offered = new List<UpgradePathData>();

        foreach (UpgradePathData data in paths)
        {
            if (data == null) continue;
            if (PlayerStats.Instance != null && !PlayerStats.Instance.CanUpgrade(data)) continue;
            offered.Add(data);
        }

        for (int i = 0; i < offered.Count; i++)
        {
            Vector3 pos = SpawnPosition(i, offered.Count);
            Powerup spawned = Instantiate(powerupPrefab, pos, Quaternion.identity);
            spawned.Init(offered[i], this);
            active.Add(spawned);
        }
    }

    Vector3 SpawnPosition(int index, int total)
    {
        Vector3 center = spawnAnchor != null ? spawnAnchor.position : transform.position;
        float offset = (index - (total - 1) * 0.5f) * horizontalSpacing;

        return center + new Vector3(offset, 0f, 0f);
    }

    public void NotifyClaimed(Powerup claimed)
    {
        foreach (Powerup p in active)
        {
            if (p == null || p == claimed) continue;
            Destroy(p.gameObject);
        }

        active.Clear();
    }

    void ClearActive()
    {
        foreach (Powerup p in active)
        {
            if (p != null) Destroy(p.gameObject);
        }

        active.Clear();
    }
}
