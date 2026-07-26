using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class HealthPickup : MonoBehaviour
{
    [SerializeField] private float healAmount = 1f;
    [SerializeField] private float lifetime = 15f;
    [SerializeField] private float bobHeight = 0.12f;
    [SerializeField] private float bobSpeed = 3f;

    private Vector3 basePosition;
    private float lifeTimer;

    void Start()
    {
        basePosition = transform.position;
    }

    void Update()
    {
        lifeTimer += Time.deltaTime;

        if (lifetime > 0f && lifeTimer >= lifetime)
        {
            Destroy(gameObject);
            return;
        }

        transform.position = basePosition + Vector3.up * (Mathf.Sin(lifeTimer * bobSpeed) * bobHeight);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.TryGetComponent(out PlayerHealth player)) return;

        if (!player.Heal(healAmount)) return;

        Destroy(gameObject);
    }
}
