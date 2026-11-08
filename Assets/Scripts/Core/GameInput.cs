using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// Thin wrapper over the Input System so gameplay code never touches devices directly.
public static class GameInput
{
    public static Vector2 PointerPosition
    {
        get
        {
            if (Mouse.current != null) return Mouse.current.position.ReadValue();
            if (Touchscreen.current != null) return Touchscreen.current.primaryTouch.position.ReadValue();
            return Vector2.zero;
        }
    }

    public static bool ClickedThisFrame =>
        (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) ||
        (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame);

    public static bool PausePressed =>
        Keyboard.current != null && (Keyboard.current.escapeKey.wasPressedThisFrame || Keyboard.current.pKey.wasPressedThisFrame);

    public static bool LeftPressed =>
        Keyboard.current != null && (Keyboard.current.leftArrowKey.wasPressedThisFrame || Keyboard.current.aKey.wasPressedThisFrame);

    public static bool RightPressed =>
        Keyboard.current != null && (Keyboard.current.rightArrowKey.wasPressedThisFrame || Keyboard.current.dKey.wasPressedThisFrame);

    /// True when the pointer is over a UI element (buttons, panels), so world clicks should be ignored.
    public static bool PointerOverUI =>
        EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

    public static bool DebugKey(int n)
    {
        var k = Keyboard.current;
        if (k == null) return false;
        switch (n)
        {
            case 1: return k.digit1Key.wasPressedThisFrame;
            case 2: return k.digit2Key.wasPressedThisFrame;
            case 3: return k.digit3Key.wasPressedThisFrame;
            case 4: return k.digit4Key.wasPressedThisFrame;
        }
        return false;
    }
}
