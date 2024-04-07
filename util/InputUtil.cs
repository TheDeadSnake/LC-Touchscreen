using System;
using BepInEx;
using BepInEx.Bootstrap;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;
using GameNetcodeStuff;

namespace touchscreen;

public static class InputUtil {
    private static ScreenScript _screen_script = null;
    public static ScreenScript SCREEN_SCRIPT {
        get => _screen_script;
        internal set {
            // Check if ScreenScript is null
            if (value) { // Script is not null / was enabled
                setupActions(value);
            } else { // Script is null / was disabled
                clearActions();
            }

            _screen_script = value;
        }
    }

    // Inputs
    internal static InputAction INPUT_PRIMARY;
    internal static InputAction INPUT_SECONDARY;
    internal static InputAction INPUT_QUICKSWITCH;
    internal static InputAction INPUT_ALT_QUICKSWITCH;

    // Input Actions
    private delegate void Execute();
    private static Execute _primaryExecute = () => {};
    private static Execute _secondaryExecute = () => {};
    private static Execute _quickSwitchExecute = () => {};
    private static Execute _altQuickSwitchExecute = () => {};

    // LethalCompanyVR - support
    public static bool inVR { get; private set; } = false;
    internal static Plugin.Func<Ray, PlayerControllerB> LOOK_RAY = ply => new Ray(ply.gameplayCamera.transform.position, ply.gameplayCamera.transform.forward);

    /*
        Helper functions    
    */

    public static string GetButtonDescription(InputAction action) {
        bool isController = StartOfRound.Instance ? StartOfRound.Instance.localPlayerUsingController : false;
        bool isPS = isController && (Gamepad.current is DualShockGamepad || Gamepad.current is DualShock3GamepadHID || Gamepad.current is DualShock4GamepadHID);
        InputBinding? binding = null;
        foreach (InputBinding x in action.bindings) {
            if (InputUtil.inVR && (x.effectivePath.StartsWith("<XRController>"))) {
                binding = x;
                break;
            } else if (isController && x.effectivePath.StartsWith("<Gamepad>")) {
                binding = x;
                break;
            } else if (!isController && (x.effectivePath.StartsWith("<Keyboard>") || x.effectivePath.StartsWith("<Mouse>"))) {
                binding = x;
                break;
            }
        }

        string path = binding != null ? binding.Value.effectivePath : "";
        string[] splits = path.Split("/");
        return (splits.Length > 1 ? path : "") switch {
            // Mouse
            "<Mouse>/leftButton" => "LΜB",  // Uses 'Greek Capital Letter Mu' for M
            "<Mouse>/rightButton" => "RΜB", // Uses 'Greek Capital Letter Mu' for M
            // Keyboard
            "<Keyboard>/escape" => "ESC",
            // Controller
            // Right buttons
            "<Gamepad>/buttonNorth" => isPS ? "△" : "Y",
            "<Gamepad>/buttonEast" => isPS ? "◯" : "B",
            "<Gamepad>/buttonSouth" => isPS ? "X" : "A",
            "<Gamepad>/buttonWest" => isPS ? "□" : "X",
            // Sticks
            "<Gamepad>/leftStickPress" => "L-Stick",
            "<Gamepad>/rightStickPress" => "R-Stick",
            // Shoulder, Trigger buttons
            "<Gamepad>/leftShoulder" => isPS ? "L1" : "L-Shoulder",
            "<Gamepad>/leftTrigger" => isPS ? "L2" : "L-Trigger",
            "<Gamepad>/rightShoulder" => isPS ? "R1" : "R-Shoulder",
            "<Gamepad>/rightTrigger" => isPS ? "R2" : "R-Trigger",
            _ => splits.Length > 1 ? splits[1].ToUpper() : "?"
        };
    }

    private static InputAction CreateKeybind(string key, string binding, Action<InputAction.CallbackContext> action) {
        InputAction inputAction = new InputAction(
            name: key,
            type: InputActionType.Button,
            binding: binding
        );
        inputAction.performed += action;
        inputAction.Enable();
        Plugin.LOGGER.LogInfo($"Set {key} button to: {InputUtil.GetButtonDescription(inputAction)}");
        return inputAction;
    }

    private static void setupActions(ScreenScript script) {
        Plugin.LOGGER.LogInfo(" > Setup actions");
        InputUtil._primaryExecute = () => script.OnPlayerInteraction(false);
        InputUtil._secondaryExecute = () => script.OnPlayerInteraction(true);
        InputUtil._quickSwitchExecute = () => script.OnPlayerQuickSwitch(false);
        InputUtil._altQuickSwitchExecute = () => script.OnPlayerQuickSwitch(true);
    }

    private static void clearActions() {
        Plugin.LOGGER.LogInfo(" > Clear actions");
        InputUtil._primaryExecute = () => {};
        InputUtil._secondaryExecute = () => {};
        InputUtil._quickSwitchExecute = () => {};
        InputUtil._altQuickSwitchExecute = () => {};
    }

    /*
        Setup    
    */

    internal static void Setup() {
        // LethalCompanyVR support
        if (Chainloader.PluginInfos.TryGetValue("io.daxcess.lcvr", out PluginInfo info)) {
            InputUtil.inVR = LCVRUtil.Setup(info);
        }

        // Create keybinds
        Plugin.Supplier<bool> _iu_create = () => TouchScreenInputClass.Instance != null;
        // Note: Remove VR check once InputUtil supports VR controller
        if (!InputUtil.inVR && Chainloader.PluginInfos.TryGetValue("com.rune580.LethalCompanyInputUtils", out PluginInfo iu) && _iu_create.Invoke()) {
            INPUT_PRIMARY.performed += _ => InputUtil._primaryExecute();
            INPUT_SECONDARY.performed += _ => InputUtil._secondaryExecute();
            INPUT_QUICKSWITCH.performed += _ => InputUtil._quickSwitchExecute();
            INPUT_ALT_QUICKSWITCH.performed += _ => InputUtil._altQuickSwitchExecute();
            Plugin.LOGGER.LogInfo($" > Hooked into InputUtils {iu.Metadata.Version}");
        } else {
            INPUT_PRIMARY = InputUtil.CreateKeybind(
                "Touchscreen:Primary",
                InputUtil.inVR ? ConfigUtil.CONFIG_VR_PRIMARY.Value : ConfigUtil.CONFIG_PRIMARY.Value,
                _ => InputUtil._primaryExecute()
            );
            INPUT_SECONDARY = InputUtil.CreateKeybind(
                "Touchscreen:Secondary",
                InputUtil.inVR ? ConfigUtil.CONFIG_VR_SECONDARY.Value : ConfigUtil.CONFIG_SECONDARY.Value,
                _ => InputUtil._secondaryExecute()
            );
            INPUT_QUICKSWITCH = InputUtil.CreateKeybind(
                "Touchscreen:QuickSwitch",
                InputUtil.inVR ? ConfigUtil.CONFIG_VR_QUICK_SWITCH.Value : ConfigUtil.CONFIG_QUICK_SWITCH.Value,
                _ => InputUtil._quickSwitchExecute()
            );
            INPUT_ALT_QUICKSWITCH = InputUtil.CreateKeybind(
                "Touchscreen:AltQuickSwitch",
                InputUtil.inVR ? ConfigUtil.CONFIG_VR_ALT_QUICK_SWITCH.Value : ConfigUtil.CONFIG_ALT_QUICK_SWITCH.Value,
                _ => InputUtil._altQuickSwitchExecute()
            );
        }
    }

}