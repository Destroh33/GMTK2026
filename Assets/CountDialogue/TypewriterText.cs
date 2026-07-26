using System.Collections;
using UnityEngine;
using TMPro;

public class TypewriterText : MonoBehaviour
{
    [SerializeField] private TMP_Text tmpLabel;
    [SerializeField] private float charactersPerSecond = 30f;
    [SerializeField] private float fadeDelay = 10f;
    [SerializeField] private AudioClip typeSound;

    public bool IsTyping { get; private set; }

    Coroutine typingRoutine;
    Coroutine fadeRoutine;
    string fullText = "";

    public void Play(string text)
    {
        fullText = text;

        if (typingRoutine != null)
        {
            StopCoroutine(typingRoutine);
        }

        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }

        typingRoutine = StartCoroutine(TypeRoutine(text, charactersPerSecond));

        AudioManager.Instance?.StartGibberish();
    }

    public void Skip()
    {
        if (typingRoutine != null)
        {
            StopCoroutine(typingRoutine);
            typingRoutine = null;
        }

        tmpLabel.text = fullText;

        IsTyping = false;

        AudioManager.Instance?.StopGibberish();

        QueueFade();
    }

    void QueueFade()
    {
        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
        }

        fadeRoutine = StartCoroutine(FadeRoutine());
    }

    IEnumerator FadeRoutine()
    {
        yield return new WaitForSecondsRealtime(fadeDelay);

        tmpLabel.text = "";
        fadeRoutine = null;
    }

    IEnumerator TypeRoutine(string text, float speed)
    {
        IsTyping = true;

        tmpLabel.text = "";

        float delay = 1f / speed;
        var builder = new System.Text.StringBuilder();

        for (int i = 0; i < text.Length; i++)
        {
            builder.Append(text[i]);

            tmpLabel.text = builder.ToString();

            if (typeSound != null)
            {
                //TODO add audiomanager integration (figuer out if it hsould be signleton or smth else)
            }

            yield return new WaitForSecondsRealtime(delay);
        }

        IsTyping = false;
        typingRoutine = null;

        AudioManager.Instance?.StopGibberish();

        QueueFade();
    }
}
