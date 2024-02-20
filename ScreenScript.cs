using System;
using System.Collections.Generic;
using GameNetcodeStuff;
using UnityEngine;
using UnityEngine.InputSystem;

namespace touchscreen {

    public class ScreenScript : MonoBehaviour {
        private static PlayerControllerB LOCAL_PLAYER => GameNetworkManager.Instance?.localPlayerController;
        private static ManualCameraRenderer MAP_RENDERER => StartOfRound.Instance?.mapScreen;
        private const float _isCloseMax = 1.25f;
        private bool _lookingAtMonitor = false;

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

        internal void OnPlayerInteraction(bool isAlt) {
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

        internal void OnPlayerQuickSwitch(bool isAlt) {
            PlayerControllerB ply = LOCAL_PLAYER;
            if (ply?.isInHangarShipRoom == true) {
                Vector3 vec = this.gameObject.transform.position - ply.transform.position;
                float distance = Math.Abs(vec.x) + Math.Abs(vec.y) + Math.Abs(vec.z);
                if (distance < 6.85f) {
                    if (!isAlt) {
                        if (ConfigUtil.CONFIG_ALT_REVERSE.Value && InputUtil.INPUT_ALT_QUICKSWITCH.IsPressed())
                            MAP_RENDERER.SwitchRadarTargetBackwards(true);
                        else
                            MAP_RENDERER.SwitchRadarTargetForward(true);
                    } else if (!ConfigUtil.CONFIG_ALT_REVERSE.Value)
                        MAP_RENDERER.SwitchRadarTargetBackwards(true);
                }
            }
        }

        private void OnEnable() {
            InputUtil.SCREEN_SCRIPT = this;

            PlayerControllerB ply = LOCAL_PLAYER;
            if (ply == null) {
                Plugin.LOGGER.LogWarning("Unable to activate monitor touchscreen. Reason: Failed to get local player.");
                return;
            } else if (InputUtil.INPUT_SECONDARY != null || InputUtil.INPUT_SECONDARY != null)
                return;
        }

        private void OnDisable() {
            InputUtil.SCREEN_SCRIPT = null;
        }

        private void Update() {
            PlayerControllerB ply = LOCAL_PLAYER;
            if (Plugin.IsActive && IsLookingAtMonitor(out Bounds bounds, out Ray lookRay, out Ray camRay)) {
                if (!_lookingAtMonitor) {
                    _lookingAtMonitor = true;
                    ply.isGrabbingObjectAnimation = true; // Blocks the default code from overwriting it again
                    if (ConfigUtil.CONFIG_SHOW_POINTER.Value) { // Display Pointer
                        ply.cursorIcon.enabled = true;
                        ply.cursorIcon.sprite = ConfigUtil.HOVER_ICON;
                    }
                    if (ConfigUtil.CONFIG_SHOW_TOOLTIP.Value) { // Display Tooltips
                        ply.cursorTip.text = String.Format("""
                            [{0}] Interact
                            [{1}] Flash (Radar)
                            {2}
                            """,
                            InputUtil.GetButtonDescription(InputUtil.INPUT_PRIMARY),
                            InputUtil.GetButtonDescription(InputUtil.INPUT_SECONDARY),
                            String.IsNullOrWhiteSpace(ConfigUtil.CONFIG_QUICK_SWITCH.Value) ?
                                "" :
                                "[" + InputUtil.GetButtonDescription(InputUtil.INPUT_QUICKSWITCH) + "] Switch target"
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