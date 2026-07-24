using UnityEngine;

[System.Serializable]
public class EnemySteering
{
    [Header("Seek")]
    [SerializeField] private float seekWeight = 1f;
    [SerializeField] private float desiredRange = 0f;
    [SerializeField] private float retreatWeight = 1f;

    [Header("Separation")]
    [SerializeField] private float separationWeight = 1.5f;
    [SerializeField] private float separationRadius = 1.1f;

    [Header("Orbit")]
    [SerializeField] private float orbitWeight = 0.35f;
    [SerializeField] private bool randomizeOrbitDirection = true;

    [Header("Wander")]
    [SerializeField] private float wanderWeight = 0.2f;
    [SerializeField] private float wanderFrequency = 0.6f;

    [Header("Layers")]
    [SerializeField] private LayerMask enemyLayers = 1 << 8;

    const int MaxNeighbours = 16;
    static readonly Collider2D[] NeighbourBuffer = new Collider2D[MaxNeighbours];
    static ContactFilter2D neighbourFilter;
    static bool filterReady;

    float orbitSign = 1f;
    float wanderSeed;
    bool initialized;

    void EnsureInit()
    {
        if (initialized) return;
        orbitSign = !randomizeOrbitDirection || Random.value > 0.5f ? 1f : -1f;
        wanderSeed = Random.Range(0f, 100f);
        initialized = true;
    }

    public Vector2 GetDirection(Vector2 self, Vector2 playerPos)
    {
        EnsureInit();

        Vector2 toPlayer = playerPos - self;
        float distance = toPlayer.magnitude;
        Vector2 toPlayerDir = distance > 0.0001f ? toPlayer / distance : Vector2.zero;

        Vector2 steer = Vector2.zero;

        if (distance > desiredRange)
            steer += toPlayerDir * seekWeight;
        else
            steer -= toPlayerDir * retreatWeight;

        steer += Separation(self) * separationWeight;

        steer += new Vector2(-toPlayerDir.y, toPlayerDir.x) * orbitSign * orbitWeight;

        if (wanderWeight > 0f)
        {
            float noise = Mathf.PerlinNoise(wanderSeed, Time.time * wanderFrequency) * 2f - 1f;
            float angle = noise * Mathf.PI;
            steer += new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * wanderWeight;
        }

        return steer.sqrMagnitude > 0.0001f ? steer.normalized : Vector2.zero;
    }

    Vector2 Separation(Vector2 self)
    {
        if (separationWeight <= 0f || separationRadius <= 0f) return Vector2.zero;

        if (!filterReady)
        {
            neighbourFilter = new ContactFilter2D { useTriggers = false };
            filterReady = true;
        }
        neighbourFilter.SetLayerMask(enemyLayers);

        int count = Physics2D.OverlapCircle(self, separationRadius, neighbourFilter, NeighbourBuffer);
        count = Mathf.Min(count, NeighbourBuffer.Length);
        Vector2 push = Vector2.zero;

        for (int i = 0; i < count; i++)
        {
            Collider2D other = NeighbourBuffer[i];
            if (other == null) continue;

            Vector2 away = self - (Vector2)other.transform.position;
            float d = away.magnitude;

            if (d < 0.0001f) continue;

            push += (away / d) * (1f - Mathf.Clamp01(d / separationRadius));
        }

        return push;
    }
}
