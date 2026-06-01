using System.Collections;
using UnityEngine;

public class Stimulus : MonoBehaviour
{
    // sounds:
    [Header("Audio — Calling Sounds (random selection)")]
    public AudioClip[] callingSounds;       // assign 4-5 clips

    [Header("Audio — Voice Clips (random selection)")]
    public AudioClip[] voiceClips;          // assign multiple recordings
    public float pitchMin = 0.92f;          // randomised pitch range
    public float pitchMax = 1.08f;

    [Header("Audio — Caught Sound")]
    public AudioClip caughtSound;
    public float caughtVolume = 1f;

    [Header("Caught Visual Animation")]
    public ParticleSystem caughtBurst;

    [Header("Visual")]
    public Light glowLight;
    public Renderer meshRenderer;

    [Header("Detection Angles (degrees)")]
    public float seenAngle   = 28f;
    public float neglectAngle = 80f;

    [Header("Peripheral Zone")]
    public float minPeripheralAngle   = 65f;
    public float maxPeripheralAngle   = 110f;
    public float orbitRadius          = 3f;
    public float orbitNoiseStrength   = 0.6f;
    public float boundarySteerStrength = 3f;
    public float noiseSpeed           = 0.4f;

    [Header("Flicker")]
    public float flickerSpeed     = 8f;
    public float flickerIntensity = 0.65f;

    [Header("Pre-Look Neglect")]
    public float preLookFadeDelay    = 20f;
    public float preLookFadeDuration = 9f;
    public float preLookMaxDistance  = 12f;

    [Header("Post-Look Neglect")]
    public float postLookFadeDelay    = 30f;
    public float postLookFadeDuration = 33f;
    public float postLookMaxDistance  = 10f;

    [Header("Audio Levels")]
    public float callingVolumeMax  = 1f;
    public float callingVolumeMin  = 0.3f;
    public float voiceVolumeOnLook = 0.9f;
    public float voiceVolumeMin    = 0.6f;

    // State
    public bool IsDead { get; private set; } = false;
    private bool hasBeenLookedAt  = false;

    // Audio
    private AudioSource callingSource;
    private AudioSource voiceSource;

    // Neglect
    private float neglectTimer = 0f;
    private float fadeAmount   = 0f;

    // Movement
    private Vector3 currentVelocity;
    private float   wanderSpeed;
    private float   noiseOffsetX;
    private float   noiseOffsetZ;
    private float   intensity  = 1f;
    private float   baseScale  = 0.14f;
    private float   pulsePhase;

    void Awake()
    {
        callingSource = gameObject.AddComponent<AudioSource>();
        voiceSource   = gameObject.AddComponent<AudioSource>();

        ConfigureSource(callingSource, spatialBlend: 1f,  minDist: 1f, maxDist: 8f);
        ConfigureSource(voiceSource,   spatialBlend: 0f, minDist: 1f, maxDist: 10f);
    }

    void ConfigureSource(AudioSource src, float spatialBlend,
        float minDist, float maxDist)
    {
        src.spatialBlend = spatialBlend;
        src.rolloffMode  = AudioRolloffMode.Logarithmic;
        src.minDistance  = minDist;
        src.maxDistance  = maxDist;
        src.loop         = true;
        src.volume       = 0f;
        src.playOnAwake  = false;
    }

    public void Initialize(int lookCount)
    {
        intensity    = 1f + (lookCount * 0.025f);
        baseScale    = 0.14f;
        wanderSpeed  = Random.Range(0.35f, 0.65f);
        noiseOffsetX = Random.Range(0f, 100f);
        noiseOffsetZ = Random.Range(0f, 100f);
        pulsePhase   = Random.Range(0f, Mathf.PI * 2f);

        transform.localScale = Vector3.one * baseScale;
        transform.position   = GetPeripheralSpawnPosition();

        // Pick a random calling sound from the array
        if (callingSounds != null && callingSounds.Length > 0)
        {
            AudioClip chosen     = callingSounds[Random.Range(0, callingSounds.Length)];
            callingSource.clip   = chosen;
            callingSource.volume = callingVolumeMax;
            callingSource.pitch  = Random.Range(pitchMin, pitchMax);
            callingSource.loop   = true;
            callingSource.Play();
        }
    }

    void Update()
    {
        if (IsDead) return;

        float angle = HeadTracker.Instance.AngleToTarget(transform.position);

        UpdateMovement();
        UpdateAttention(angle);
        UpdateNeglect(angle);
        UpdateFlicker();
    }


    void UpdateMovement()
    {
        Vector3 camPos     = HeadTracker.Instance.CameraPosition;
        float currentAngle = HeadTracker.Instance.AngleToTarget(transform.position);
        Vector3 steer      = Vector3.zero;

        if (currentAngle < minPeripheralAngle)
        {
            Vector3 away    = (transform.position - camPos).normalized;
            float pressure  = 1f - (currentAngle / minPeripheralAngle);
            steer          += away * boundarySteerStrength * pressure;
        }
        else if (currentAngle > maxPeripheralAngle)
        {
            Vector3 toward  = (camPos
                + HeadTracker.Instance.CameraForward * 4f
                - transform.position).normalized;
            float pressure  = (currentAngle - maxPeripheralAngle) / 20f;
            steer          += toward * boundarySteerStrength * pressure;
        }

        float t = Time.time * noiseSpeed;
        Vector3 wander = new Vector3(
            Mathf.PerlinNoise(t + noiseOffsetX, 0f) * 2f - 1f,
            (Mathf.PerlinNoise(t + noiseOffsetX, 99f) * 2f - 1f) * 0.3f,
            Mathf.PerlinNoise(0f, t + noiseOffsetZ) * 2f - 1f
        ).normalized * wanderSpeed;

        Vector3 desired  = wander + steer;

        float maxDist    = hasBeenLookedAt ? postLookMaxDistance : preLookMaxDistance;
        float targetDist = Mathf.Lerp(orbitRadius, maxDist, fadeAmount)
            + (Mathf.PerlinNoise(t * 0.5f, noiseOffsetX) - 0.5f)
            * orbitNoiseStrength;

        float currentDist = Vector3.Distance(transform.position, camPos);
        if (Mathf.Abs(currentDist - targetDist) > 0.3f)
        {
            Vector3 radialDir    = (transform.position - camPos).normalized;
            Vector3 radialTarget = camPos + radialDir * targetDist;
            desired             += (radialTarget - transform.position) * 2f;
        }

        currentVelocity    = Vector3.Lerp(
            currentVelocity, desired, Time.deltaTime * 2f);
        transform.position += currentVelocity * Time.deltaTime;
    }

    Vector3 GetPeripheralSpawnPosition()
    {
        Vector3 camPos = HeadTracker.Instance.CameraPosition;

        for (int i = 0; i < 40; i++)
        {
            Vector3 candidate = camPos + Random.onUnitSphere * orbitRadius;
            float angle = HeadTracker.Instance.AngleToTarget(candidate);
            if (angle >= minPeripheralAngle && angle <= maxPeripheralAngle)
                return candidate;
        }

        return camPos
            + HeadTracker.Instance.headCamera.transform.right * orbitRadius;
    }

    public void QuieterScript(float toVolume)
    {
        if (voiceSource != null && voiceSource.isPlaying)
        voiceSource.volume = toVolume;
    }



    void UpdateAttention(float angle)
    {
        if (IsDead) return;
        if (angle < seenAngle && !hasBeenLookedAt)
        {
            hasBeenLookedAt  = true;
            neglectTimer     = 0f;
            fadeAmount       = 0f;

            if (caughtSound != null)
            AudioSource.PlayClipAtPoint(caughtSound, transform.position, caughtVolume);

            // Picking a voice script audio clip and randomising its pitch
            if (voiceClips != null && voiceClips.Length > 0)
            {
                voiceSource.clip   = voiceClips[Random.Range(0, voiceClips.Length)];
                voiceSource.pitch  = Random.Range(pitchMin, pitchMax);
                voiceSource.volume = voiceVolumeOnLook;
                voiceSource.loop   = false;
                voiceSource.Play();
            }

            caughtBurst.Play();

            StimulusManager.Instance.OnStimulusLookedAt(this);
        }
    }



    void UpdateNeglect(float angle)
    {
        float fadeDelay    = hasBeenLookedAt ? postLookFadeDelay    : preLookFadeDelay;
        float fadeDuration = hasBeenLookedAt ? postLookFadeDuration : preLookFadeDuration;

        if (angle > neglectAngle)
        {
            neglectTimer += Time.deltaTime;

            if (neglectTimer > fadeDelay)
            {
                float progress = (neglectTimer - fadeDelay) / fadeDuration;
                fadeAmount     = Mathf.Clamp01(progress);
            }

            if (fadeAmount >= 1f && !hasBeenLookedAt)
            {
                StartCoroutine(Disintegrate());
                return;
            }
        }
        else
        {
            neglectTimer = Mathf.Max(0f, neglectTimer - Time.deltaTime * 3f);
            fadeAmount   = Mathf.Lerp(fadeAmount, 0f, Time.deltaTime * 2f);
        }

        // Calling sound fades if the stimulus is being ignored
        float targetCalling  = Mathf.Lerp(callingVolumeMax, callingVolumeMin, fadeAmount);
        callingSource.volume = Mathf.Lerp(
            callingSource.volume, targetCalling, Time.deltaTime * 1.5f);
    }

    IEnumerator Disintegrate()
    {
        IsDead = true;

        float duration   = 2.5f;
        float elapsed    = 0f;
        float startScale = transform.localScale.x;
        float startLight = glowLight != null ? glowLight.intensity : 0f;
        float startVol   = callingSource.volume;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t  = elapsed / duration;

            transform.localScale = Vector3.one * Mathf.Lerp(startScale, 0f, t);

            if (glowLight != null)
                glowLight.intensity = Mathf.Lerp(startLight, 0f, t);
            callingSource.volume = Mathf.Lerp(startVol, 0f, t);
            yield return null;
        }

        Destroy(gameObject);
    }



    void UpdateFlicker()
    {
        if (glowLight == null) return;

        float flicker =
            Mathf.Sin(Time.time * flickerSpeed)* 0.4f +
            Mathf.Sin(Time.time * flickerSpeed * 2.3f)* 0.3f +
            Mathf.Sin(Time.time * flickerSpeed * 5.7f)* 0.2f +
            Mathf.Sin(Time.time * flickerSpeed * 11.3f) * 0.1f;

        flicker = (flicker + 1f) * 0.5f;

        float fadedIntensity = Mathf.Lerp(1f, 0.08f, fadeAmount);
        glowLight.intensity  = (2f + flickerIntensity * flicker * 4f)
            * intensity * fadedIntensity;
    }

    void OnDestroy() { IsDead = true; }
}