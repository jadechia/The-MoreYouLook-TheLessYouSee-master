using System.Collections;
using UnityEngine;

// *SCRIPT TO HANDLE THE ENDING OF THE EXPERIENCE*
public class ResolutionManager : MonoBehaviour
{
    public static ResolutionManager Instance;

    [Header("Post-script stillness period needed")]
    public float calmSecondsToResolve = 3f;

    [Header("Outro Audio Fade")]
    public float audioFadeTime = 8f;

    [Header("Resolution Message")]
    public string closingLine = "Successful. \n Thank you for your attention";
    public string resetLine = "You may remove the headset to see your attention map";
    public float messageDelay = 2f;

    [Header("Nucleus Retreat")]
    public Transform nucleus; // drag the nucleus in
    public float nucleusRetreatZ = 11f; // how far it drifts back
    public float nucleusDriftTime = 2f;


    IEnumerator ResolutionSequence()
    {

        if (StimulusManager.Instance != null)
            StimulusManager.Instance.OnExperienceResolved(); // end experience

        if (ExperienceAudioManager.Instance != null)
            ExperienceAudioManager.Instance.ResolveAudio(audioFadeTime); // fading out all sound

        if (nucleus != null)
            StartCoroutine(DriftNucleus()); // nucleus moves into horizon

        yield return new WaitForSeconds(messageDelay);

        if (SubtitleDisplay.Instance != null)
            SubtitleDisplay.Instance.ShowClosingMessage(closingLine, resetLine); // showing the final message

        Debug.Log("you did it. thank you for your attention.");
    }

    IEnumerator DriftNucleus()
    {
        // var follow = nucleus.GetComponent<NucleusSetup>(); 
        // if (follow != null) follow.enabled = false;

        Vector3 p = nucleus.position;
        p.z = 11f;
        nucleus.position = p;
        yield break;
    }

    private bool scriptCompleted = false;
    private bool resolved = false;
    private float calmTimer = 0f;
    private bool counting = false;
    public bool ScriptCompleted => scriptCompleted;

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
        StartCoroutine(ResolutionSequence());
    }

    public bool IsResolved => resolved;
}