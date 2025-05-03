using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InputManager : MonoBehaviour
{
    [SerializeField] private FixedJoystick joystick;
    [SerializeField] private Button jumpButton;
    [SerializeField] private Button dashCrouchButton;
    [SerializeField] private Button hatchButton; // New hatch button
    [SerializeField] private float holdThreshold = 0.2f;

    private bool isJumpPressed;
    private bool isDashCrouchHeld;
    private bool isDashCrouchPressed;
    private bool isDashCrouchReleased;
    private float dashCrouchPressTime;
    private bool isHatchHeld;

    public float Horizontal => joystick != null ? joystick.Horizontal : 0f;
    public float Vertical => joystick != null ? joystick.Vertical : 0f; // For hatch direction
    public bool IsJumpPressed
    {
        get
        {
            bool value = isJumpPressed;
            isJumpPressed = false;
            return value;
        }
    }
    public bool IsDashCrouchPressed => isDashCrouchPressed;
    public bool IsDashCrouchHeld => isDashCrouchHeld && (Time.time - dashCrouchPressTime >= holdThreshold);
    public bool IsDashCrouchTapped => isDashCrouchReleased && (Time.time - dashCrouchPressTime < holdThreshold);
    public bool IsHatchHeld => isHatchHeld;

    void Start()
    {
        SetupButtonListeners();
    }

    void SetupButtonListeners()
    {
        if (jumpButton != null)
        {
            jumpButton.onClick.RemoveAllListeners();
            jumpButton.onClick.AddListener(() => isJumpPressed = true);
        }

        if (dashCrouchButton != null)
        {
            var trigger = dashCrouchButton.gameObject.GetComponent<EventTrigger>();
            if (trigger == null)
                trigger = dashCrouchButton.gameObject.AddComponent<EventTrigger>();

            trigger.triggers.Clear();

            var pointerDown = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            pointerDown.callback.AddListener((data) => OnDashCrouchButtonDown());
            trigger.triggers.Add(pointerDown);

            var pointerUp = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
            pointerUp.callback.AddListener((data) => OnDashCrouchButtonUp());
            trigger.triggers.Add(pointerUp);
        }

        if (hatchButton != null)
        {
            var trigger = hatchButton.gameObject.GetComponent<EventTrigger>();
            if (trigger == null)
                trigger = hatchButton.gameObject.AddComponent<EventTrigger>();

            trigger.triggers.Clear();

            var pointerDown = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            pointerDown.callback.AddListener((data) => isHatchHeld = true);
            trigger.triggers.Add(pointerDown);

            var pointerUp = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
            pointerUp.callback.AddListener((data) => isHatchHeld = false);
            trigger.triggers.Add(pointerUp);
        }
    }

    void OnDashCrouchButtonDown()
    {
        isDashCrouchHeld = true;
        isDashCrouchPressed = true;
        dashCrouchPressTime = Time.time;
    }

    void OnDashCrouchButtonUp()
    {
        isDashCrouchHeld = false;
        isDashCrouchReleased = true;
    }

    void LateUpdate()
    {
        isDashCrouchPressed = false;
        isDashCrouchReleased = false;
    }
}