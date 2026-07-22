using UnityEngine;
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
