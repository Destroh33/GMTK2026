using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum WaveState { Idle, Spawning, Clearing, Intermission, AwaitFloorAdvance, Complete }

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
        [Min(0f)] public float spawnDuration = 8f;
        [Min(0f)] public float intermission = 4f;
    }

    [Serializable]
    public class Floor
    {
        public string name;
        public List<Wave> waves = new List<Wave>();
    }

    [Header("Timer")]
    [SerializeField] private float startTime = 30f * 60f;
    [SerializeField] private float countdownSpeed = 1f;

    public float StartTime => startTime;
    public float CountdownSpeed => countdownSpeed;

    [Header("Floors")]
    [SerializeField] private List<Floor> floors = new List<Floor>();
    [SerializeField] private List<Transform> spawnPoints = new List<Transform>();
    [SerializeField] private bool autoStart = true;
    [SerializeField] private float initialDelay = 2f;

    public float TimeRemaining { get; private set; }
    public bool TimerRunning { get; private set; }
    public int CurrentFloorIndex { get; private set; } = -1;
    public int CurrentWaveIndexInFloor { get; private set; } = -1;
    public WaveState CurrentWaveState { get; private set; } = WaveState.Idle;
    public int AliveEnemyCount => aliveEnemies.Count;
    public int PendingSpawnCount => spawnQueue.Count;
    public int FloorCount => floors.Count;

    public event Action OnTimeExpired;
    public event Action<int> OnFloorStarted;
    public event Action<int> OnFloorCleared;
    public event Action OnAllFloorsCleared;
    public event Action<int> OnWaveStarted;
    public event Action<int> OnWaveCleared;
    public event Action OnRunReset;

    readonly List<EnemyBase> aliveEnemies = new List<EnemyBase>();
    readonly List<EnemyBase> spawnQueue = new List<EnemyBase>();
    readonly List<float> spawnTimes = new List<float>();

    float waveTimer;
    int nextSpawnIndex;
    private Coroutine gameSpeedCoroutine;

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

    void Start()
    {
        if (autoStart && floors.Count > 0)
        {
            CurrentFloorIndex = 0;
            CurrentWaveIndexInFloor = -1;
            CurrentWaveState = WaveState.Intermission;
            waveTimer = initialDelay;
        }
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
        UpdateTimer();
        UpdateWaves();
    }

    void UpdateTimer()
    {
        if (!TimerRunning) return;

        TimeRemaining -= Time.deltaTime * countdownSpeed;

        if (TimeRemaining <= 0f)
        {
            TimeRemaining = 0f;
            TimerRunning = false;
            OnTimeExpired?.Invoke();
            ResetRun();
        }
    }

    void UpdateWaves()
    {
        switch (CurrentWaveState)
        {
            case WaveState.Intermission:
                waveTimer -= Time.deltaTime;
                if (waveTimer <= 0f) StartNextWave();
                break;

            case WaveState.Spawning:
                waveTimer += Time.deltaTime;
                ReleaseDueEnemies();

                if (nextSpawnIndex >= spawnQueue.Count)
                {
                    CurrentWaveState = WaveState.Clearing;
                    CheckWaveCleared();
                }
                break;

            case WaveState.Clearing:
                CheckWaveCleared();
                break;
        }
    }

    void ReleaseDueEnemies()
    {
        while (nextSpawnIndex < spawnQueue.Count && waveTimer >= spawnTimes[nextSpawnIndex])
        {
            SpawnEnemy(spawnQueue[nextSpawnIndex]);
            nextSpawnIndex++;
        }
    }

    void CheckWaveCleared()
    {
        aliveEnemies.RemoveAll(e => e == null);

        if (aliveEnemies.Count > 0) return;

        OnWaveCleared?.Invoke(CurrentWaveIndexInFloor);

        Floor floor = floors[CurrentFloorIndex];

        if (CurrentWaveIndexInFloor + 1 >= floor.waves.Count)
        {
            HandleFloorCleared();
            return;
        }

        CurrentWaveState = WaveState.Intermission;
        waveTimer = floor.waves[CurrentWaveIndexInFloor].intermission;
    }

    void HandleFloorCleared()
    {
        OnFloorCleared?.Invoke(CurrentFloorIndex);

        if (CurrentFloorIndex + 1 >= floors.Count)
        {
            CurrentWaveState = WaveState.Complete;
            OnAllFloorsCleared?.Invoke();
            return;
        }

        //freeze timer until next floor
        CurrentWaveState = WaveState.AwaitFloorAdvance;
        PauseTimer();
    }

    public void AdvanceToNextFloor()
    {
        if (CurrentWaveState != WaveState.AwaitFloorAdvance) return;

        ResumeTimer();
        StartWaveInFloor(CurrentFloorIndex + 1, 0);
    }

    public void StartNextWave()
    {
        StartWaveInFloor(CurrentFloorIndex, CurrentWaveIndexInFloor + 1);
    }

    void StartWaveInFloor(int floorIndex, int waveIndex)
    {
        if (floorIndex < 0 || floorIndex >= floors.Count)
        {
            Debug.LogWarning($"[GameManager] StartWaveInFloor called with invalid floor index {floorIndex}.");
            return;
        }

        Floor floor = floors[floorIndex];

        if (waveIndex < 0 || waveIndex >= floor.waves.Count)
        {
            Debug.LogWarning($"[GameManager] StartWaveInFloor called with invalid wave index {waveIndex} for floor {floorIndex}.");
            return;
        }

        bool isFirstWaveOfFloor = waveIndex == 0;

        CurrentFloorIndex = floorIndex;
        CurrentWaveIndexInFloor = waveIndex;
        Wave wave = floor.waves[waveIndex];

        BuildSpawnSchedule(wave);

        waveTimer = 0f;
        nextSpawnIndex = 0;
        CurrentWaveState = WaveState.Spawning;

        if (isFirstWaveOfFloor)
            OnFloorStarted?.Invoke(floorIndex);

        OnWaveStarted?.Invoke(waveIndex);

        ReleaseDueEnemies();

        if (nextSpawnIndex >= spawnQueue.Count)
        {
            CurrentWaveState = WaveState.Clearing;
            CheckWaveCleared();
        }
    }

    //public void StartWave(int waveIndex)
    //{
    //    if (waveIndex < 0 || waveIndex >= waves.Count)
    //    {
    //        Debug.LogWarning($"[GameManager] StartWave called with invalid index {waveIndex}.");
    //        return;
    //    }

    //    CurrentWaveIndex = waveIndex;
    //    Wave wave = waves[waveIndex];

    //    BuildSpawnSchedule(wave);

    //    waveTimer = 0f;
    //    nextSpawnIndex = 0;
    //    CurrentWaveState = WaveState.Spawning;

    //    OnWaveStarted?.Invoke(waveIndex);

    //    ReleaseDueEnemies();

    //    if (nextSpawnIndex >= spawnQueue.Count)
    //    {
    //        CurrentWaveState = WaveState.Clearing;
    //        CheckWaveCleared();
    //    }
    //}

    void BuildSpawnSchedule(Wave wave)
    {
        spawnQueue.Clear();
        spawnTimes.Clear();

        foreach (EnemySpawn spawn in wave.spawns)
        {
            if (spawn.enemyPrefab == null)
            {
                Debug.LogWarning($"[GameManager] Wave '{wave.name}' has a spawn entry with no prefab assigned.");
                continue;
            }

            for (int i = 0; i < spawn.count; i++)
                spawnQueue.Add(spawn.enemyPrefab);
        }

        Shuffle(spawnQueue);

        for (int i = 0; i < spawnQueue.Count; i++)
            spawnTimes.Add(UnityEngine.Random.Range(0f, wave.spawnDuration));

        spawnTimes.Sort();
    }

    static void Shuffle(List<EnemyBase> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    void SpawnEnemy(EnemyBase prefab)
    {
        EnemyBase enemy = Instantiate(prefab, GetRandomSpawnPosition(), Quaternion.identity);
        aliveEnemies.Add(enemy);
        enemy.OnDied += HandleEnemyDied;
    }

    void HandleEnemyDied(EnemyBase enemy)
    {
        enemy.OnDied -= HandleEnemyDied;
        aliveEnemies.Remove(enemy);
    }

    Vector3 GetRandomSpawnPosition()
    {
        if (spawnPoints.Count == 0)
        {
            Debug.LogWarning("[GameManager] No spawn points assigned; spawning at origin.");
            return Vector3.zero;
        }

        Transform point = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Count)];
        return point != null ? point.position : Vector3.zero;
    }

    public void AddTime(float seconds)
    {
        bool wasRunning = TimerRunning;
        TimeRemaining = Mathf.Max(0f, TimeRemaining + seconds);

        if (TimeRemaining > 0f)
        {
            TimerRunning = true;
        }
        else if (wasRunning)
        {
            TimerRunning = false;
            OnTimeExpired?.Invoke();
            ResetRun();
        }
    }

    public void SetCountdownSpeed(float speed)
    {
        countdownSpeed = speed;
    }

    public void PauseTimer() => TimerRunning = false;
    public void ResumeTimer() => TimerRunning = true;
    
    public void HandlePlayerDied() { ResetRun(); }

    public void ResetRun()
    {
        foreach (EnemyBase e in aliveEnemies)
        {
            if (e == null) continue;
            e.OnDied -= HandleEnemyDied;
            Destroy(e.gameObject);
        }

        aliveEnemies.Clear();
        spawnQueue.Clear();
        spawnTimes.Clear();
        nextSpawnIndex = 0;

        TimeRemaining = startTime;
        TimerRunning = true;

        CurrentFloorIndex = -1;
        CurrentWaveIndexInFloor = -1;
        CurrentWaveState = WaveState.Idle;

        OnRunReset?.Invoke();

        if (floors.Count > 0)
        {
            CurrentFloorIndex = 0;
            CurrentWaveIndexInFloor = -1;
            CurrentWaveState = WaveState.Intermission;
            waveTimer = initialDelay;
        }
    }

    public void GameSpeed(float speed, float duration, bool overrule)
    {
        if (gameSpeedCoroutine != null)
        {
            if (overrule)
            {
                StopCoroutine(gameSpeedCoroutine);
            } else
            {
                return;
            }
        }
        gameSpeedCoroutine = StartCoroutine(GameSpeeder(speed, duration));
    }

    private IEnumerator GameSpeeder(float speed, float duration)
    {
        float elapsed = 0.0f;
        Time.timeScale = speed;
        while (elapsed < duration)
        {
            if(SettingsButton.Instance == null || !SettingsButton.Instance.gamePaused)
            {
                elapsed += Time.unscaledDeltaTime;
            }
            yield return null;
        }
        Time.timeScale = 1;
        gameSpeedCoroutine = null;
    }
}
