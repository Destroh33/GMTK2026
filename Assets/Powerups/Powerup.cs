using System.Collections.Generic;
using UnityEngine;

public class Powerup : MonoBehaviour
{
    [System.Serializable]
    public class TooltipSettings
    {
        public bool show = true;
        public Vector3 offset = new Vector3(0f, 0.85f, 0f);
        public float fontSize = 2.2f;
        public float width = 5f;
        public float height = 1.5f;
        public bool showRareDescription = true;
        public string promptKey = "E";
        public Color titleColor = Color.white;
        public int sortingOrder = 20;
    }

    [SerializeField] private UpgradePathData data;
    [SerializeField] private SpriteRenderer tintTarget;
    [SerializeField] private float bobHeight = 0.15f;
    [SerializeField] private float bobSpeed = 2f;

    [Header("Rare")]
    [SerializeField] private float rareScale = 1.35f;
    [SerializeField] private float rarePulseSpeed = 6f;
    [SerializeField] private float rarePulseAmount = 0.08f;

    [Header("Tooltip")]
    [SerializeField] private TooltipSettings tooltip = new TooltipSettings();

    public UpgradePathData Data => data;
    public bool IsRare => isRare;
    public bool PlayerInRange => playerInRange;

    static readonly List<Powerup> inRange = new List<Powerup>();

    PowerupSpawner owner;
    PowerupTooltip activeTooltip;
    Vector3 basePosition;
    Vector3 baseScale;
    float bobOffset;
    bool isRare;
    bool playerInRange;
    bool claimed;

    void Awake()
    {
        if (tintTarget == null) tintTarget = GetComponentInChildren<SpriteRenderer>();
        baseScale = transform.localScale;
    }

    void Start()
    {
        basePosition = transform.position;
        bobOffset = Random.Range(0f, Mathf.PI * 2f);
        ApplyLook();

        activeTooltip = PowerupTooltip.Create(transform, data, isRare, tooltip);
        if (activeTooltip != null) activeTooltip.SetPrompt(playerInRange);
    }

    void OnDestroy()
    {
        inRange.Remove(this);
        if (activeTooltip != null) Destroy(activeTooltip.gameObject);
    }

    void Update()
    {
        if (bobHeight > 0f)
        {
            float y = Mathf.Sin(Time.time * bobSpeed + bobOffset) * bobHeight;
            transform.position = basePosition + new Vector3(0f, y, 0f);
        }

        if (isRare && rarePulseAmount > 0f)
        {
            float pulse = 1f + Mathf.Sin(Time.time * rarePulseSpeed + bobOffset) * rarePulseAmount;
            transform.localScale = baseScale * rareScale * pulse;
        }
    }

    public void Init(UpgradePathData pathData, PowerupSpawner spawner, bool rare)
    {
        data = pathData;
        owner = spawner;
        isRare = rare;
        basePosition = transform.position;
        ApplyLook();
    }

    void ApplyLook()
    {
        if (data == null) return;

        if (tintTarget != null)
            tintTarget.color = isRare ? data.rareTint : data.tint;

        if (baseScale == Vector3.zero) baseScale = transform.localScale;
        transform.localScale = isRare ? baseScale * rareScale : baseScale;
    }

    public void Claim()
    {
        if (claimed || data == null) return;
        claimed = true;

        if (PlayerStats.Instance != null)
            PlayerStats.Instance.Upgrade(data, isRare);

        if (owner != null)
            owner.NotifyClaimed(this);

        Destroy(gameObject);
    }

    public static Powerup NearestInRange(Vector3 from)
    {
        Powerup best = null;
        float bestDistance = float.MaxValue;

        for (int i = inRange.Count - 1; i >= 0; i--)
        {
            Powerup p = inRange[i];

            if (p == null)
            {
                inRange.RemoveAt(i);
                continue;
            }

            float d = (p.transform.position - from).sqrMagnitude;
            if (d >= bestDistance) continue;

            bestDistance = d;
            best = p;
        }

        return best;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.gameObject.CompareTag("Player")) return;

        playerInRange = true;
        if (!inRange.Contains(this)) inRange.Add(this);
        if (activeTooltip != null) activeTooltip.SetPrompt(true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.gameObject.CompareTag("Player")) return;

        playerInRange = false;
        inRange.Remove(this);
        if (activeTooltip != null) activeTooltip.SetPrompt(false);
    }
}
