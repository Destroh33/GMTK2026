using System.Collections;
using UnityEngine;

public class ClockTurret : MonoBehaviour
{

    private Vector2 target;
    private GameManager gameManager;
    private Transform playerpos;
    private float distFromPlayer;

    [SerializeField] Transform clockHandTurretPos;
    [SerializeField] private GameObject bullet;
    [SerializeField] private Transform bulletSpawnPos;
    [SerializeField] private float bulletSpeed;
    [SerializeField] private AnimationCurve precisionCurve;
    [SerializeField] private AnimationCurve coolDownCurve;

    private void Start()
    {
        gameManager = GameManager.Instance;
        playerpos = FindAnyObjectByType<PlayerMovement>().transform;
        StartCoroutine(FindTargetAndShoot());
    }

    void Update()
    {
        // Attach visually without actually being a child in the hierarchy
        transform.position = clockHandTurretPos.position;
    }

    private void FixedUpdate()
    {
        distFromPlayer = Vector2.Distance(transform.position, playerpos.position);
    }

    IEnumerator FindTargetAndShoot() 
    {
        while (true)
        {
            target = (Vector2)playerpos.position + (Random.onUnitCircle * precisionCurve.Evaluate(distFromPlayer));
            Vector2 direction = target - (Vector2)transform.position;
            transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            EnemyProjectile p = Instantiate(bullet, bulletSpawnPos.position, Quaternion.identity).GetComponent<EnemyProjectile>();
            p.Launch((target - (Vector2)transform.position).normalized, bulletSpeed);

            yield return new WaitForSeconds(coolDownCurve.Evaluate(distFromPlayer));
        }
        
    }
}
