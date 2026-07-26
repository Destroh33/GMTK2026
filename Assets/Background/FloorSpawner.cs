using System.Collections;
using UnityEngine;

// Builds the vertical floor stack piece by piece:
// - Spawns the starting floor once, at this object's position.
// - Spawns a new "in between" floor, a serialized distance above the last
//   floor, every time GameManager starts a new wave (OnWaveStarted) - this
//   keeps the stack growing for as long as waves keep starting.
// - When a floor's enemies are all cleared (GameManager.OnFloorCleared),
//   spawns the intermediary floor a (separately serialized) distance above
//   the last floor, then pauses in-between spawning for a serialized amount
//   of time before resuming normal in-between spawning.
// Note: this only manages floor-piece geometry - it does NOT call
// GameManager.AdvanceToNextFloor(); something else is responsible for that.
public class FloorSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private Transform startingFloorPrefab;
    [SerializeField] private Transform inBetweenPrefab;
    [SerializeField] private Transform intermediaryPrefab;

    [Header("Spacing")]
    [SerializeField] private float inBetweenDistance = 5f;
    [SerializeField] private float intermediaryDistance = 8f;

    [Header("Timing")]
    [SerializeField] private float pauseDuration = 3f;

    private Transform lastFloor;
    private bool spawningPaused;

    void Awake()
    {
        lastFloor = startingFloorPrefab != null
            ? Instantiate(startingFloorPrefab, transform.position, Quaternion.identity, transform)
            : transform;
    }

    void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnWaveStarted += HandleWaveStarted;
            GameManager.Instance.OnFloorCleared += HandleFloorCleared;
        }
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnWaveStarted -= HandleWaveStarted;
            GameManager.Instance.OnFloorCleared -= HandleFloorCleared;
        }
    }

    void HandleWaveStarted(int waveIndex)
    {
        if (spawningPaused) return;
        SpawnInBetween();
    }

    void HandleFloorCleared(int floorIndex)
    {
        SpawnIntermediary();
        StartCoroutine(PauseThenResume());
    }

    void SpawnInBetween()
    {
        if (inBetweenPrefab == null || lastFloor == null) return;

        Vector3 spawnPos = lastFloor.position + Vector3.up * inBetweenDistance;
        lastFloor = Instantiate(inBetweenPrefab, spawnPos, Quaternion.identity, transform);
    }

    void SpawnIntermediary()
    {
        if (intermediaryPrefab == null || lastFloor == null) return;

        Vector3 spawnPos = lastFloor.position + Vector3.up * intermediaryDistance;
        lastFloor = Instantiate(intermediaryPrefab, spawnPos, Quaternion.identity, transform);
    }

    IEnumerator PauseThenResume()
    {
        spawningPaused = true;
        yield return new WaitForSeconds(pauseDuration);
        spawningPaused = false;
    }
}
