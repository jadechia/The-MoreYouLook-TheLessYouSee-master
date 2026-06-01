using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StimulusManager : MonoBehaviour
{
    public static StimulusManager Instance;

    [Header("Prefabs")]
    public GameObject stimulusPrefab;

    [Header("Spawn Settings")]
    public float initialDelay = 0.9f;

    [Header("Escalation")]
    public int maxSimultaneousStimuli = 50;
        [Header("Population")]
    public int minimumStimuli = 2;  
    public float topUpCheckInterval = 2f;
    private float topUpTimer = 0f;

    // State
    private List<Stimulus> activeStimuli = new List<Stimulus>();
    private int lookCount = 0;
    private bool experienceResolved = false;

    int TargetStimuliCount
    {
        get
        {
            if (lookCount < 3)  return 1;
            if (lookCount < 7)  return 2;
            if (lookCount < 13) return 3;
            if (lookCount < 20) return 4;
            return Mathf.Min(maxSimultaneousStimuli,
                5 + (lookCount - 20) / 4);
        }
    }


    void Awake() => Instance = this;

    void Start() => StartCoroutine(InitialSpawn());

    IEnumerator InitialSpawn()
    {
        yield return new WaitForSeconds(initialDelay);
        SpawnStimulus(); 
    }

    void Update()
    {
        if (experienceResolved) return;
        CleanDeadStimuli();

        topUpTimer += Time.deltaTime; // making sure there is always one stimulus to be triggered or loop dies
        if (topUpTimer >= topUpCheckInterval) {
            topUpTimer = 0f;
            while (activeStimuli.Count < minimumStimuli)
                SpawnStimulus();
        }
    }
    public void OnStimulusLookedAt(Stimulus s)
    {
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
        // if (activeStimuli.Count < maxSimultaneousStimuli)
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

    void CleanDeadStimuli()
    {
        activeStimuli.RemoveAll(s => s == null || s.IsDead);
    }

    public void OnExperienceResolved()
    {
        experienceResolved = true;
        StopAllCoroutines();
        foreach (Stimulus s in activeStimuli)
            if (s != null)
                StartCoroutine(FadeOutStimulus(s));
    }

    IEnumerator FadeOutStimulus(Stimulus s)
    {
        float duration = 5f;
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

    public int LookCount => lookCount;
}