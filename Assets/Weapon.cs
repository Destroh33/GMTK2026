using UnityEngine;
using UnityEngine.InputSystem;

public class Weapon : MonoBehaviour
{
    [SerializeField] private Transform pivotAround;
    [SerializeField] private bool flipWhenAimingLeft = true;

    private Camera cam;
    private Vector2 lookScreenPos;
    private Vector3 baseScale;

    void Awake()
    {
        cam = Camera.main;
        if (pivotAround == null && transform.parent != null)
            pivotAround = transform.parent;
        baseScale = transform.localScale;
    }

    public void OnLook(InputValue value)
    {
        lookScreenPos = value.Get<Vector2>();
    }

    void Update()
    {
        if (cam == null || pivotAround == null) return;

        Vector3 world = cam.ScreenToWorldPoint(new Vector3(lookScreenPos.x, lookScreenPos.y, -cam.transform.position.z));
        Vector2 dir = (Vector2)world - (Vector2)pivotAround.position;
        if (dir.sqrMagnitude < 0.0001f) return;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        if (flipWhenAimingLeft)
        {
            float sign = dir.x < 0f ? -1f : 1f;
            transform.localScale = new Vector3(baseScale.x, baseScale.y * sign, baseScale.z);
        }
    }
}
