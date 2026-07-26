using UnityEngine;

// Spins the gear it's attached to, faster and faster as the Boss's health
// drops (auto-finds the Boss if left unassigned).
public class BossGearSpin : MonoBehaviour
{
    [SerializeField] private Boss boss;

    [Tooltip("Degrees/sec at full health.")]
    [SerializeField] private float minSpinSpeed = 30f;
    [Tooltip("Degrees/sec at zero health.")]
    [SerializeField] private float maxSpinSpeed = 300f;

    void Awake()
    {
        if (boss == null) boss = GetComponentInParent<Boss>();
        if (boss == null) boss = FindAnyObjectByType<Boss>();
    }

    void Update()
    {
        if (boss == null) return;

        float speed = Mathf.Lerp(minSpinSpeed, maxSpinSpeed, 1f - boss.HealthFraction);
        transform.Rotate(0f, 0f, speed * Time.deltaTime);
    }
}
