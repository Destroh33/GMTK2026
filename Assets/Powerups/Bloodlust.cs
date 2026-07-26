using UnityEngine;

public class Bloodlust : MonoBehaviour
{
    [Header("Stacks")]
    [SerializeField] private int baseMaxStacks = 5;
    [SerializeField] private int maxStacksPerLevel = 2;

    [Header("Duration")]
    [SerializeField] private float baseDuration = 3f;
    [SerializeField] private float durationPerLevel = 1.5f;

    [Header("Power Per Stack")]
    [SerializeField] private float damagePerStack = 0.08f;
    [SerializeField] private float damagePerStackPerLevel = 0.04f;
    [SerializeField] private float moveSpeedPerStack = 0.04f;
    [SerializeField] private float moveSpeedPerStackPerLevel = 0.02f;

    [Header("Feedback")]
    [SerializeField] private SpriteRenderer tintTarget;
    [SerializeField] private Color fullStackTint = new Color(1f, 0.35f, 0.35f, 0.85f);

    [Header("Particles")]
    [SerializeField] private Sprite particleSprite;
    [SerializeField] private Material particleMaterial;
    [SerializeField] private Color particleColor = new Color(0.85f, 0.05f, 0.1f, 1f);
    [SerializeField] private float particlesPerStack = 9f;
    [SerializeField] private float particleLifetime = 0.7f;
    [SerializeField] private float particleSpeed = 1.1f;
    [SerializeField] private float particleSize = 0.22f;
    [SerializeField] private float particleRadius = 0.35f;
    [SerializeField] private int particleSortingOrder = 4;

    int stacks;
    float timer;
    Color baseTint;
    bool hasBaseTint;
    ParticleSystem particles;
    ParticleSystem.EmissionModule emission;

    public int Stacks => stacks;
    public float TimeRemaining => timer;

    void Awake()
    {
        if (tintTarget != null)
        {
            baseTint = tintTarget.color;
            hasBaseTint = true;
        }

        BuildParticles();
    }

    void BuildParticles()
    {
        GameObject go = new GameObject("BloodlustParticles");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = Vector3.zero;

        particles = go.AddComponent<ParticleSystem>();
        particles.Stop();

        ParticleSystem.MainModule main = particles.main;
        main.loop = true;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = particleLifetime;
        main.startSpeed = particleSpeed;
        main.startSize = particleSize;
        main.startColor = particleColor;
        main.gravityModifier = -0.12f;

        emission = particles.emission;
        emission.rateOverTime = 0f;

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = particleRadius;
        shape.radiusThickness = 1f;

        ParticleSystem.ColorOverLifetimeModule fade = particles.colorOverLifetime;
        fade.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        fade.color = new ParticleSystem.MinMaxGradient(g);

        ParticleSystem.SizeOverLifetimeModule shrink = particles.sizeOverLifetime;
        shrink.enabled = true;
        shrink.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.2f));

        ParticleSystemRenderer psr = go.GetComponent<ParticleSystemRenderer>();
        psr.sortingOrder = particleSortingOrder;
        psr.material = ResolveParticleMaterial();
    }

    Material ResolveParticleMaterial()
    {
        if (particleMaterial != null) return particleMaterial;

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (shader == null) return null;

        Material m = new Material(shader);
        if (particleSprite != null) m.mainTexture = particleSprite.texture;

        return m;
    }

    void UpdateParticles()
    {
        if (particles == null) return;

        if (stacks > 0)
        {
            emission.rateOverTime = stacks * particlesPerStack;
            if (!particles.isPlaying) particles.Play();
        }
        else
        {
            emission.rateOverTime = 0f;
            if (particles.isPlaying) particles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }

    void OnEnable()
    {
        EnemyBase.OnAnyDied += HandleEnemyDied;
    }

    void OnDisable()
    {
        EnemyBase.OnAnyDied -= HandleEnemyDied;

        stacks = 0;
        timer = 0f;
        Apply();
    }

    void Update()
    {
        if (stacks <= 0) return;

        timer -= Time.deltaTime;

        if (timer <= 0f)
        {
            stacks = 0;
            timer = 0f;
            Apply();
        }
    }

    void HandleEnemyDied(EnemyBase enemy)
    {
        int level = PlayerStats.Rare(UpgradePath.Body);
        if (level <= 0) return;

        stacks = Mathf.Min(stacks + 1, baseMaxStacks + maxStacksPerLevel * (level - 1));
        timer = baseDuration + durationPerLevel * (level - 1);

        Apply();
    }

    void Apply()
    {
        int level = PlayerStats.Rare(UpgradePath.Body);

        float damageStep = damagePerStack + damagePerStackPerLevel * Mathf.Max(0, level - 1);
        float speedStep = moveSpeedPerStack + moveSpeedPerStackPerLevel * Mathf.Max(0, level - 1);

        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.BuffDamageMult = 1f + damageStep * stacks;
            PlayerStats.Instance.BuffMoveSpeedMult = 1f + speedStep * stacks;
        }

        UpdateParticles();

        if (tintTarget != null && hasBaseTint)
        {
            int max = Mathf.Max(1, baseMaxStacks + maxStacksPerLevel * Mathf.Max(0, level - 1));
            tintTarget.color = Color.Lerp(baseTint, fullStackTint, stacks / (float)max);
        }
    }
}
