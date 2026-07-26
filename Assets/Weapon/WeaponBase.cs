using UnityEngine;

public abstract class WeaponBase : MonoBehaviour
{
    [SerializeField] protected float cooldown = 0.4f;

    private float nextUseTime;

    public float CooldownRemaining => Mathf.Max(0f, nextUseTime - Time.time);
    public bool IsReady => Time.time >= nextUseTime;
    public virtual bool LockAim => false;

    protected virtual void Update() { }

    public void SetActiveWeapon(bool active)
    {
        gameObject.SetActive(active);
    }

    public void TryUse(Vector2 aimDir)
    {
        if (!IsReady) return;

        nextUseTime = Time.time + cooldown * CooldownMultiplier();
        Use(aimDir);
    }

    protected virtual float CooldownMultiplier() => 1f;

    protected abstract void Use(Vector2 aimDir);
}
