using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Attach to a parent whose children are the comic panels, in the order they
// should appear. Each click/key press reveals the next panel; once every
// panel is visible, one more click/press loads nextSceneName.
public class ComicSequencer : MonoBehaviour
{
    [Tooltip("Leave empty to auto-populate from direct children, in hierarchy order, on Awake.")]
    [SerializeField] private GameObject[] panels;

    [Tooltip("Scene to load once every panel has been revealed and the player advances one more time. Must be added to Build Settings.")]
    [SerializeField] private string nextSceneName = "SampleScene";

    [Tooltip("Child index where the page clears: right before this panel is revealed, every panel shown so far gets hidden again, and this panel becomes the fresh start of the next page. Set to -1 to disable.")]
    [SerializeField] private int halfwayIndex = -1;

    [Header("Page Transition (plays at Halfway Index)")]
    [Tooltip("Renderer whose color changes during the page transition.")]
    [SerializeField] private SpriteRenderer backgroundRenderer;
    [SerializeField] private Color transitionColor = Color.black;
    [Tooltip("How long to wait, after the old panels are cleared, before the background starts changing color.")]
    [SerializeField] private float pauseBeforeTransition = 0.5f;
    [SerializeField] private float colorTransitionDuration = 0.5f;

    [Header("Panel Reveal")]
    [Tooltip("How long each panel takes to fade from 0 to full opacity when revealed.")]
    [SerializeField] private float panelFadeDuration = 0.5f;

    [Header("Scene Transition")]
    [Tooltip("Full-screen overlay (a SpriteRenderer) faded to opaque before nextSceneName loads.")]
    [SerializeField] private SpriteRenderer fadeToBlackOverlay;
    [SerializeField] private float fadeToBlackDuration = 0.5f;

    private int revealedCount;
    private bool transitioning;

    void Awake()
    {
        if (panels == null || panels.Length == 0)
        {
            panels = new GameObject[transform.childCount];
            for (int i = 0; i < transform.childCount; i++)
                panels[i] = transform.GetChild(i).gameObject;
        }

        foreach (GameObject panel in panels)
        {
            if (panel != null)
                panel.SetActive(false);
        }
    }

    void Update()
    {
        bool advancePressed =
            (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) ||
            (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame);

        if (advancePressed)
            Advance();
    }

    void Advance()
    {
        if (transitioning) return;

        if (revealedCount < panels.Length)
        {
            if (revealedCount == halfwayIndex)
            {
                StartCoroutine(PageTransition());
                return;
            }

            StartCoroutine(RevealPanel(panels[revealedCount]));
            revealedCount++;
            return;
        }

        // Every panel is already revealed - this click moves on.
        if (!string.IsNullOrEmpty(nextSceneName))
            StartCoroutine(FadeToBlackThenLoadScene());
    }

    IEnumerator PageTransition()
    {
        transitioning = true;

        ClearRevealed();

        yield return new WaitForSeconds(pauseBeforeTransition);

        yield return TransitionBackgroundColor();

        yield return RevealPanel(panels[revealedCount]);

        revealedCount++;
        transitioning = false;
    }

    IEnumerator RevealPanel(GameObject panel)
    {
        if (panel == null) yield break;

        SetPanelAlpha(panel, 0f);
        panel.SetActive(true);

        float elapsed = 0f;
        while (elapsed < panelFadeDuration)
        {
            elapsed += Time.deltaTime;
            SetPanelAlpha(panel, Mathf.Clamp01(elapsed / panelFadeDuration));
            yield return null;
        }

        SetPanelAlpha(panel, 1f);
    }

    void SetPanelAlpha(GameObject panel, float alpha)
    {
        if (panel == null) return;

        if (panel.TryGetComponent(out CanvasGroup canvasGroup))
        {
            canvasGroup.alpha = alpha;
            return;
        }

        if (panel.TryGetComponent(out SpriteRenderer spriteRenderer))
        {
            Color c = spriteRenderer.color;
            c.a = alpha;
            spriteRenderer.color = c;
            return;
        }

        if (panel.TryGetComponent(out Image image))
        {
            Color c = image.color;
            c.a = alpha;
            image.color = c;
        }
    }

    IEnumerator FadeToBlackThenLoadScene()
    {
        transitioning = true;

        yield return FadeOverlayAlpha(0f, 1f, fadeToBlackDuration);

        SceneManager.LoadScene(nextSceneName);
    }

    IEnumerator FadeOverlayAlpha(float fromAlpha, float toAlpha, float duration)
    {
        if (fadeToBlackOverlay == null) yield break;

        Color c = fadeToBlackOverlay.color;
        c.a = fromAlpha;
        fadeToBlackOverlay.color = c;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Lerp(fromAlpha, toAlpha, elapsed / duration);
            fadeToBlackOverlay.color = c;
            yield return null;
        }

        c.a = toAlpha;
        fadeToBlackOverlay.color = c;
    }

    IEnumerator TransitionBackgroundColor()
    {
        if (backgroundRenderer == null) yield break;

        Color from = backgroundRenderer.color;
        float elapsed = 0f;

        while (elapsed < colorTransitionDuration)
        {
            elapsed += Time.deltaTime;
            backgroundRenderer.color = Color.Lerp(from, transitionColor, elapsed / colorTransitionDuration);
            yield return null;
        }

        backgroundRenderer.color = transitionColor;
    }

    void ClearRevealed()
    {
        for (int i = 0; i < revealedCount; i++)
        {
            if (panels[i] != null)
                panels[i].SetActive(false);
        }
    }
}
