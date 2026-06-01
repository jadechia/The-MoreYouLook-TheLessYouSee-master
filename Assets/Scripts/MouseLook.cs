using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
public class EditorMouseLook : MonoBehaviour
{
    [Header("Look Sensitivity")]
    public float sensitivity = 0.1f;    
    public float pitchMin = -80f;
    public float pitchMax = 80f;

    [Header("Controls")]

    public bool holdRightMouseToLook = false;

    public bool lockCursor = true;

    private float yaw;
    private float pitch;

    void Start()
    {
        Vector3 e = transform.localEulerAngles;
        yaw   = e.y;
        pitch = e.x;
        if (pitch > 180f) pitch -= 360f;

        ApplyCursorState();
    }

    void Update()
    {
#if ENABLE_INPUT_SYSTEM
        Mouse mouse = Mouse.current;
        if (mouse == null) return;

        bool looking = !holdRightMouseToLook || mouse.rightButton.isPressed;

        if (looking)
        {
            Vector2 delta = mouse.delta.ReadValue();
            yaw   += delta.x * sensitivity;
            pitch -= delta.y * sensitivity;
            pitch  = Mathf.Clamp(pitch, pitchMin, pitchMax);

            transform.localRotation = Quaternion.Euler(pitch, yaw, 0f);
        }
#endif
    }

    void ApplyCursorState()
    {
        if (lockCursor && !holdRightMouseToLook)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}