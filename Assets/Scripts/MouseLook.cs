using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Editor/Play-mode mouse-look proxy for the headset, written for the
/// new Unity Input System (Mouse.current / Keyboard.current).
///
/// SETUP:
///   1. Add this component to your Main Camera (the one inside XR Origin
///      that HeadTracker reads).
///   2. Press Play. Move the mouse to look around. Stimuli react to where
///      the camera points exactly as head tracking would on device.
///
/// NOTES:
///   - Keep HeadTracker enabled — it reads this camera's forward direction.
///   - Disable THIS component before building (real XR tracking takes over).
/// </summary>
public class EditorMouseLook : MonoBehaviour
{
    [Header("Look Sensitivity")]
    public float sensitivity = 0.1f;      // new Input System mouse delta is larger
    public float pitchMin = -80f;
    public float pitchMax = 80f;

    [Header("Controls")]
    [Tooltip("If true, hold right mouse button to look. If false, always look.")]
    public bool holdRightMouseToLook = false;

    [Tooltip("Lock and hide the cursor while looking.")]
    public bool lockCursor = true;

    [Header("Optional WASD Movement")]
    public bool enableMovement = false;
    public float moveSpeed = 2f;

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
        if (mouse == null) return;   // no mouse present

        bool looking = !holdRightMouseToLook || mouse.rightButton.isPressed;

        if (looking)
        {
            Vector2 delta = mouse.delta.ReadValue();
            yaw   += delta.x * sensitivity;
            pitch -= delta.y * sensitivity;
            pitch  = Mathf.Clamp(pitch, pitchMin, pitchMax);

            transform.localRotation = Quaternion.Euler(pitch, yaw, 0f);
        }

        if (enableMovement)
        {
            Keyboard kb = Keyboard.current;
            if (kb != null)
            {
                float h = (kb.dKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed ? 1f : 0f);
                float v = (kb.wKey.isPressed ? 1f : 0f) - (kb.sKey.isPressed ? 1f : 0f);
                Vector3 move = (transform.right * h + transform.forward * v)
                    * moveSpeed * Time.deltaTime;
                transform.position += move;
            }
        }

        // Escape frees the cursor
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = false;
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