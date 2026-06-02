using UnityEngine;


// *VARYING THE COLOUR AND SIZE OF EACH SRIMULUS*

public class StimulusAppearance : MonoBehaviour
{
    [Header("Colour Palette")]
    public Gradient palette;

    [Header("Scale Variation")]
    public float scaleMin = 0.8f;
    public float scaleMax = 1.6f;

    [Header("Links")]
    public ParticleSystem auraParticles;     // the main halo cloud
    public ParticleSystem caughtBurst; //spark burst when it's looked at
    public Light glowLight; // tinting the light child obj to match the colour of the stimulus (forgot initially)

    void Awake()
    {
        Randomise();
    }

    public void Randomise() // randomising the colour and scale particle stimulus
    {
        Color chosen = palette != null
            ? palette.Evaluate(Random.value) : Random.ColorHSV(0f, 1f, 0.6f, 1f, 0.7f, 1f);  // pick a random value

        if (auraParticles != null)
        {
            var main = auraParticles.main;
            main.startColor = chosen; // a tint so the chosen colour applies to the spawning particles
        }
        // 'caught' burst matching the same colour:
        if (caughtBurst != null) 
        {
            var burstMain = caughtBurst.main;
            burstMain.startColor = chosen;
        }


        if (glowLight != null)
            glowLight.color = chosen; // apply to glow light

        // slightly diff size for each particle:
        float s = Random.Range(scaleMin, scaleMax);
        transform.localScale = transform.localScale * s;
    }
}