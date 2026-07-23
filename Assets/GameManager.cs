using System;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Serializable]
    public class EnemySpawn
    {
        public EnemyBase enemyPrefab;
        [Min(0)] public int count = 1;
    }

    [Serializable]
    public class Wave
    {
        public string name;
        public List<EnemySpawn> spawns = new List<EnemySpawn>();
    }

    [SerializeField] private float startTime = 30f * 60f;
    [SerializeField] private float countdownSpeed = 1f;
    [SerializeField] private List<Wave> waves = new List<Wave>();]
    [SerializeField] private List<Transform> spawnPoints = new List<Transform>();

    public float TimeRemaining { get; private set; }
    public bool TimerRunning { get; private set; }
    public int CurrentWaveIndex { get; private set; } = -1;

    public event Action OnTimeExpired;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        TimeRemaining = startTime;
        TimerRunning = true;
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    void Update()
    {
        if (!TimerRunning) return;

        TimeRemaining -= Time.deltaTime * countdownSpeed;

        if (TimeRemaining <= 0f)
        {
            TimeRemaining = 0f;
            TimerRunning = false;
            OnTimeExpired?.Invoke();
        }
    }

    public void AddTime(float seconds)
    {
        TimeRemaining += seconds;
        if (TimeRemaining > 0f && !TimerRunning)
        {
            TimerRunning = true;
        }
    }

    public void SetCountdownSpeed(float speed)
    {
        countdownSpeed = speed;
    }

    public void PauseTimer() => TimerRunning = false;
    public void ResumeTimer() => TimerRunning = true;

    public List<EnemyBase> SpawnWave(int waveIndex)
    {
        var spawned = new List<EnemyBase>();

        if (waveIndex < 0 || waveIndex >= waves.Count)
        {
            Debug.LogWarning($"[GameManager] SpawnWave called with invalid index {waveIndex}.");
            return spawned;
        }

        CurrentWaveIndex = waveIndex;
        Wave wave = waves[waveIndex];

        foreach (EnemySpawn spawn in wave.spawns)
        {
            if (spawn.enemyPrefab == null)
            {
                Debug.LogWarning($"[GameManager] Wave '{wave.name}' has a spawn entry with no prefab assigned.");
                continue;
            }

            for (int i = 0; i < spawn.count; i++)
            {
                EnemyBase enemy = Instantiate(spawn.enemyPrefab, GetRandomSpawnPosition(), Quaternion.identity);
                spawned.Add(enemy);
            }
        }

        return spawned;
    }
    private Vector3 GetRandomSpawnPosition()
    {
        if (spawnPoints.Count == 0)
        {
            Debug.LogWarning("[GameManager] No spawn points assigned; spawning at origin.");
            return Vector3.zero;
        }

        Transform point = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Count)];
        return point != null ? point.position : Vector3.zero;
    }

    public int WaveCount => waves.Count;
}
