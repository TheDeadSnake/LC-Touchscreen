using System;
using System.Collections.Generic;
using GameNetcodeStuff;
using Unity.Netcode;
using UnityEngine;

namespace touchscreen {

    [DefaultExecutionOrder(100)] // Guarantees that this LateUpdate is executed after LCVR LateUpdate
    public class ScreenScript : MonoBehaviour {
        private static PlayerControllerB LOCAL_PLAYER => GameNetworkManager.Instance?.localPlayerController;
        private static ManualCameraRenderer MAP_RENDERER => StartOfRound.Instance?.mapScreen;
        private const float _isCloseMax = 1.25f;
        private LineRenderer _vrRay;
        private bool _vlookingAtMonitor;
        private bool _lookingAtMonitor {
            get => _vlookingAtMonitor;
            set {
                if (_vrRay is not null) {
                    _vrRay.enabled = value;
                }
                _vlookingAtMonitor = value;
            }
        }

        // private LineRenderer _dbgRenderer;
        // private int _dbgDraw;
        // public void dbg(bool mapBound) {
        //     if (!_dbgRenderer) {
        //         _dbgRenderer = gameObject.AddComponent<LineRenderer>();
        //     }
        //
        //     _dbgRenderer.loop = false;
        //     _dbgRenderer.useWorldSpace = true;
        //
        //     if (mapBound) {
        //         _dbgRenderer.SetPositions(new Vector3[10]);
        //         _dbgRenderer.positionCount = 10;
        //         _dbgRenderer.startColor = Color.magenta;
        //         _dbgRenderer.endColor = Color.magenta;
        //         _dbgRenderer.startWidth = .05f;
        //         _dbgRenderer.endWidth = .05f;
        //         _dbgDraw = 1;
        //     } else {
        //         _dbgRenderer.SetPositions(new Vector3[2]);
        //         _dbgRenderer.positionCount = 2;
        //         _dbgRenderer.startWidth = _isCloseMax * 2;
        //         _dbgRenderer.endWidth = _isCloseMax * 2;
        //         _dbgRenderer.startColor = Color.red;
        //         _dbgRenderer.endColor = Color.red;
        //         _dbgDraw = 2;
        //     }
        // }

        private Bounds GetBounds() {
            // Debug
            // Bounds b = Plugin.CREATE_BOUNDS.Invoke(gameObject);
            // if (_dbgDraw == 1) {
            //     _dbgRenderer.SetPositions(new [] {
            //         b.min,
            //         new(b.max.x, b.min.y, b.min.z),
            //         new(b.max.x, b.max.y, b.min.z),
            //         new(b.min.x, b.max.y, b.min.z),
            //         b.min,
            //         new(b.min.x, b.min.y, b.max.z),
            //         new(b.max.x, b.min.y, b.max.z),
            //         new(b.max.x, b.max.y, b.max.z),
            //         new(b.min.x, b.max.y, b.max.z),
            //         new(b.min.x, b.min.y, b.max.z),
            //     });
            // }
            // return b;

            return Plugin.CREATE_BOUNDS.Invoke(gameObject);
        }

        // public static float xOffset = 1.5f;
        // public static float zOffset = 0.05f;
        
        private bool IsLookingAtMonitor(out Bounds bound, out Ray viewRay, out Ray camRay) {
            PlayerControllerB ply = LOCAL_PLAYER;
            if (ply is not null && ply.isInHangarShipRoom) {
                Ray lookRay = InputUtil.LOOK_RAY.Invoke(ply);
                Bounds bounds = GetBounds();
                if (bounds.IntersectRay(lookRay, out float distance) && distance <= ply.grabDistance) {
                    bound = bounds;
                    viewRay = lookRay;
                    camRay = MAP_RENDERER.cam.ViewportPointToRay(GetMonitorCoordinates(bounds, lookRay.GetPoint(distance)));
                    Plugin.ORBITAL_OFFSET.Invoke(ref camRay.m_Origin);
                    // camRay.m_Origin.x += xOffset;
                    // camRay.m_Origin.z += zOffset;
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
                // Debug
                // if (_dbgDraw == 2) {
                //     _dbgRenderer.SetPositions(new[] {
                //         camRay.origin,
                //         camRay.GetPoint(10),
                //     });
                // }
                
                foreach (Collider x in Physics.OverlapCapsule(camRay.GetPoint(0), camRay.GetPoint(10), _isCloseMax)) {
                    if (!isAlt && x.GetComponent<TerminalAccessibleObject>() is { } tObject) { // Clicked on BigDoor, Land mine, Turret
                        tObject.CallFunctionFromTerminal();
                        return;
                    } else if (!isAlt && x.gameObject.GetComponentInParent<NetworkObject>() is { } oSpike && oSpike.name.StartsWith("SpikeRoofTrapHazard")) { // Clicked on Spike trap
                        oSpike.GetComponentInChildren<TerminalAccessibleObject>()?.CallFunctionFromTerminal();
                        return;
                    } else if (x.GetComponent<RadarBoosterItem>() is { } rItem) { // Clicked on Radar booster
                        TriggerRadar(rItem, isAlt);
                        return;
                    } else if (x.GetComponent<PlayerControllerB>() is { } tgtPlayer) { // Clicked on player or radar the player is holding
                        if (!TriggerRadar(tgtPlayer.currentlyHeldObjectServer?.GetComponent<RadarBoosterItem>(), isAlt) && !isAlt) {
                            List<TransformAndName> list = MAP_RENDERER.radarTargets;
                            for (int i = 0; i < list.Count; i++) {
                                if (tgtPlayer.transform.Equals(list[i].transform)) {
                                    MAP_RENDERER.SwitchRadarTargetAndSync(i);
                                    return;
                                }
                            }
                        }
                    } else if (!isAlt && ToilHeadUtil.IsEnabled && ToilHeadUtil.CallFollowTerminalAccessibleObject(x)) { // Clicked on ToilHead TerminalAccessibleObject
                        return;
                    }
                }
            }
        }

        internal void OnPlayerQuickSwitch(bool isAlt) {
            PlayerControllerB ply = LOCAL_PLAYER;
            if (ply?.isInHangarShipRoom == true) {
                Vector3 vec = gameObject.transform.position - ply.transform.position;
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
            }

            // VR Ray
            if (InputUtil.inVR && ConfigUtil.CONFIG_VR_SHOW_RAY.Value) {
                _vrRay = gameObject.AddComponent<LineRenderer>();
                _vrRay.enabled = false;
                _vrRay.positionCount = 2;
                _vrRay.SetPositions(new[] {Vector3.zero, Vector3.zero});
                _vrRay.widthMultiplier = 0.0075f;
                _vrRay.alignment = LineAlignment.View;

                // Get material from LCVR
                LineRenderer lcvrLR = LCVRUtil.getVRControllerRay();
                if (lcvrLR is not null) {
                    _vrRay.material = lcvrLR.material;
                }
            }
        }

        private void OnDisable() {
            InputUtil.SCREEN_SCRIPT = null;
        }

        private void LateUpdate() { // Moved from Update to LateUpdate due to LCVR
            PlayerControllerB ply = LOCAL_PLAYER;
            if (Plugin.IsActive && IsLookingAtMonitor(out Bounds bounds, out Ray lookRay, out Ray camRay)) {
                if (InputUtil.inVR) {
                    _vrRay?.SetPositions(new[] {lookRay.origin, lookRay.GetPoint(ply.grabDistance)});
                    LCVRUtil.UpdateInteractCanvas(lookRay.GetPoint(ply.grabDistance / 2));
                    if (ConfigUtil.CONFIG_SHOW_TOOLTIP.Value) { // Display Tooltips - Is overridden every update by LCVR https://github.com/DaXcess/LCVR/blob/eb568caab38abb1b9dbacb56eaa543059bcb05c3/Source/Player/VRController.cs#L319
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

                if (!_lookingAtMonitor) {
                    _lookingAtMonitor = true;
                    ply.isGrabbingObjectAnimation = true; // Blocks the default code from overwriting it again
                    if (ConfigUtil.CONFIG_SHOW_POINTER.Value && !InputUtil.inVR) { // Display Pointer
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