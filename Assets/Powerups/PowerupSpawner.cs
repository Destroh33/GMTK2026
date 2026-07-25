using System.Collections.Generic;
using UnityEngine;

public class PowerupSpawner : MonoBehaviour
{
    [Header("Spawning")]
    [SerializeField] private Powerup powerupPrefab;
    [SerializeField] private List<UpgradePathData> paths = new List<UpgradePathData>();
    [SerializeField] private Transform spawnAnchor;
    [SerializeField] private float horizontalSpacing = 2f;

    [Header("Rare Rolls")]
    [Range(0f, 4f)][SerializeField] private float rareChanceScale = 1f;
    [SerializeField] private int maxRarePerWave = -1;

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
        SetSelectionPending(false);
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
            SetSelectionPending(false);
            return;
        }

        List<UpgradePathData> offered = new List<UpgradePathData>();
        List<bool> rare = new List<bool>();
        int rareCount = 0;

        foreach (UpgradePathData data in paths)
        {
            if (data == null) continue;

            PlayerStats stats = PlayerStats.Instance;
            bool normalAvailable = stats == null || stats.CanUpgrade(data);
            bool rareAvailable = stats != null && stats.CanUpgradeRare(data);

            if (!normalAvailable && !rareAvailable) continue;

            bool underCap = maxRarePerWave < 0 || rareCount < maxRarePerWave;
            bool rollRare = rareAvailable && underCap &&
                            (!normalAvailable || Random.value < data.rareChance * rareChanceScale);

            if (rollRare) rareCount++;

            offered.Add(data);
            rare.Add(rollRare);
        }

        for (int i = 0; i < offered.Count; i++)
        {
            Vector3 pos = SpawnPosition(i, offered.Count);
            Powerup spawned = Instantiate(powerupPrefab, pos, Quaternion.identity);
            spawned.Init(offered[i], this, rare[i]);
            active.Add(spawned);
        }

        SetSelectionPending(active.Count > 0);
    }

    void SetSelectionPending(bool pending)
    {
        if (GameManager.Instance == null) return;

        if (pending) GameManager.Instance.BeginPowerupSelection();
        else GameManager.Instance.EndPowerupSelection();
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
        SetSelectionPending(false);
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
