using UnityEngine;
using UnityEngine.InputSystem;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;
    [SerializeField] private Vector2 offset = Vector2.zero;

    [Header("Smoothing")]
    [SerializeField] private float smoothTime = 0.18f;
    [SerializeField] private float maxSpeed = 40f;

    [Header("Deadzone")]
    [SerializeField] private float deadzoneRadius = 0.6f;

    [Header("Pixel Snap")]
    [SerializeField] private bool snapToPixelGrid = false;
    [SerializeField] private float pixelsPerUnit = 32f;

    [Header("Aim Bias")]
    [SerializeField] private float aimLookAhead = 1.4f;
    [SerializeField] private float aimLookAheadSmoothTime = 0.35f;
    [SerializeField] private float maxAimDistance = 6f;

    Camera cam;
    Vector3 followVelocity;
    Vector2 aimOffset;
    Vector2 aimOffsetVelocity;
    Vector2 anchor;

    void Awake()
    {
        cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;
    }

    void Start()
    {
        if (target == null && PlayerRef.Instance != null)
            target = PlayerRef.Instance.transform;

        if (target != null)
        {
            anchor = (Vector2)target.position + offset;
            transform.position = new Vector3(anchor.x, anchor.y, transform.position.z);
        }
    }

    void LateUpdate()
    {
        if (target == null)
        {
            if (PlayerRef.Instance == null) return;
            target = PlayerRef.Instance.transform;
            anchor = (Vector2)target.position + offset;
        }

        Vector2 targetPos = (Vector2)target.position + offset;

        Vector2 toTarget = targetPos - anchor;
        float distance = toTarget.magnitude;
        if (distance > deadzoneRadius)
            anchor += toTarget * ((distance - deadzoneRadius) / distance);

        aimOffset = Vector2.SmoothDamp(aimOffset, DesiredAimOffset(targetPos), ref aimOffsetVelocity, aimLookAheadSmoothTime);

        Vector3 desired = new Vector3(anchor.x + aimOffset.x, anchor.y + aimOffset.y, transform.position.z);
        Vector3 smoothed = Vector3.SmoothDamp(transform.position, desired, ref followVelocity, smoothTime, maxSpeed);

        if (snapToPixelGrid && pixelsPerUnit > 0f)
        {
            smoothed.x = Mathf.Round(smoothed.x * pixelsPerUnit) / pixelsPerUnit;
            smoothed.y = Mathf.Round(smoothed.y * pixelsPerUnit) / pixelsPerUnit;
        }

        transform.position = smoothed;
    }

    Vector2 DesiredAimOffset(Vector2 targetPos)
    {
        if (aimLookAhead <= 0f || cam == null || Mouse.current == null) return Vector2.zero;

        Vector2 screen = Mouse.current.position.ReadValue();
        Vector2 world = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -cam.transform.position.z));

        Vector2 toAim = world - targetPos;
        float distance = toAim.magnitude;
        if (distance < 0.0001f) return Vector2.zero;

        float scaled = Mathf.Min(distance, maxAimDistance) / maxAimDistance;
        return (toAim / distance) * scaled * aimLookAhead;
    }

    public void SnapToTarget()
    {
        if (target == null) return;

        anchor = (Vector2)target.position + offset;
        aimOffset = Vector2.zero;
        aimOffsetVelocity = Vector2.zero;
        followVelocity = Vector3.zero;
        transform.position = new Vector3(anchor.x, anchor.y, transform.position.z);
    }
}
