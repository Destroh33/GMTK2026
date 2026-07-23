using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponController : MonoBehaviour
{
    [SerializeField] private Transform pivot;
    [SerializeField] private bool flipWhenAimingLeft = true;
    [SerializeField] private WeaponBase[] weapons;
    [SerializeField] private int startingWeapon = 1;

    private Camera cam;
    private Vector2 lookScreenPos;
    private Vector3 pivotBaseScale;
    private int activeIndex = -1;

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

    void SelectWeapon(int index)
    {
        if (weapons == null || index < 0 || index >= weapons.Length) return;
        activeIndex = index;
        for (int i = 0; i < weapons.Length; i++)
        {
            if (weapons[i] != null)
                weapons[i].SetActiveWeapon(i == index);
        }
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
