using System.Collections;
using UnityEngine;

// Reads the Boss's current action (via Boss.OnActionChanged) and drives the
// arena's lighting reaction to it:
// - Teleport: swaps the map to its lit sprite for a moment (a flash), then
//   back to the non-lit (default) sprite.
// - Spawning (bats or skeleton): fades the map's tint down to a random
//   darkened amount within a range, holds, then fades it back up to normal.
// A separate "big background" renderer's color is pushed to match the same
// lit/dark state on the exact same timing as the map, via its own colors.
public class BossBackgroundManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Boss boss;
    [SerializeField] private SpriteRenderer bigBackgroundRenderer;
    [Tooltip("The Boss's own sprite, for the dash tint. Auto-found from Boss if left unassigned.")]
    [SerializeField] private SpriteRenderer bossSpriteRenderer;

    [Header("Map GameObjects (children)")]
    [Tooltip("Active by default.")]
    [SerializeField] private GameObject nonLitMap;
    [SerializeField] private GameObject litMap;

    [Header("Teleport Flash")]
    [SerializeField] private float teleportLitDuration = 0.5f;
    [SerializeField] private Color bigBackgroundLitColor = Color.white;

    [Header("Spawn Darken")]
    [Tooltip("How long the fade down and fade back up each take.")]
    [SerializeField] private float spawnFadeDuration = 0.2f;
    [Tooltip("How long it stays fully darkened between the two fades.")]
    [SerializeField] private float spawnDarkenDuration = 0.5f;
    [SerializeField] private float spawnDarkenMin = 0.3f;
    [SerializeField] private float spawnDarkenMax = 0.6f;
    [SerializeField] private Color bigBackgroundDarkColor = new Color(0.35f, 0.35f, 0.35f);

    [Header("Dash Tint")]
    [SerializeField] private Color dashTintColor = Color.red;
    [Tooltip("How long the tint holds before it starts fading back.")]
    [SerializeField] private float dashTintHoldDuration = 0.2f;
    [SerializeField] private float dashTintFadeDuration = 0.15f;

    private SpriteRenderer nonLitMapRenderer;
    private Color mapNormalColor = Color.white;
    private Color bigBackgroundNormalColor = Color.white;
    private Color bossNormalColor = Color.white;
    private Coroutine activeRoutine;
    private Coroutine dashTintRoutine;

    void Awake()
    {
        if (nonLitMap != null)
        {
            nonLitMapRenderer = nonLitMap.GetComponent<SpriteRenderer>();
            if (nonLitMapRenderer != null) mapNormalColor = nonLitMapRenderer.color;
        }

        SetMapLit(false);

        if (bigBackgroundRenderer != null)
            bigBackgroundNormalColor = bigBackgroundRenderer.color;
    }

    void OnEnable()
    {
        TrySubscribe();
    }

    void Start()
    {
        TrySubscribe();
    }

    void OnDisable()
    {
        if (boss != null)
            boss.OnActionChanged -= HandleActionChanged;
    }

    void TrySubscribe()
    {
        if (boss == null)
            boss = FindAnyObjectByType<Boss>();

        if (boss == null) return;

        if (bossSpriteRenderer == null)
        {
            bossSpriteRenderer = boss.GetComponent<SpriteRenderer>();
            if (bossSpriteRenderer != null) bossNormalColor = bossSpriteRenderer.color;
        }

        boss.OnActionChanged -= HandleActionChanged; // avoid double-subscribing
        boss.OnActionChanged += HandleActionChanged;
    }

    void HandleActionChanged(Boss.BossAction action)
    {
        switch (action)
        {
            case Boss.BossAction.Teleport:
                HandleTeleport();
                break;
            case Boss.BossAction.SpawnBats:
                HandleSpawnBats();
                break;
            case Boss.BossAction.Dash:
                HandleDash();
                break;
            case Boss.BossAction.ShootProjectiles:
                HandleShootProjectiles();
                break;
            case Boss.BossAction.SpawnSkeleton:
                HandleSpawnSkeleton();
                break;
        }
    }

    // --- Per-action reactions ---

    void HandleTeleport()
    {
        RunSequence(TeleportFlash());
    }

    void HandleSpawnBats()
    {
        RunSequence(SpawnDarken());
    }

    void HandleDash()
    {
        if (bossSpriteRenderer == null) return;

        if (dashTintRoutine != null) StopCoroutine(dashTintRoutine);
        dashTintRoutine = StartCoroutine(DashTintThenRevert());
    }

    void HandleShootProjectiles()
    {
    }

    void HandleSpawnSkeleton()
    {
        RunSequence(SpawnDarken());
    }

    // --- Sequences ---

    void RunSequence(IEnumerator routine)
    {
        if (activeRoutine != null)
            StopCoroutine(activeRoutine);

        activeRoutine = StartCoroutine(routine);
    }

    IEnumerator TeleportFlash()
    {
        SetMapLit(true);
        SetBigBackgroundColor(bigBackgroundLitColor);

        yield return new WaitForSeconds(teleportLitDuration);

        SetMapLit(false);
        SetBigBackgroundColor(bigBackgroundNormalColor);

        activeRoutine = null;
    }

    IEnumerator SpawnDarken()
    {
        float darkenAmount = Random.Range(spawnDarkenMin, spawnDarkenMax);
        Color darkColor = new Color(darkenAmount, darkenAmount, darkenAmount, mapNormalColor.a);

        yield return FadeColors(mapNormalColor, darkColor, bigBackgroundNormalColor, bigBackgroundDarkColor, spawnFadeDuration);

        yield return new WaitForSeconds(spawnDarkenDuration);

        yield return FadeColors(darkColor, mapNormalColor, bigBackgroundDarkColor, bigBackgroundNormalColor, spawnFadeDuration);

        activeRoutine = null;
    }

    IEnumerator FadeColors(Color mapFrom, Color mapTo, Color bgFrom, Color bgTo, float duration)
    {
        if (duration <= 0f)
        {
            SetMapColor(mapTo);
            SetBigBackgroundColor(bgTo);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            SetMapColor(Color.Lerp(mapFrom, mapTo, t));
            SetBigBackgroundColor(Color.Lerp(bgFrom, bgTo, t));
            yield return null;
        }

        SetMapColor(mapTo);
        SetBigBackgroundColor(bgTo);
    }

    IEnumerator DashTintThenRevert()
    {
        bossSpriteRenderer.color = dashTintColor;

        yield return new WaitForSeconds(dashTintHoldDuration);

        float elapsed = 0f;
        while (elapsed < dashTintFadeDuration)
        {
            elapsed += Time.deltaTime;
            bossSpriteRenderer.color = Color.Lerp(dashTintColor, bossNormalColor, elapsed / dashTintFadeDuration);
            yield return null;
        }

        bossSpriteRenderer.color = bossNormalColor;
        dashTintRoutine = null;
    }

    // --- Helpers ---

    void SetMapLit(bool lit)
    {
        if (litMap != null) litMap.SetActive(lit);
        if (nonLitMap != null) nonLitMap.SetActive(!lit);
    }

    void SetMapColor(Color color)
    {
        if (nonLitMapRenderer != null)
            nonLitMapRenderer.color = color;
    }

    void SetBigBackgroundColor(Color color)
    {
        if (bigBackgroundRenderer != null)
            bigBackgroundRenderer.color = color;
    }
}
