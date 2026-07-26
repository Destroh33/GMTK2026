using UnityEngine;

// Builds the vertical floor stack piece by piece:
// - Spawns the starting floor once, at startingFloorSpawnPos.
// - Keeps a stream of "in between" floors going: every one spawns at the
//   fixed inBetweenSpawnPos, and a new one spawns as soon as the most
//   recently spawned floor piece has scrolled down to (or past)
//   inBetweenTriggerY - so the stream is driven purely by how far the last
//   piece has fallen, not by waves.
// - The FIRST time a powerup choice comes up (GameManager.AwaitingPowerupChoice
//   turns true, which happens once a floor's enemies are all cleared), spawns
//   the intermediary/transition floor at intermediarySpawnPos - it only ever
//   spawns once per run, not on every subsequent powerup choice. Every time a
//   powerup choice is up (first or not), in-between spawning pauses; the
//   moment the choice is resolved (AwaitingPowerupChoice turns back off), the
//   in-between stream resumes.
// Note: this only manages floor-piece geometry - it does NOT call
// GameManager.AdvanceToNextFloor(); something else is responsible for that.
public class FloorSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private Transform startingFloorPrefab;
    [SerializeField] private Transform inBetweenPrefab;
    [SerializeField] private Transform intermediaryPrefab;

    [Header("Spawn Positions")]
    [SerializeField] private Vector3 startingFloorSpawnPos;
    [SerializeField] private Vector3 inBetweenSpawnPos;
    [SerializeField] private Vector3 intermediarySpawnPos;

    [Header("In Between Stream")]
    [Tooltip("Once the last-spawned in-between floor's Y drops to (or below) this, the next one spawns.")]
    [SerializeField] private float inBetweenTriggerY = 0f;

    private Transform lastFloor;
    private bool spawningPaused;
    private bool wasAwaitingPowerupChoice;
    private bool intermediarySpawned;

    void Awake()
    {
        lastFloor = startingFloorPrefab != null
            ? Instantiate(startingFloorPrefab, startingFloorSpawnPos, Quaternion.identity, transform)
            : transform;
    }

    void Start()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnRunReset += HandleRunReset;
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnRunReset -= HandleRunReset;
    }

    void HandleRunReset()
    {
        intermediarySpawned = false;
    }

    void Update()
    {
        HandlePowerupState();

        if (!spawningPaused && ShouldSpawnNextInBetween())
            SpawnInBetween();
    }

    void HandlePowerupState()
    {
        if (GameManager.Instance == null) return;

        bool awaiting = GameManager.Instance.AwaitingPowerupChoice;

        if (awaiting && !wasAwaitingPowerupChoice)
        {
            if (!intermediarySpawned)
            {
                SpawnIntermediary();
                intermediarySpawned = true;
            }

            spawningPaused = true;
        }
        else if (!awaiting && wasAwaitingPowerupChoice)
        {
            spawningPaused = false;
        }

        wasAwaitingPowerupChoice = awaiting;
    }

    bool ShouldSpawnNextInBetween()
    {
        return lastFloor == null || lastFloor.position.y <= inBetweenTriggerY;
    }

    void SpawnInBetween()
    {
        if (inBetweenPrefab == null) return;

        lastFloor = Instantiate(inBetweenPrefab, inBetweenSpawnPos, Quaternion.identity, transform);
    }

    void SpawnIntermediary()
    {
        if (intermediaryPrefab == null) return;

        lastFloor = Instantiate(intermediaryPrefab, intermediarySpawnPos, Quaternion.identity, transform);
    }
}
