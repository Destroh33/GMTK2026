using UnityEngine;

public abstract class WeaponBase : MonoBehaviour
{
    [SerializeField] protected float cooldown = 0.4f;

    // test comment
    private float cooldownTimer;

    protected virtual void Update()
    {
        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;
    }

    public void SetActiveWeapon(bool active)
    {
        gameObject.SetActive(active);
    }

    public void TryUse(Vector2 aimDir)
    {
        if (cooldownTimer > 0f) return;
        cooldownTimer = cooldown;
        Use(aimDir);
    }

    protected abstract void Use(Vector2 aimDir);
}
