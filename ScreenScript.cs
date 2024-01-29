using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Bootstrap;
using GameNetcodeStuff;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;

namespace touchscreen {

    public class ScreenScript : MonoBehaviour {
        private static PlayerControllerB LOCAL_PLAYER => GameNetworkManager.Instance?.localPlayerController;
        private static ManualCameraRenderer MAP_RENDERER => StartOfRound.Instance?.mapScreen;
        private const float _isCloseMax = 1.25f;
        private bool _lookingAtMonitor = false;
        private InputAction _primary;
        private InputAction _secondary;
        private InputAction _quickSwitch;
        private InputAction _altQuickSwitch;
        private Action<InputAction.CallbackContext> _primaryAction;
        private Action<InputAction.CallbackContext> _secondaryAction;
        private Action<InputAction.CallbackContext> _quickSwitchAction;
        private Action<InputAction.CallbackContext> _altQuickSwitchAction;

        private Bounds GetBounds() {
            return Plugin.CREATE_BOUNDS.Invoke(this.gameObject);
        }

        private bool IsLookingAtMonitor(out Bounds bound, out Ray viewRay, out Ray camRay) {
            PlayerControllerB ply = LOCAL_PLAYER;
            if (ply is not null && ply.isInHangarShipRoom) {
                Ray lookRay = new Ray(ply.gameplayCamera.transform.position, ply.gameplayCamera.transform.forward);
                Bounds bounds = GetBounds();

                if (bounds.IntersectRay(lookRay, out float distance) && distance <= ply.grabDistance) {
                    bound = bounds;
                    viewRay = lookRay;
                    camRay = MAP_RENDERER.cam.ViewportPointToRay(GetMonitorCoordinates(bounds, lookRay.GetPoint(distance)));
                    return true;
                }
            }
            bound = default;
            viewRay = default;
            camRay = default;
            return false;
        }

        private Vector3 GetMonitorCoordinates(Bounds bounds, Vector3 point) {
            return new Vector3(
                1f - 1f / Math.Abs(bounds.max.z - bounds.min.z) * (point.z - bounds.min.z),
                1f / Math.Abs(bounds.max.y - bounds.min.y) * (point.y - bounds.min.y),
                0
            );
        }

        private bool TriggerRadar(RadarBoosterItem rItem, bool isAlt) {
            if (rItem != null) {
                if (isAlt)
                    rItem.FlashAndSync();
                else
                    rItem.PlayPingAudioAndSync();
                return true;
            } else
                return false;
        }

        private void OnPlayerInteraction(bool isAlt) {
            PlayerControllerB ply = LOCAL_PLAYER;
            if (ply != null && Plugin.IsActive && IsLookingAtMonitor(out Bounds bounds, out Ray lookRay, out Ray camRay)) {
                foreach (Collider x in Physics.OverlapCapsule(camRay.GetPoint(0), camRay.GetPoint(10), _isCloseMax)) {
                    if (!isAlt && x.GetComponent<TerminalAccessibleObject>() is TerminalAccessibleObject tObject) { // Clicked on BigDoor, Land mine, Turret
                        tObject.CallFunctionFromTerminal();
                        return;
                    } else if (x.GetComponent<RadarBoosterItem>() is RadarBoosterItem rItem) { // Clicked on Radar booster
                        TriggerRadar(rItem, isAlt);
                        return;
                    } else if (x.GetComponent<PlayerControllerB>() is PlayerControllerB tgtPlayer) { // Clicked on player or radar the player is holding
                        if (!TriggerRadar(tgtPlayer.currentlyHeldObjectServer?.GetComponent<RadarBoosterItem>(), isAlt) && !isAlt) {
                            List<TransformAndName> list = MAP_RENDERER.radarTargets;
                            for (int i = 0; i < list.Count; i++) {
                                if (tgtPlayer.transform.Equals(list[i].transform)) {
                                    MAP_RENDERER.SwitchRadarTargetAndSync(i);
                                    return;
                                }
                            }
                        }
                    }
                }
            }
        }

        private void OnPlayerQuickSwitch(bool isAlt) {
            PlayerControllerB ply = LOCAL_PLAYER;
            if (ply?.isInHangarShipRoom == true) {
                Vector3 vec = this.gameObject.transform.position - ply.transform.position;
                float distance = Math.Abs(vec.x) + Math.Abs(vec.y) + Math.Abs(vec.z);
                if (distance < 6.85f) {
                    if (!isAlt) {
                        if (Plugin.CONFIG_ALT_REVERSE.Value && _altQuickSwitch.IsPressed())
                            MAP_RENDERER.SwitchRadarTargetBackwards(true);
                        else
                            MAP_RENDERER.SwitchRadarTargetForward(true);
                    } else if (!Plugin.CONFIG_ALT_REVERSE.Value)
                        MAP_RENDERER.SwitchRadarTargetBackwards(true);
                }
            }
        }

        private string GetButtonDescription(InputAction action) {
            bool isController = StartOfRound.Instance.localPlayerUsingController;
            bool isPS = isController && (Gamepad.current is DualShockGamepad || Gamepad.current is DualShock3GamepadHID || Gamepad.current is DualShock4GamepadHID);
            InputBinding? binding = null;
            foreach (InputBinding x in action.bindings) {
                if (isController && x.effectivePath.StartsWith("<Gamepad>")) {
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

        private InputAction CreateKeybind(string key, string binding, Action<InputAction.CallbackContext> action) {
            InputAction inputAction = new InputAction(
                name: key,
                type: InputActionType.Button,
                binding: binding
            );
            inputAction.performed += action;
            inputAction.Enable();

            Plugin.LOGGER.LogInfo($"Set ${key} button to: ${GetButtonDescription(inputAction)}");
            return inputAction;
        }

        private void OnEnable() {
            PlayerControllerB ply = LOCAL_PLAYER;
            if (ply == null) {
                Plugin.LOGGER.LogWarning("Unable to activate monitor touchscreen. Reason: Failed to get local player.");
                return;
            } else if (_primary != null || _secondary != null)
                return;

            // Create new InputActions
            _primary = CreateKeybind(
                "Touchscreen:Primary",
                Plugin.CONFIG_PRIMARY.Value,
                _primaryAction = (_ => OnPlayerInteraction(false))
            );
            _secondary = CreateKeybind(
                "Touchscreen:Secondary",
                Plugin.CONFIG_SECONDARY.Value,
                _secondaryAction = (_ => OnPlayerInteraction(true))
            );
            _quickSwitch = CreateKeybind(
                "Touchscreen:QuickSwitch",
                Plugin.CONFIG_QUICK_SWITCH.Value,
                _quickSwitchAction = (_ => OnPlayerQuickSwitch(false))
            );
            _altQuickSwitch = CreateKeybind(
                "Touchscreen:AltQuickSwitch",
                Plugin.CONFIG_ALT_QUICK_SWITCH.Value,
                _altQuickSwitchAction = (_ => OnPlayerQuickSwitch(true))
            );
        }

        private void OnDisable() {
            _primary.performed -= _primaryAction;
            _secondary.performed -= _secondaryAction;
            _quickSwitch.performed -= _quickSwitchAction;
            _altQuickSwitch.performed -= _altQuickSwitchAction;
        }

        private void Update() {
            PlayerControllerB ply = LOCAL_PLAYER;
            if (Plugin.IsActive && IsLookingAtMonitor(out Bounds bounds, out Ray lookRay, out Ray camRay)) {
                if (!_lookingAtMonitor) {
                    _lookingAtMonitor = true;
                    ply.isGrabbingObjectAnimation = true; // Blocks the default code from overwriting it again
                    if (Plugin.CONFIG_SHOW_POINTER.Value) { // Display Pointer
                        ply.cursorIcon.enabled = true;
                        ply.cursorIcon.sprite = Plugin.HOVER_ICON;
                    }
                    if (Plugin.CONFIG_SHOW_TOOLTIP.Value) { // Display Tooltips
                        ply.cursorTip.text = String.Format("""
                            [{0}] Interact
                            [{1}] Flash (Radar)
                            {2}
                            """,
                            GetButtonDescription(_primary),
                            GetButtonDescription(_secondary),
                            String.IsNullOrWhiteSpace(Plugin.CONFIG_QUICK_SWITCH.Value) ?
                                "" :
                                "[" + GetButtonDescription(_quickSwitch) + "] Switch target"
                        );
                    }
                }
            } else if (ply != null && _lookingAtMonitor) {
                ply.isGrabbingObjectAnimation = false;
                _lookingAtMonitor = false;
            }

        }

    }

}