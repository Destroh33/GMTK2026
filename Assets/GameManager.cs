using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum WaveState { Idle, Spawning, Clearing, Intermission, AwaitFloorAdvance, Complete }

    [Serializable]
    public class EnemyCost
    {
        public EnemyBase enemyPrefab;
        [Min(0)] public int cost = 1;
    }

    [Serializable]
    public class Wave
    {
        public string name;
        [Min(0)] public int purchasingPower = 10;
        [Min(0f)] public float spawnDuration = 8f;
        [Min(0f)] public float intermission = 4f;
    }

    [Serializable]
    public class Floor
    {
        public string name;
        public List<Wave> waves = new List<Wave>();
        public List<EnemyBase> enemyPool = new List<EnemyBase>();
    }

    [Header("Timer")]
    [SerializeField] private float startTime = 30f * 60f;
    [SerializeField] private float countdownSpeed = 1f;

    public float StartTime => startTime;
    public float CountdownSpeed => countdownSpeed;

    [Header("Enemy Costs")]
    [Tooltip("Add enemy prefabs and their costs. this will be the pool of enemies for this wave.")]
    [SerializeField] private List<EnemyCost> enemyCosts = new List<EnemyCost>();

    [Header("Floors")]
    [SerializeField] private List<Floor> floors = new List<Floor>();
    [SerializeField] private List<Transform> spawnPoints = new List<Transform>();
    [SerializeField] private bool autoStart = true;
    [SerializeField] private float initialDelay = 2f;

    [Header("First Floor Gate")]
    [SerializeField] private bool gateFirstFloorRewards = true;
    [SerializeField] private int gateFloorIndex = 0;
    [SerializeField] private float gateReleaseDelay = 1f;

    public float TimeRemaining { get; private set; }
    public bool TimerRunning { get; private set; }
    public int CurrentFloorIndex { get; private set; } = -1;
    public int CurrentWaveIndexInFloor { get; private set; } = -1;
    public WaveState CurrentWaveState { get; private set; } = WaveState.Idle;
    public int AliveEnemyCount => aliveEnemies.Count;
    public int PendingSpawnCount => spawnQueue.Count;
    public int FloorCount => floors.Count;
    public bool AwaitingPowerupChoice { get; private set; }
    public bool TutorialHold { get; private set; }
    public bool ClockFrozen => !TimerRunning || AwaitingPowerupChoice || TutorialHold;

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

    Dictionary<EnemyBase, int> costlookup;

    float waveTimer;
    int nextSpawnIndex;
    bool struckClockThisRun;
    bool awaitingGateStrike;
    Coroutine gateReleaseRoutine;
    private Coroutine gameSpeedCoroutine;

    float baseTimeScale = 1f;
    float hitstopScale;
    float hitstopTimer;
    Coroutine hitstopCoroutine;

    public bool AwaitingGateStrike => awaitingGateStrike;

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

        BuildCostLookup();
    }

    void BuildCostLookup()
    {
        costlookup = new Dictionary<EnemyBase, int>();

        foreach (EnemyCost entry in enemyCosts)
        {
            if (entry.enemyPrefab == null)
            {
                Debug.LogWarning("[GameManager] Enemy cost entry has no prefab assigned.");
                continue;
            }

            if (costlookup.ContainsKey(entry.enemyPrefab))
            {
                Debug.LogWarning($"[GameManager] Duplicate cost entry for '{entry.enemyPrefab.name}'; using the first one.");
                continue;
            }

            costlookup.Add(entry.enemyPrefab, entry.cost);
        }
    }

    int GetCost(EnemyBase prefab)
    {
        if (prefab == null) return 0;
        return costlookup != null && costlookup.TryGetValue(prefab, out int cost) ? cost : 0;
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

    void OnEnable()
    {
        StageClock.OnAnyHandStruck += HandleAnyHandStruck;
    }

    void OnDisable()
    {
        StageClock.OnAnyHandStruck -= HandleAnyHandStruck;
    }

    void HandleAnyHandStruck()
    {
        struckClockThisRun = true;

        if (!awaitingGateStrike || gateReleaseRoutine != null) return;

        gateReleaseRoutine = StartCoroutine(ReleaseGateAfterDelay());
    }

    IEnumerator ReleaseGateAfterDelay()
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, gateReleaseDelay));

        gateReleaseRoutine = null;
        ReleaseFloorGate();
    }

    public void ReleaseFloorGate()
    {
        if (!awaitingGateStrike) return;

        awaitingGateStrike = false;

        if (gateReleaseRoutine != null)
        {
            StopCoroutine(gateReleaseRoutine);
            gateReleaseRoutine = null;
        }

        CompleteFloorCleared();
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
        if (!TimerRunning || AwaitingPowerupChoice || TutorialHold) return;

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
        if (AwaitingPowerupChoice || TutorialHold) return;

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

            PlayerStats.Instance?.SnapshotForBoss();
            PlayerHealth.Instance?.SnapshotForBoss();

            SceneManager.LoadScene("BossScene");
            return;
        }

        //freeze timer until next floor
        CurrentWaveState = WaveState.AwaitFloorAdvance;
        PauseTimer();
    }

    void CompleteFloorCleared()
    {
        OnFloorCleared?.Invoke(CurrentFloorIndex);

        if (CurrentFloorIndex + 1 >= floors.Count)
        {
            CurrentWaveState = WaveState.Complete;
            OnAllFloorsCleared?.Invoke();
            SceneManager.LoadScene("BossScene");
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

        BuildSpawnSchedule(floor, wave);

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

    void BuildSpawnSchedule(Floor floor, Wave wave)
    {
        spawnQueue.Clear();
        spawnTimes.Clear();

        if (floor.enemyPool == null || floor.enemyPool.Count == 0)
        {
            Debug.LogWarning($"[GameManager] Floor '{floor.name}' has no enemies in its pool; wave '{wave.name}' will have no spawns.");
            return;
        }

        int wallet = wave.purchasingPower;

        // loop till wallet is empty OR the cheapest enemy isnt purchaseable
        while (wallet > 0 && CheapestAffordable(floor.enemyPool, wallet) >= 0)
        {
            EnemyBase browsedEnemy = floor.enemyPool[UnityEngine.Random.Range(0, floor.enemyPool.Count)];
            int cost = GetCost(browsedEnemy);

            if (cost <= 0)
            {
                Debug.LogWarning($"[GameManager] '{(browsedEnemy != null ? browsedEnemy.name : "null")}' has no cost entry (or cost 0); it will never be purchased.");
                continue;
            }

            if (cost <= wallet)
            {
                spawnQueue.Add(browsedEnemy);
                wallet -= cost;
            }
        }

        Shuffle(spawnQueue);

        for (int i = 0; i < spawnQueue.Count; i++)
            spawnTimes.Add(UnityEngine.Random.Range(0f, wave.spawnDuration));

        spawnTimes.Sort();
    }

    int CheapestAffordable(List<EnemyBase> shop, int budget)
    {
        int cheapest = int.MaxValue;

        foreach (EnemyBase e in shop)
        {
            int cost = GetCost(e);
            if (cost > 0 && cost <= budget && cost < cheapest)
                cheapest = cost;
        }

        return cheapest == int.MaxValue ? -1 : cheapest;
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

    public void BeginPowerupSelection()
    {
        AwaitingPowerupChoice = true;
    }

    public void EndPowerupSelection()
    {
        AwaitingPowerupChoice = false;
    }

    public void BeginTutorialHold()
    {
        TutorialHold = true;
    }

    public void EndTutorialHold()
    {
        TutorialHold = false;
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
        struckClockThisRun = false;
        awaitingGateStrike = false;

        if (gateReleaseRoutine != null)
        {
            StopCoroutine(gateReleaseRoutine);
            gateReleaseRoutine = null;
        }

        TimeRemaining = startTime;
        TimerRunning = true;

        CurrentFloorIndex = -1;
        CurrentWaveIndexInFloor = -1;
        CurrentWaveState = WaveState.Idle;
        AwaitingPowerupChoice = false;
        TutorialHold = false;

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

    public void Hitstop(float duration, float scale = 0f)
    {
        if (duration <= 0f) return;
        if (ExternallyPaused()) return;

        hitstopScale = Mathf.Clamp01(scale);
        hitstopTimer = Mathf.Max(hitstopTimer, duration);

        if (hitstopCoroutine == null)
            hitstopCoroutine = StartCoroutine(Hitstopper());
    }

    private IEnumerator Hitstopper()
    {
        while (hitstopTimer > 0f)
        {
            RefreshTimeScale();

            if (!ExternallyPaused())
                hitstopTimer -= Time.unscaledDeltaTime;

            yield return null;
        }

        hitstopTimer = 0f;
        hitstopCoroutine = null;
        RefreshTimeScale();
    }

    static bool ExternallyPaused()
    {
        return (SettingsButton.Instance != null && SettingsButton.Instance.gamePaused)
            || TutorialDirector.WorldPaused;
    }

    public void RefreshTimeScale()
    {
        if (ExternallyPaused()) return;

        Time.timeScale = hitstopTimer > 0f ? hitstopScale : baseTimeScale;
    }

    private IEnumerator GameSpeeder(float speed, float duration)
    {
        float elapsed = 0.0f;
        baseTimeScale = speed;
        RefreshTimeScale();

        while (elapsed < duration)
        {
            if (!ExternallyPaused())
            {
                elapsed += Time.unscaledDeltaTime;
            }
            yield return null;
        }

        baseTimeScale = 1f;
        RefreshTimeScale();
        gameSpeedCoroutine = null;
    }
}
