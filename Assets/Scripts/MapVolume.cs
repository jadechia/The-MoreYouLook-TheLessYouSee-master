using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[RequireComponent(typeof(Volume))]
public class HueRandomiser : MonoBehaviour
{
    void Start()
    {
        Volume volume = GetComponent<Volume>();
        if (volume.profile.TryGet(out ColorAdjustments color))
        {
            color.hueShift.overrideState = true;
            color.hueShift.value = Random.Range(-90f, 180f);
        }
    }
}