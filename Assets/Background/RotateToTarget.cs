using UnityEngine;

public class RotateToTarget : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float magnitude = 1f;
    [SerializeField] private bool invert = false;

    private float lastTargetAngle;

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

        lastTargetAngle = currentTargetAngle;
    }
}
