using System.Collections.Generic;
using UnityEngine;

// Spawns tiled copies of a backdrop sprite stacked edge-to-edge, scrolls them
// downward, and recycles each tile to the top once it fully exits the bottom.
// All copies are forced onto the same Sorting Layer + Order in Layer so there's
// nothing to fight over - flicker only happens if tiles overlap, so this keeps
// them edge-to-edge instead of relying on sort order to fix an overlap.
public class ScrollingBackdrop : MonoBehaviour
{
    [SerializeField] private SpriteRenderer backdropPrefab;
    [SerializeField] private int tileCount = 3;
    [SerializeField] private float scrollSpeed = 2f;

    [Header("Enforced sorting (applied to every instance)")]
    [SerializeField] private string sortingLayerName = "Default";
    [SerializeField] private int sortingOrder = 0;

    private readonly List<Transform> tiles = new List<Transform>();
    private float tileHeight;

    void Start()
    {
        tileHeight = backdropPrefab.bounds.size.y;

        for (int i = 0; i < tileCount; i++)
        {
            SpriteRenderer instance = Instantiate(backdropPrefab, transform);
            instance.transform.localPosition = new Vector3(0f, i * tileHeight, 0f);

            // Stamp the same sorting values on every tile - don't rely on the
            // prefab's inspector values alone, since they can drift.
            instance.sortingLayerName = sortingLayerName;
            instance.sortingOrder = sortingOrder;

            tiles.Add(instance.transform);
        }
    }

    void Update()
    {
        float delta = scrollSpeed * Time.deltaTime;
        float wrapHeight = tileHeight * tileCount;

        foreach (Transform tile in tiles)
        {
            tile.position += Vector3.down * delta;

            // Once a tile has fully scrolled past the bottom, snap it back
            // above the top of the stack - edge-to-edge, no overlap.
            if (tile.position.y <= transform.position.y - tileHeight)
            {
                tile.position += Vector3.up * wrapHeight;
            }
        }
    }
}
