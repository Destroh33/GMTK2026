using UnityEngine;

// Optional fallback mover for projectiles that don't use a Rigidbody2D.
// Also auto-destroys the ball after a lifetime so they don't pile up.
public class Projectile : MonoBehaviour
{
    [SerializeField] private float lifetime = 3f;

    private Vector2 velocity;
    private bool launched;

    public void Launch(Vector2 direction, float speed)
    {
        velocity = direction * speed;
        launched = true;
    }

    void Start()
    {
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        if (launched)
            transform.position += (Vector3)(velocity * Time.deltaTime);
    }
}
