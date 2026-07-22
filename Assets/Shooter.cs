using UnityEngine;
using UnityEngine.InputSystem;

public class Shooter : MonoBehaviour
{
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float projectileSpeed = 12f;
    [SerializeField] private Transform firePoint;

    private Camera cam;
    private Vector2 lookScreenPos;

    void Awake()
    {
        cam = Camera.main;
    }

    public void OnLook(InputValue value)
    {
        lookScreenPos = value.Get<Vector2>();
    }

    public void OnShoot(InputValue value)
    {
        if (value.isPressed)
            Shoot(lookScreenPos);
    }

    void Shoot(Vector2 screenPos)
    {
        if (projectilePrefab == null || cam == null) return;
        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;

        Vector2 dir;
        if (firePoint != null)
        {
            dir = ((Vector2)firePoint.right).normalized;
        }
        else
        {
            Vector3 world = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, -cam.transform.position.z));
            world.z = 0f;
            dir = ((Vector2)world - (Vector2)transform.position).normalized;
        }
        GameObject ball = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
        if (ball.TryGetComponent(out Rigidbody2D ballRb))
        {
            ballRb.linearVelocity = dir * projectileSpeed;
        }
        else if (ball.TryGetComponent(out Projectile proj))
        {
            proj.Launch(dir, projectileSpeed);
        }
    }
}
