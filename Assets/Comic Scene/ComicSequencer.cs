using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

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

    private int revealedCount;

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
        if (revealedCount < panels.Length)
        {
            if (revealedCount == halfwayIndex)
                ClearRevealed();

            if (panels[revealedCount] != null)
                panels[revealedCount].SetActive(true);

            revealedCount++;
            return;
        }

        // Every panel is already revealed - this click moves on.
        if (!string.IsNullOrEmpty(nextSceneName))
            SceneManager.LoadScene(nextSceneName);
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
