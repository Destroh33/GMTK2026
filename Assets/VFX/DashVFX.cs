using UnityEngine;

public class DashVFX : MonoBehaviour
{
    [SerializeField] private float lifetime = 0.35f;
    [SerializeField] private bool orientToDirection = true;

    [Header("Placement")]
    [SerializeField] private float backOffset = 0.55f;
    [SerializeField] private float rotationOffset = 0f;

    public void Play(Vector2 direction)
    {
        Vector2 dir = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;

        transform.localPosition = -dir * backOffset;

        if (orientToDirection)
        {
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + rotationOffset;
            transform.localRotation = Quaternion.Euler(0f, 0f, angle);
        }
    }

    void Start()
    {
        Destroy(gameObject, lifetime);
    }
}
