using UnityEngine;

public class NucleusSetup : MonoBehaviour
{
    public ReflectionProbe reflectionProbe;

    [Header("Follow Settings")]
    public float followSpeed = 1.2f;
    public float forwardDistance = 2.5f;
    public float absoluteHeight = 1.7f;

    private Camera headCamera;

    void Start()
    {
        headCamera = Camera.main;
        if (reflectionProbe != null)
        {
            reflectionProbe.transform.position = transform.position;
            reflectionProbe.RenderProbe();
        }
    }

    void Update()
    {
        if (headCamera == null) return;

        Vector3 forward = headCamera.transform.forward;
        forward.y = 0f;
        forward.Normalize();

        Vector3 target = headCamera.transform.position
            + forward * forwardDistance;
        target.y = absoluteHeight;

        transform.position = Vector3.Lerp(
            transform.position, target, Time.deltaTime * followSpeed);
    }
}