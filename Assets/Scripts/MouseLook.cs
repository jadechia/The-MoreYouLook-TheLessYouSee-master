using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// *SCRIPT TO CONTROL MAINCAMERA POSITION WITH MY MOUSE* (for when i dont have the headset & need to debug in Unity Editor)
public class EditorMouseLook : MonoBehaviour
{
    [Header("Look Sensitivity")]
    public float sensitivity = 1f;
    public float pitchMin = -80f;
    public float pitchMax = 80f;

    [Header("Controls")]
    [Tooltip("If true, hold right mouse button to look. If false, always look.")]
    public bool holdRightMouseToLook = false;

    [Tooltip("Lock and hide the cursor while looking.")]
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

            if (lockCursor)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
        else if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // press esc to release the cursor
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
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