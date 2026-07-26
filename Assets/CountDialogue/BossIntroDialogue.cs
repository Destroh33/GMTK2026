using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class BossIntroDialogue : MonoBehaviour
{
    [System.Serializable]
    public class Line
    {
        [TextArea(2, 4)] public string text;
        public float minReadTime = 0.4f;
    }

    [SerializeField] private TypewriterText typewriter;
    [SerializeField] private GameObject dialogueRoot;
    [SerializeField] private GameObject dialoguePrefab;
    [SerializeField] private List<Line> lines = new List<Line>();

    [SerializeField] private bool allowSkipInput = true;
    [SerializeField] private float skipLockout = 0.2f;

    float scaleBeforeHold = 1f;

    void Start()
    {
        ResolveDialogue();
        StartDialogue();
    }

    public void StartDialogue()
    {
        StartCoroutine(Run());
    }

    IEnumerator Run()
    {
        TakeHold();

        foreach (Line line in lines)
        {
            if (line == null || string.IsNullOrWhiteSpace(line.text)) continue;
            yield return Speak(line);
        }

        ReleaseHold();
        dialogueRoot?.SetActive(false);
    }

    IEnumerator Speak(Line line)
    {
        dialogueRoot?.SetActive(true);

        if (typewriter != null)
        {
            typewriter.Play(line.text);

            float lockout = skipLockout;
            while (typewriter.IsTyping)
            {
                lockout -= Time.unscaledDeltaTime;
                if (lockout <= 0f && SkipPressed())
                {
                    typewriter.Skip();
                    break;
                }
                yield return null;
            }
        }

        float minRead = line.minReadTime;
        while (minRead > 0f)
        {
            minRead -= Time.unscaledDeltaTime;
            yield return null;
        }

        while (!SkipPressed()) yield return null;
    }

    void ResolveDialogue()
    {
        if (typewriter == null)
            typewriter = FindAnyObjectByType<TypewriterText>(FindObjectsInactive.Include);

        if (typewriter == null && dialoguePrefab != null)
        {
            Canvas canvas = FindAnyObjectByType<Canvas>();
            GameObject instance = canvas != null
                ? Instantiate(dialoguePrefab, canvas.transform, false)
                : Instantiate(dialoguePrefab);

            instance.name = dialoguePrefab.name;
            dialogueRoot = instance;
            typewriter = instance.GetComponentInChildren<TypewriterText>(true);
        }

        if (dialogueRoot == null && typewriter != null)
            dialogueRoot = typewriter.gameObject;
    }

    void TakeHold()
    {
        scaleBeforeHold = Time.timeScale > 0f ? Time.timeScale : 1f;
        Time.timeScale = 0f;
    }

    void ReleaseHold()
    {
        Time.timeScale = scaleBeforeHold;
    }

    bool SkipPressed()
    {
        if (!allowSkipInput) return false;
        if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) return true;
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) return true;
        return false;
    }
}