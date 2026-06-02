using UnityEngine;

// *TRACKS WHERE THE HEAD ANGLE IS AND ADJUSTS THE CAMERA ACCORDINGLY*
public class HeadTracker : MonoBehaviour
{
    public static HeadTracker Instance;

    [Header("References")]
    public Camera headCamera;

    [Header("Thresholds (degrees)")]
    public float fovealThreshold = 28f; // 'looking at' cone
    public float peripheralMin = 30f;      // min angle dist to be in periphery
    public float peripheralMax = 80f;      // outer peripheral limit ( so its not behind user )
    public float spawnBehindMin = 85f; // defining ehst counts as behind

    void Awake()
    {
        Instance = this;
        if (headCamera == null)
            headCamera = Camera.main;
    }

    
    public float AngleToTarget(Vector3 worldPosition)
    {
        Vector3 dirToTarget = (worldPosition - headCamera.transform.position).normalized;
        return Vector3.Angle(headCamera.transform.forward, dirToTarget);
    }

    
    public bool IsLookingAt(Vector3 worldPosition)
    {
        return AngleToTarget(worldPosition) < fovealThreshold; // checking if angle is within the cone threshold
    }

   
    public bool IsInPeriphery(Vector3 worldPosition)
    {
        float angle = AngleToTarget(worldPosition);
        return angle >= peripheralMin && angle <= peripheralMax;
    }

    
    public Vector3 CameraPosition => headCamera.transform.position;
    public Vector3 CameraForward => headCamera.transform.forward;
}