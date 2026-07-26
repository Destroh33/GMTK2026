using UnityEngine;

// Picks one sprite at random when the object spawns, so a stream of otherwise
// identical projectiles reads as a spray of chunks rather than one repeated icon.
[RequireComponent(typeof(SpriteRenderer))]
public class RandomSpriteVariant : MonoBehaviour
{
    [SerializeField] private SpriteRenderer target;
    [SerializeField] private Sprite[] variants;

    [Header("Random Rotation")]
    [SerializeField] private bool randomizeRotation = true;

    void Awake()
    {
        if (target == null)
            target = GetComponent<SpriteRenderer>();

        if (variants != null && variants.Length > 0)
        {
            Sprite pick = variants[Random.Range(0, variants.Length)];
            if (pick != null) target.sprite = pick;
        }

        if (randomizeRotation)
            target.transform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
    }
}
