using UnityEngine;
using UnityEngine.InputSystem;
using System;

public class WeaponController : MonoBehaviour
{
    [SerializeField] private Transform pivot;
    [SerializeField] private bool flipWhenAimingLeft = true;
    [SerializeField] private WeaponBase[] weapons;
    [SerializeField] private int startingWeapon = 1;

    [Header("Scroll Switching")]
    [SerializeField] private float scrollDeadzone = 0.1f;
    [SerializeField] private bool invertScroll;

    private Camera cam;
    private Vector2 lookScreenPos;
    private Vector3 pivotBaseScale;
    private int activeIndex = -1;

    public int ActiveIndex => activeIndex;

    public event Action<int> OnWeaponChanged;

    void Awake()
    {
        cam = Camera.main;
        if (pivot == null) pivot = transform;
        pivotBaseScale = pivot.localScale;
    }

    void Start()
    {
        SelectWeapon(startingWeapon);
    }

    public void OnLook(InputValue value)
    {
        lookScreenPos = value.Get<Vector2>();
    }

    public void OnShoot(InputValue value)
    {
        if (!value.isPressed) return;
        WeaponBase active = ActiveWeapon();
        if (active != null)
            active.TryUse(AimDirection());
    }

    public void OnSelectSword(InputValue value)
    {
        if (value.isPressed) SelectWeapon(0);
    }

    public void OnSelectGun(InputValue value)
    {
        if (value.isPressed) SelectWeapon(1);
    }

    public void OnSelectPierceGun(InputValue value)
    {
        if (value.isPressed) SelectWeapon(2);
    }

    public void OnScrollWeapon(InputValue value)
    {
        float scroll = value.Get<float>();
        if (Mathf.Abs(scroll) < scrollDeadzone) return;

        int step = scroll > 0f ? 1 : -1;
        if (invertScroll) step = -step;

        CycleWeapon(step);
    }

    void Update()
    {
        if (cam == null || pivot == null) return;

        Vector2 dir = AimDirection();
        if (dir.sqrMagnitude < 0.0001f) return;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        pivot.rotation = Quaternion.Euler(0f, 0f, angle);

        if (flipWhenAimingLeft)
        {
            float sign = dir.x < 0f ? -1f : 1f;
            pivot.localScale = new Vector3(pivotBaseScale.x, pivotBaseScale.y * sign, pivotBaseScale.z);
        }
    }

    public void CycleWeapon(int step)
    {
        int count = WeaponCount();
        if (count <= 1) return;

        int index = activeIndex < 0 ? 0 : activeIndex;

        for (int i = 0; i < count; i++)
        {
            index = ((index + step) % count + count) % count;
            if (weapons[index] != null) break;
        }

        SelectWeapon(index);
    }

    void SelectWeapon(int index)
    {
        if (weapons == null || index < 0 || index >= weapons.Length) return;
        if (weapons[index] == null) return;

        activeIndex = index;
        for (int i = 0; i < weapons.Length; i++)
        {
            if (weapons[i] != null)
                weapons[i].SetActiveWeapon(i == index);
        }

        OnWeaponChanged?.Invoke(activeIndex);
    }

    int WeaponCount()
    {
        return weapons != null ? weapons.Length : 0;
    }

    WeaponBase ActiveWeapon()
    {
        if (weapons == null || activeIndex < 0 || activeIndex >= weapons.Length) return null;
        return weapons[activeIndex];
    }

    Vector2 AimDirection()
    {
        if (cam == null) return Vector2.right;
        Vector3 world = cam.ScreenToWorldPoint(new Vector3(lookScreenPos.x, lookScreenPos.y, -cam.transform.position.z));
        return (Vector2)world - (Vector2)pivot.position;
    }
}
