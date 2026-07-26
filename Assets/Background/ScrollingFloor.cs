using UnityEngine;

// Attach directly to a floor prefab (starting floor, in-between, or
// intermediary) - each spawned instance scrolls on its own, driven by
// however much the tracked clock hand is currently turning: normal forward
// sweep moves it down, a reversed/struck sweep moves it back up by the same
// proportional amount. It destroys itself once it's fallen far enough below
// the camera's view.
//
// The intermediary piece (freezeAtPowerupY enabled) is the one that decides
// when the whole world pauses: once IT scrolls down to freezeY during a
// powerup choice, it sets a shared/static freeze flag that every
// ScrollingFloor instance checks - so everything stops together, not just
// the intermediary. The flag clears (and everything resumes) once the
// powerup choice is resolved.
public class ScrollingFloor : MonoBehaviour
{
    [Tooltip("Tracks the clock hand (or anything else) whose signed turn speed drives this piece's scroll - forward turning moves it down, reversed turning moves it back up.")]
    [SerializeField] private RotateToTarget gear;
    [SerializeField] private float speedMultiplier = 1f;
    [Tooltip("Used instead of the gear if no gear is assigned.")]
    [SerializeField] private float fallbackSpeed = 2f;

    [SerializeField] private float despawnDistanceBelowScreen = 5f;

    [Tooltip("Seconds after scene start before this piece begins scrolling at all.")]
    [SerializeField] private float initialPauseDuration = 0f;

    [Header("Powerup Transition Freeze")]
    [Tooltip("Only enable this on the intermediary/transition prefab. Once this piece scrolls down to freezeY during a powerup choice, it freezes EVERY floor piece (not just itself) until the choice is resolved.")]
    [SerializeField] private bool freezeAtPowerupY = false;
    [SerializeField] private float freezeY = 0f;

    // Shared across every ScrollingFloor instance - set true by whichever
    // piece has freezeAtPowerupY once it reaches freezeY, cleared once the
    // powerup choice ends.
    private static bool worldFrozen;

    private Camera cam;

    void Start()
    {
        cam = Camera.main;

        if (gear == null)
        {
            Transform targetGear = transform.Find("TARGET GEAR");
            if (targetGear != null)
                gear = targetGear.GetComponent<RotateToTarget>();
        }
    }

    void Update()
    {
        if (Time.time < initialPauseDuration)
            return;

        if (worldFrozen)
        {
            if (GameManager.Instance == null || !GameManager.Instance.AwaitingPowerupChoice)
                worldFrozen = false;
            else
                return;
        }

        float speed = gear != null ? gear.CurrentAngularSpeed * speedMultiplier : fallbackSpeed;
        transform.position += Vector3.down * speed * Time.deltaTime;

        if (freezeAtPowerupY
            && GameManager.Instance != null
            && GameManager.Instance.AwaitingPowerupChoice
            && transform.position.y <= freezeY)
        {
            Vector3 pos = transform.position;
            pos.y = freezeY;
            transform.position = pos;
            worldFrozen = true;
            return;
        }

        if (IsBelowScreen())
            Destroy(gameObject);
    }

    bool IsBelowScreen()
    {
        if (cam == null)
        {
            cam = Camera.main;
            if (cam == null) return false;
        }

        float cameraBottom = cam.transform.position.y - cam.orthographicSize;
        return transform.position.y < cameraBottom - despawnDistanceBelowScreen;
    }
}
