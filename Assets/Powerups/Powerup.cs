using UnityEngine;

public class Powerup : MonoBehaviour
{
    [SerializeField] private UpgradePathData data;
    [SerializeField] private SpriteRenderer tintTarget;
    [SerializeField] private float bobHeight = 0.15f;
    [SerializeField] private float bobSpeed = 2f;

    public UpgradePathData Data => data;

    PowerupSpawner owner;
    Vector3 basePosition;
    float bobOffset;

    void Awake()
    {
        if (tintTarget == null) tintTarget = GetComponentInChildren<SpriteRenderer>();
    }

    void Start()
    {
        basePosition = transform.position;
        bobOffset = Random.Range(0f, Mathf.PI * 2f);
        ApplyTint();
    }

    void Update()
    {
        if (bobHeight <= 0f) return;

        float y = Mathf.Sin(Time.time * bobSpeed + bobOffset) * bobHeight;
        transform.position = basePosition + new Vector3(0f, y, 0f);
    }

    public void Init(UpgradePathData pathData, PowerupSpawner spawner)
    {
        data = pathData;
        owner = spawner;
        basePosition = transform.position;
        ApplyTint();
    }

    void ApplyTint()
    {
        if (tintTarget != null && data != null) tintTarget.color = data.tint;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.gameObject.CompareTag("Player")) return;
        if (data == null) return;

        if (PlayerStats.Instance != null)
            PlayerStats.Instance.Upgrade(data);

        if (owner != null)
            owner.NotifyClaimed(this);

        Destroy(gameObject);
    }
}
