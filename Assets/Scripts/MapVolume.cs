using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// *VARIES THE HUE OF THE ATTENTION MAP EVERY TIME IT RUNS*

[RequireComponent(typeof(Volume))]
public class HueRandomiser : MonoBehaviour
{
    void Start()
    {
        Volume volume = GetComponent<Volume>();
        if (volume.profile.TryGet(out ColorAdjustments color)) // checking just in case the component is unticked
        {
            color.hueShift.overrideState = true;
            color.hueShift.value = Random.Range(-20f, 180f);
        }
    }
}