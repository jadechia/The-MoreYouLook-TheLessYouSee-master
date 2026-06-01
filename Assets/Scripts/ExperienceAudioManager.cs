using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

public class ExperienceAudioManager : MonoBehaviour
{
    public static ExperienceAudioManager Instance;
    
    [Header("Intro Stereo")]
    public AudioClip introLeft;        // "the more you look"
    public AudioClip introRight;       // "the less you see"
    public float introDelay = 1f;    // seconds after scene load before intro plays
    public float introPause = 0.2f;    // gap between left and right

    private AudioSource introSourceL;
    private AudioSource introSourceR;

    [Header("Voice Track")]
    public AudioClip voiceClip;
    public AudioMixer audioMixer;
    public string voiceVolumeParam  = "VoiceVolume";
    public string voiceLowPassParam = "VoiceLowPass";

    [Header("Ambience")]
    public AudioClip ambienceClip;
    public float ambienceVolume = 0.12f;

    [Header("Timing")]
    public float silenceBeforeVoice = 1f;

    [Header("Degradation")]
    public float volumeDropPerLook  = 0.035f;
    public float lowPassDropPerLook = 700f;
    public float minVolume          = 0.02f;
    public float maxLowPassCutoff   = 22000f;
    public float minLowPassCutoff   = 350f;

    // State
    private AudioSource voiceSource;
    private AudioSource ambienceSource;
    private float currentVolume;
    private float currentLowPass;

    void Awake()
    {
        Instance = this;

        introSourceL = gameObject.AddComponent<AudioSource>();
        introSourceR = gameObject.AddComponent<AudioSource>();

        introSourceL.spatialBlend = 0f;
        introSourceR.spatialBlend = 0f;
        introSourceL.panStereo = -1f;   // left ear
        introSourceR.panStereo =  1f;   // right
        introSourceL.loop = false;
        introSourceR.loop = false;
        introSourceL.playOnAwake = false;
        introSourceR.playOnAwake = false;

        voiceSource    = gameObject.AddComponent<AudioSource>();
        ambienceSource = gameObject.AddComponent<AudioSource>();

        voiceSource.spatialBlend    = 0f;
        voiceSource.loop            = false;
        voiceSource.playOnAwake     = false;

        ambienceSource.spatialBlend = 0f;
        ambienceSource.loop         = true;
        ambienceSource.playOnAwake  = false;
        ambienceSource.volume       = ambienceVolume;

        currentVolume   = 0.75f;
        currentLowPass  = maxLowPassCutoff;
    }

    void Start() { StartCoroutine(IntroSequence());}

    IEnumerator IntroSequence()
    {
        
        if (ambienceClip != null) // bg music
        {
            ambienceSource.clip = ambienceClip;
            ambienceSource.Play();
        }

        yield return new WaitForSeconds(introDelay);

        // left ear
        if (introLeft != null)
        {
            introSourceL.clip = introLeft;
            introSourceL.Play();
            yield return new WaitForSeconds(introLeft.length + introPause);
        }

        // right ear
        if (introRight != null)
        {
            introSourceR.clip = introRight;
            introSourceR.Play();
            yield return new WaitForSeconds(introRight.length);
        }

        // pause before script begins
        yield return new WaitForSeconds(silenceBeforeVoice);

        if (voiceClip != null)
        {
            voiceSource.clip = voiceClip;
            voiceSource.volume = currentVolume;
            voiceSource.Play();
        }

        ApplyMixerSettings();
        StartCoroutine(MonitorVoiceCompletion());
    }

    IEnumerator MonitorVoiceCompletion()
    {
        // waitin until the clip has finished playing
        while (voiceSource.isPlaying)
            yield return new WaitForSeconds(0.5f);
        OnVoiceCompleted();
    }

    public void RegisterLook()
    {
        currentVolume  = Mathf.Max(minVolume, currentVolume *0.28f);
        voiceSource.volume = currentVolume; 
        ApplyMixerSettings();
    }

    void ApplyMixerSettings()
    {
        // if (audioMixer == null) return;
        // audioMixer.SetFloat(voiceVolumeParam,  db);
        // audioMixer.SetFloat(voiceLowPassParam, currentLowPass);
    }

    void OnVoiceCompleted()
    {
        StimulusManager.Instance.OnExperienceResolved();
        StartCoroutine(FadeAmbience());
    }

    public void ResolveAudio(float fadeTime)
    {
        StopAllCoroutines(); 
        StartCoroutine(SettleEverything(fadeTime));
    }

    IEnumerator SettleEverything(float fadeTime)
    {
        float elapsed = 0f;
        float startVoice = voiceSource.volume;
        float startAmb = ambienceSource.volume;

        while (elapsed < fadeTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeTime;
            voiceSource.volume = Mathf.Lerp(startVoice, 1f, t); // making the voice loud and clear
            ambienceSource.volume = Mathf.Lerp(startAmb, 0f, t); // stops bg music
            yield return null;
        }

        voiceSource.volume = 1f;
        ambienceSource.volume = 0f;
    }

    IEnumerator FadeAmbience()
    {
        float duration = 8f;
        float elapsed  = 0f;
        float startVol = ambienceSource.volume;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            ambienceSource.volume = Mathf.Lerp(startVol, 0f, elapsed / duration);
            yield return null;
        }
    }
}