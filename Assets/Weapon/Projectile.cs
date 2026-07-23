using NUnit.Framework;
using UnityEngine;
public class Projectile : MonoBehaviour
{
    [SerializeField] private float lifetime = 3f;
    [SerializeField] private int damage = 1;
    [SerializeField] private GameObject aoePrefab;

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

    void OnCollisionEnter2D(Collision2D col)
    {
        // projectile hit enemy
        EnemyBase hitEnemy = null;
        if (col.collider.TryGetComponent<EnemyBase>(out var e))
        {
            e.TakeDamage(damage);
            hitEnemy = e;
        }

        // spawn AOE
        GameObject aoeObj = Instantiate(aoePrefab, transform.position, Quaternion.identity);
        aoeObj.GetComponent<AOE>().Init(hitEnemy);

        Destroy(gameObject);
    }
}
