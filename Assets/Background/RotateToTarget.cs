using UnityEngine;

public class RotateToTarget : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float magnitude = 1f;
    [SerializeField] private bool invert = false;

    private float lastTargetAngle;

    // Degrees per second this GameObject is currently turning at (after
    // magnitude/invert are applied) - lets other scripts (e.g. ScrollingFloor)
    // tie their own speed to how fast this gear is actually spinning.
    public float CurrentAngularSpeed { get; private set; }

    void Start()
    {
        if (target != null)
            lastTargetAngle = target.eulerAngles.z;
    }

    void Update()
    {
        if (target == null)
            return;

        float currentTargetAngle = target.eulerAngles.z;
        float delta = Mathf.DeltaAngle(lastTargetAngle, currentTargetAngle);

        float appliedDelta = delta * magnitude * (invert ? -1f : 1f);
        transform.Rotate(0f, 0f, appliedDelta);

        CurrentAngularSpeed = Time.deltaTime > 0f ? appliedDelta / Time.deltaTime : 0f;

        lastTargetAngle = currentTargetAngle;
    }
}
