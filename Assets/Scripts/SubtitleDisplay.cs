using System.Collections;
using UnityEngine;
using TMPro;

// SUBTITLES FOR ONSCREEN TEXT! ALSO SWITCHING BETWEEN SCRIPT, OVERLOAD & OUTRO TEXT*
public class SubtitleDisplay : MonoBehaviour
{
    public static SubtitleDisplay Instance;

    [Header("Text Target")]
    public TMP_Text subtitleText;

    [Header("Timing")]
    public float totalDuration = 61f;
    public float startDelay = 6f;

    [Header("Behaviour")]
    public bool restartOnLook = true;

    [Header("Overload Warning")]
    public Color warningColor = Color.red;

    [Header("Closing Sequence")]
    public CanvasGroup darkOverlay;    
    public TMP_Text resetText;
    public float overlayFadeTime = 4.5f;

    private readonly (string text, float weight)[] lines = new (string, float)[]
    { // i split each line and used weighted lengths to match my cadence
        ("Listen carefully.", 1.1f),
        ("I will explain how to end the experience.", 1f),
        ("But, in order to understand what is happening", 1.2f),
        ("and why you're here,", 1f),
        ("you need to ask yourself one question first.", 1.4f),
        ("What do you notice?", 1f),
        ("Everything around you", 1f),
        ("the lights, the sounds, all of it,", 1f),
        ("they only exist because you are telling them to.", 1.5f),
        ("What are you in control of right now?", 1.3f),
        ("There is no correct answer,", 1f),
        ("but the answer is simple.", 1f),
        ("How aware are you of what you're doing?", 1f),
        ("Are you aware of what you're experiencing?", 1f),
        ("Because it will all stop", 1f),
        ("the moment you choose to stop too.", 1.2f),
        ("What is it that you aren't doing?", 1f),
        ("As long as you stay here,", 1f),
        ("you will be able to stop this.", 1f),
        ("Stay here.", 1.1f),
        ("THE MORE YOU LOOK", 1f),
        ("the less you see.", 1.2f),
        ("Do you see it now?", 2.5f)
    };
    private Coroutine revealRoutine;

    void Awake()
    {
        Instance = this;
        if (subtitleText != null) 
            subtitleText.text = "";
    }
    void Start()
    {
        revealRoutine = StartCoroutine(Reveal());
    }

    IEnumerator Reveal()
    {
        if (subtitleText != null) subtitleText.text = "";
        yield return new WaitForSeconds(startDelay);
        yield return ShowLines();
    }

    public void OnLook()
    {
        if (!restartOnLook) return;

        if (revealRoutine != null) StopCoroutine(revealRoutine);
        revealRoutine = StartCoroutine(RestartReveal());
    }

    public void ShowOverloadWarning(string message) // switching to my overload text
    {
        if (revealRoutine != null) StopCoroutine(revealRoutine);  // stop the old script reveal
        if (subtitleText != null)
        {
            subtitleText.color = warningColor;
            subtitleText.text = message;
        }
    }

    public void ClearOverloadWarning() // function so the intructions show again after the warning's finished
    {
        if (subtitleText != null)
        {
            subtitleText.color = Color.white;
            subtitleText.text = "";
        }
    }

    IEnumerator RestartReveal() // restarting the subtitles every time they look at a new stimulus.
    {
        if (subtitleText != null) subtitleText.text = "";
        yield return ShowLines();
    }

    public void RestartFromTop()
    {
        if (revealRoutine != null) StopCoroutine(revealRoutine); 
        if (subtitleText != null) subtitleText.text = "";
        revealRoutine = StartCoroutine(ShowLines());
    }

    IEnumerator ShowLines()
    {
        float totalWeight = 0f;
        foreach (var line in lines) totalWeight += line.weight;
        if (totalWeight <= 0f) totalWeight = lines.Length;

        float timePerWeight = totalDuration / totalWeight;

        for (int i = 0; i < lines.Length; i++)
        {
            if (subtitleText != null)       {
            subtitleText.color = Color.white; 
            subtitleText.text = lines[i].text;
        }
            yield return new WaitForSeconds(lines[i].weight * timePerWeight);
        }

        if (ResolutionManager.Instance != null)
        ResolutionManager.Instance.OnScriptCompleted();
    }

        public void ShowClosingMessage(string mainLine, string resetMessage)
    {
        if (revealRoutine != null) StopCoroutine(revealRoutine);
        StartCoroutine(ClosingSequence(mainLine, resetMessage));
    }
    IEnumerator ClosingSequence(string mainLine, string resetMessage)
    {
        if (darkOverlay != null) {
            float e = 0f;
            while (e < overlayFadeTime)
            {
                e += Time.deltaTime;
                darkOverlay.alpha = Mathf.Clamp01(e / overlayFadeTime);
                yield return null;
            }
            darkOverlay.alpha = 1f;
        }


        if (subtitleText != null)
        {
            subtitleText.color = Color.white;
            subtitleText.text = mainLine; // change text to the main exit line.
        }

        yield return new WaitForSeconds(2.4f); // then after a couple seconds the small reset prompt shows up too

        if (resetText != null)
        {
            resetText.color = new Color(1f, 1f, 1f, 0.75f); 
            resetText.text = resetMessage;
        }
}
}