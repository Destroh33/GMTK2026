using UnityEngine;

// One-shot dash streak. Spawned by PlayerMovement as a child of the player so it
// travels with them for the length of the dash, then destroyed once its
// (non-looping) clip has played out.
public class DashVFX : MonoBehaviour
{
    [SerializeField] private float lifetime = 0.35f;
    [SerializeField] private bool orientToDirection = true;
    [SerializeField] private Vector2 localOffset = Vector2.zero;

    public void Play(Vector2 direction)
    {
        transform.localPosition = localOffset;

        if (orientToDirection && direction.sqrMagnitude > 0.0001f)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.localRotation = Quaternion.Euler(0f, 0f, angle);
        }
    }

    void Start()
    {
        Destroy(gameObject, lifetime);
    }
}
