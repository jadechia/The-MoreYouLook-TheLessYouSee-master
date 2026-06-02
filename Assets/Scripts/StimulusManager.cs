using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// *MANAGING THE BEHAVIOUR, SPAWN SETTINGS AND LIMITS OF THE ENTIRE STIMULUS POOOL*
public class StimulusManager : MonoBehaviour
{
    public static StimulusManager Instance;

    [Header("Prefabs")]
    public GameObject stimulusPrefab;

    [Header("Spawn Settings")]
    public float initialDelay = 8f;
    
    [Header("Population")]
    public int minimumStimuli = 2;  
    public float topUpCheckInterval = 2f;
    private float topUpTimer = 0f;

    [Header("Overload Reset")]
    public int overloadThreshold = 20;
    public AudioClip wooshSound;
    public AudioClip hintVoice;  
    public AudioSource overloadAudioSource; 
    public float overloadFadeTime = 1.5f;
    private bool overloading = false;


    // State
    private List<Stimulus> activeStimuli = new List<Stimulus>();
    private int lookCount = 0;
    private bool experienceResolved = false;

 

    void Awake() => Instance = this;
    void Start() => StartCoroutine(InitialSpawn());

    IEnumerator InitialSpawn()
    {
        yield return new WaitForSeconds(initialDelay);
        SpawnStimulus(); 
    }

    void Update()
    {
        if (experienceResolved || overloading) return;
        CleanDeadStimuli();

        topUpTimer += Time.deltaTime; // making sure there is always one stimulus to be triggered or loop dies
        if (topUpTimer >= topUpCheckInterval) {
            topUpTimer = 0f;
            while (activeStimuli.Count < minimumStimuli)
                SpawnStimulus();
        }

        if (activeStimuli.Count >= overloadThreshold)
            StartCoroutine(Overload());
    }
    public void OnStimulusLookedAt(Stimulus s)
    {
        if (overloading || experienceResolved) return; // fixing a bug where my stimuli kept triggering during overload animation
        if (s == null || s.IsDead) return;
        lookCount++;

        if (ExperienceAudioManager.Instance != null)
            ExperienceAudioManager.Instance.RegisterLook();

        if (SubtitleDisplay.Instance != null)
            SubtitleDisplay.Instance.OnLook(); // restart subtitles!

        if (ResolutionManager.Instance != null)
        ResolutionManager.Instance.NotifyLook(); // tell the subtitle script its been looked at

        foreach (Stimulus other in activeStimuli)
        {
            if (other != null && other != s)
                other.QuieterScript(0.22f);
        }
            StartCoroutine(SpawnAfterDelay(0.2f)); // spawn a new distraction right as user looks at a past one 
    }

    IEnumerator SpawnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        SpawnStimulus();
    }

    void SpawnStimulus()
    {
        if (stimulusPrefab == null) return;

        GameObject go = Instantiate(stimulusPrefab);
        Stimulus s    = go.GetComponent<Stimulus>();

        if (s != null)
        {
            s.Initialize(lookCount);
            activeStimuli.Add(s);
        }
    }

    IEnumerator Overload()
    {
        overloading = true;

        if (SubtitleDisplay.Instance != null) // switching subtitles
        SubtitleDisplay.Instance
        .ShowOverloadWarning("Let's try this again\nFocus on the instructions.\n Don't get lost.");

        // play the reset woosh sound:
        if (overloadAudioSource != null && wooshSound != null)
            overloadAudioSource.PlayOneShot(wooshSound);

        // fade out all the stimuli present
        foreach (Stimulus s in activeStimuli)
            if (s != null) StartCoroutine(FadeOutStimulus(s, overloadFadeTime));

        yield return new WaitForSeconds(overloadFadeTime);

        // clear:
        foreach (Stimulus s in activeStimuli)
            if (s != null) Destroy(s.gameObject);
        activeStimuli.Clear();

        // play the voice prompt with the overload reset
        if (overloadAudioSource != null && hintVoice != null)
        {
            overloadAudioSource.PlayOneShot(hintVoice);
            yield return new WaitForSeconds(hintVoice.length + 0.5f);
        }

        // restarting the script & the experience
        lookCount = 0;

        if (SubtitleDisplay.Instance != null)
            SubtitleDisplay.Instance.RestartFromTop();

        if (ExperienceAudioManager.Instance != null)
            ExperienceAudioManager.Instance.RestartScript();

        overloading = false;
        SpawnStimulus(); // spawn first stimulus
    }

    void CleanDeadStimuli()
    {
        activeStimuli.RemoveAll(s => s == null || s.IsDead);
    }

    IEnumerator FadeOutStimulus(Stimulus s, float duration = 2.5f)
    {
        float elapsed  = 0f;
        Vector3 start  = s != null ? s.transform.localScale : Vector3.zero;

        while (elapsed < duration && s != null)
        {
            elapsed += Time.deltaTime;
            s.transform.localScale = Vector3.Lerp(start, Vector3.zero,
                elapsed / duration);
            yield return null;
        }

        if (s != null) Destroy(s.gameObject);
    }

    public void OnExperienceResolved()
    {
        experienceResolved = true;
        StopAllCoroutines();
        foreach (Stimulus s in activeStimuli)
            if (s != null)
                StartCoroutine(FadeOutStimulus(s, 2.5f));
    }


    public int LookCount => lookCount;
}