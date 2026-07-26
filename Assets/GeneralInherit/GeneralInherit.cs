using UnityEngine;
using System.Collections;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class PlayerBehaviors : VignetteController {}
public class EnemyBehaviors : Flash {}

public class Flash : MonoBehaviour
{
    [Header("Flash Details")]
    [SerializeField] protected SpriteRenderer spriteRenderer;
    [SerializeField] protected Material flashMaterial;
    [SerializeField] protected float flashDuration = 0.05f;

    [Header("Tint Details")]
    [SerializeField] protected Color tintColor = Color.red;

    [SerializeField] protected float tintDuration = 0.05f;
    private Coroutine flashCoroutine { get; set; }
    private Coroutine tintCoroutine { get; set; }

    protected Color baseColor { get; set; }
    protected Material originalMaterial;
    protected virtual void SetFlashInfo()
    {
        if(spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            baseColor = spriteRenderer.color;
            originalMaterial = spriteRenderer.material;
        }
        if(flashMaterial == null)
        {
            flashMaterial = Resources.Load<Material>("Materials/WhiteFlash");
        }
    }
    public void FlashEntity()
    {
        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
        }
        flashCoroutine = StartCoroutine(FlashingEntity());
    }
    private IEnumerator FlashingEntity()
    {
        spriteRenderer.material = flashMaterial;
        yield return new WaitForSeconds(flashDuration);
        spriteRenderer.material = originalMaterial;
        flashCoroutine = null;
    }

    public void TintEntity()
    {
        if (tintCoroutine != null)
        {
            StopCoroutine(tintCoroutine);
        }
        tintCoroutine = StartCoroutine(TintingEntity());
    }
    private IEnumerator TintingEntity()
    {
        spriteRenderer.color = tintColor;
        yield return new WaitForSeconds(tintDuration);
        spriteRenderer.color = baseColor;
        tintCoroutine = null;
    }
}

public class VignetteController : Flash
{
    [Header("Vignette Controls")]
    [SerializeField] private Volume volume;
    [SerializeField] private AnimationCurve vignetteIntensityCurve =
        /*new AnimationCurve(
            new Keyframe(0.00f, 1.25f), 
            new Keyframe(0.40f, 1.00f),
            new Keyframe(0.50f, 1.10f),
            new Keyframe(0.65f, 0.75f), 
            new Keyframe(0.85f, 0.55f),
            new Keyframe(0.90f, 0.35f),
            new Keyframe(0.97f, 0.15f),
            new Keyframe(1.00f, 0.00f)
        );*/
        new AnimationCurve(
            new Keyframe(0.00f, 1.00f), 
            new Keyframe(0.40f, 0.75f),
            new Keyframe(0.50f, 0.85f),
            new Keyframe(0.65f, 0.50f), 
            new Keyframe(0.85f, 0.35f),
            new Keyframe(0.90f, 0.15f),
            new Keyframe(0.97f, 0.07f),
            new Keyframe(1.00f, 0.00f)
        );
        /*new AnimationCurve(
            new Keyframe(0.00f, 1.00f), 
            new Keyframe(0.15f, 0.80f),
            new Keyframe(0.30f, 0.90f),
            new Keyframe(0.45f, 0.60f), 
            new Keyframe(0.60f, 0.75f),
            new Keyframe(0.75f, 0.40f),
            new Keyframe(0.90f, 0.15f),
            new Keyframe(1.00f, 0.00f)
        );*/
    private Vignette vignette;
    private Coroutine vignetteCoroutine { get; set; }
    private float originalVignetteIntensity { get; set; }
    private Color originalVignetteColor { get; set; }
    protected virtual void SetVignetteInfo()
    {
        if (volume == null)
        {
            volume = GameObject.Find("Global Volume").GetComponent<Volume>();
        }
        volume?.profile?.TryGet(out vignette);
        if (vignette != null)
        {
            originalVignetteIntensity = vignette.intensity.value;
            originalVignetteColor = vignette.color.value;
        }
    }

    public void DoVignette(float intensity = 0.35f, float duration = 1.5f, Color? color = null)
    {
        if (color == null)
        {
            color = new Color32(255, 65, 65, 255);
        }
        if (vignetteCoroutine != null)
        {
            StopCoroutine(vignetteCoroutine);
        }
        vignetteCoroutine = StartCoroutine(PlayVignette(intensity, duration, (Color)color));
    }

    private IEnumerator PlayVignette(float intensity, float duration, Color color)
    {
        if (vignette != null)
        {
            float elapsed = 0f;
            vignette.color.value = color;
            while (elapsed < duration)
            {
                if(SettingsButton.Instance == null || !SettingsButton.Instance.gamePaused)
                {
                    elapsed += Time.unscaledDeltaTime;
                }
                float t = 1f - Mathf.Clamp01(elapsed / duration);
                vignette.intensity.value = intensity * vignetteIntensityCurve.Evaluate(t);
                yield return null;
            }
            vignette.intensity.value = originalVignetteIntensity;
            vignette.color.value = originalVignetteColor;
        }
        vignetteCoroutine = null;
    }
}
