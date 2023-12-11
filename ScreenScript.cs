using System;
using System.Collections.Generic;
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

        private Bounds GetBounds() {
            // Magic values are the offset from the monitor object center
            return new Bounds(
                new Vector3(
                    this.gameObject.transform.position.x + .06f,
                    this.gameObject.transform.position.y + -.05f,
                    this.gameObject.transform.position.z + .84f
                ),
                new Vector3(0, 1.05f, 1.36f)
            );
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

        private void OnPlayerInteraction(bool isAlt) {
            PlayerControllerB ply = LOCAL_PLAYER;
            if (ply != null && IsLookingAtMonitor(out Bounds bounds, out Ray lookRay, out Ray camRay)) {
                foreach (Collider x in Physics.OverlapCapsule(camRay.GetPoint(0), camRay.GetPoint(10), _isCloseMax)) {
                    if (!isAlt && x.GetComponent<TerminalAccessibleObject>() is TerminalAccessibleObject tObject) { // Clicked on BigDoor, Land mine, Turret
                        tObject.CallFunctionFromTerminal();
                        return;
                    } else if (x.GetComponent<RadarBoosterItem>() is RadarBoosterItem rItem) { // Clicked on Radar booster
                        if (isAlt)
                            rItem.FlashAndSync(); // New Radar boost function (v45+)
                        else
                            rItem.PlayPingAudioAndSync();
                        return;
                    } else if (!isAlt && x.GetComponent<PlayerControllerB>() is PlayerControllerB tgtPlayer) { // Clicked on player
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

        private void OnEnable()
        {
            PlayerControllerB ply = LOCAL_PLAYER;
            if (ply != null) {
                foreach (InputAction x in LOCAL_PLAYER.playerActions) {
                    if (x.name.Equals("Interact"))
                        x.performed += _ => OnPlayerInteraction(false);
                    else if (x.name.Equals("Use"))
                        x.performed += _ => OnPlayerInteraction(true);
                }
            } else {
                Plugin.LOGGER.LogWarning("Unable to activate monitor touchscreen. Reason: Failed to get local player.");
            }
        }

        private void Update() {
            PlayerControllerB ply = LOCAL_PLAYER;
            if (IsLookingAtMonitor(out Bounds bounds, out Ray lookRay, out Ray camRay)) {
                if (!_lookingAtMonitor) {
                    _lookingAtMonitor = true;
                    ply.isGrabbingObjectAnimation = true; // Blocks the default code from overwriting it again
                    ply.cursorIcon.enabled = true;
                    ply.cursorIcon.sprite = Plugin.hoverIcon;
                    if (!StartOfRound.Instance.localPlayerUsingController) {
                        ply.cursorTip.text = """
                        [E] Interact
                        [LΜB] Flash (Radar)
                        """; // Used 'Greek Capital Letter Mu' for M as otherwise "[LMB]" will be replaced with "[E]"
                    } else {
                        if (Gamepad.current is DualShockGamepad) {
                            ply.cursorTip.text = """
                            [□] Interact
                            [R2] Flash (Radar)
                            """;
                        } else {
                            ply.cursorTip.text = """
                            [X] Interact
                            [R-Trigger] Flash (Radar)
                            """;
                        }
                    }
                }
            } else if (ply != null && _lookingAtMonitor) {
                ply.isGrabbingObjectAnimation = false;
                _lookingAtMonitor = false;
            }

        }

    }

}