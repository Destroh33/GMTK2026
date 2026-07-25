using UnityEngine;

public class BulletDetector : MonoBehaviour
{
    private ShieldEnemy parent;

    void Start()
    {
        parent = GetComponentInParent<ShieldEnemy>();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        parent.OnChildTrigger(other);
    }
}
