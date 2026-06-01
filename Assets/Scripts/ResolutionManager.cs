using System.Collections;
using UnityEngine;
public class ResolutionManager : MonoBehaviour
{
    public static ResolutionManager Instance;

    [Header("Post-script stillness period needed")]
    public float calmSecondsToResolve = 3f;

    [Header("Outro Audio Fade")]
    public float audioFadeTime = 8f;

    [Header("Attention Map Memento")]
    // public MapCapture mapCapture;     // screencap to export later

    private bool scriptCompleted = false;
    private bool resolved = false;
    private float calmTimer = 0f;
    private bool counting = false;

    void Awake() => Instance = this;

    void Update()
    {
        if (resolved) return;
// counting the secs the user spends focused after script has completed uninterrupted
        if (scriptCompleted && counting)
        {
            calmTimer += Time.deltaTime;
            if (calmTimer >= calmSecondsToResolve)
                Resolve();
        }
    }

 
    public void OnScriptCompleted()
    {
        if (resolved) return;
        scriptCompleted = true;
        counting = true;
        calmTimer = 0f;
    }


  // after resolution the user has to stay undistracted for a few seconds to end the experience
    public void NotifyLook()
    {
        if (resolved) return;
        calmTimer = 0f;  
    }

    void Resolve()
    {
        if (resolved) return;
        resolved = true;

        // Stop spawning stimuli and fade the existing 1s
        if (StimulusManager.Instance != null)
            StimulusManager.Instance.OnExperienceResolved();

        if (ExperienceAudioManager.Instance != null)
            ExperienceAudioManager.Instance.ResolveAudio(audioFadeTime);

        // if (mapCapture != null)
        //     mapCapture.Capture(); // photograph the attention map as a memento for the user

        Debug.Log("Thank you for your attention.");
    }

    public bool IsResolved => resolved;
}