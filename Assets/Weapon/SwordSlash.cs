using UnityEngine;

public class SwordSlash : MonoBehaviour
{
    [SerializeField] private SpriteRenderer target;
    [SerializeField] private Sprite[] frames;
    [SerializeField] private float size = 1.8f;
    [SerializeField] private Vector2 offset = new Vector2(0.8f, 0f);
    [SerializeField] private Color tint = Color.white;
    [SerializeField] private int sortingOrder = 5;

    float duration;
    float timer;
    bool playing;

    void Awake()
    {
        if (target == null) target = GetComponent<SpriteRenderer>();
        if (target != null)
        {
            target.sortingOrder = sortingOrder;
            target.color = tint;
        }

        Stop();
    }

    public void Play(float swingDuration)
    {
        if (target == null || frames == null || frames.Length == 0) return;

        duration = Mathf.Max(0.01f, swingDuration);
        timer = 0f;
        playing = true;

        transform.localPosition = offset;
        target.enabled = true;

        Apply(0f);
    }

    public void Stop()
    {
        playing = false;
        if (target != null) target.enabled = false;
    }

    void Update()
    {
        if (!playing) return;

        timer += Time.deltaTime;

        float t = timer / duration;
        if (t >= 1f)
        {
            Stop();
            return;
        }

        Apply(t);
    }

    void Apply(float t)
    {
        int index = Mathf.Clamp(Mathf.FloorToInt(t * frames.Length), 0, frames.Length - 1);
        Sprite frame = frames[index];
        if (frame == null) return;

        target.sprite = frame;

        Vector2 bounds = frame.bounds.size;
        float largest = Mathf.Max(bounds.x, bounds.y);
        if (largest > 0.0001f)
            transform.localScale = Vector3.one * (size / largest);
    }
}
