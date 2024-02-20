using LethalCompanyInputUtils.Api;
using UnityEngine.InputSystem;

namespace touchscreen;

public class TouchScreenInputClass : LcInputActions {
    public static readonly TouchScreenInputClass Instance = new();

    [InputAction("<Keyboard>/e", ActionId = "Touchscreen:Primary", Name = "Primary")]
    public InputAction PRIMARY {
        get => InputUtil.INPUT_PRIMARY;
        set => InputUtil.INPUT_PRIMARY = value;
    }

    [InputAction("<Mouse>/leftButton", ActionId = "Touchscreen:Secondary", Name = "Secondary")]
    public InputAction SECONDARY {
        get => InputUtil.INPUT_SECONDARY;
        set => InputUtil.INPUT_SECONDARY = value;
    }

    [InputAction("", ActionId = "Touchscreen:QuickSwitch", Name = "QuickSwitch")]
    public InputAction QUICK_SWITCH {
        get => InputUtil.INPUT_QUICKSWITCH;
        set => InputUtil.INPUT_QUICKSWITCH = value;
    }

    [InputAction("", ActionId = "Touchscreen:AltQuickSwitch", Name = "Alt QuickSwitch")]
    public InputAction ALT_QUICK_SWITCH {
        get => InputUtil.INPUT_ALT_QUICKSWITCH;
        set => InputUtil.INPUT_ALT_QUICKSWITCH = value;
    }

}