using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// SUBTITLE DISPLAY — fixed-in-view subtitles for the scene voice.
///
/// Reveals the transcript one line at a time, paced to match the voice clip's
/// duration. Every time a stimulus is looked at, the subtitle RESETS to the
/// first line — the textual echo of losing the thread.
///
/// SETUP:
///   1. World Space Canvas parented to Main Camera, centre-bottom of view.
///   2. TMP text element assigned to subtitleText.
///   3. Set totalDuration to your voice clip length (61s here).
///   4. Set startDelay to match the voice start.
///   5. StimulusManager calls OnLook() on each look.
/// </summary>
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
        if (subtitleText != null) subtitleText.text = "";
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

    IEnumerator RestartReveal() // restarting the subtitles every time they look at a new stimulus.
    {
        if (subtitleText != null) subtitleText.text = "";
        yield return ShowLines();
    }

    IEnumerator ShowLines()
    {
        float totalWeight = 0f;
        foreach (var line in lines) totalWeight += line.weight;
        if (totalWeight <= 0f) totalWeight = lines.Length;

        float timePerWeight = totalDuration / totalWeight;

        for (int i = 0; i < lines.Length; i++)
        {
            if (subtitleText != null) subtitleText.text = lines[i].text;
            yield return new WaitForSeconds(lines[i].weight * timePerWeight);
        }

        if (ResolutionManager.Instance != null)
        ResolutionManager.Instance.OnScriptCompleted();
    }
}