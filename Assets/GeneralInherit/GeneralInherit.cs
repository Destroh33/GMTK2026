using UnityEngine;
using System.Collections;

public class PlayerBehaviors : Flash {}
public class EnemyBehaviors : Flash {}

public class Flash : MonoBehaviour
{
    [Header("Flash Details")]
    [SerializeField] protected SpriteRenderer spriteRenderer;
    [SerializeField] protected Color flashColor = Color.red;
    [SerializeField] protected float flashDuration = 0.05f;
    private Coroutine flashCoroutine { get; set; }
    protected Color baseColor { get; set; }
    protected virtual void SetFlashInfo()
    {
        if(spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            baseColor = spriteRenderer.color;
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
        spriteRenderer.color = flashColor;
        yield return new WaitForSeconds(flashDuration);
        spriteRenderer.color = baseColor;
        flashCoroutine = null;
    }
}
