using BepInEx;
using UnityEngine;

namespace touchscreen;

public static class LCVRUtil {

    internal static bool Setup(PluginInfo info) {
        if (LCVR.Player.VRSession.InVR) {
            InputUtil.LOOK_RAY = ply => {
                Transform t = LCVR.Player.VRSession.Instance.LocalPlayer?.PrimaryController.InteractOrigin;
                if (t) {
                    return new Ray(t.position, t.forward);
                }
                Plugin.LOGGER.LogWarning(" > Failed to get primary VRController.");
                return new Ray(ply.gameplayCamera.transform.position, ply.gameplayCamera.transform.forward);
            };
            Plugin.LOGGER.LogInfo($" > Hooked into LethalCompanyVR {info.Metadata.Version}");
            return true;
        } else {
            return false;
        }
    }

    public static LineRenderer getVRControllerRay() {
        return LCVR.Player.VRSession.Instance.LocalPlayer?.PrimaryController.GetComponent<LineRenderer>();
    }

    public static void UpdateInteractCanvas(Vector3 position) {
        LCVR.Player.VRSession.Instance.HUD.UpdateInteractCanvasPosition(position);
    }

}