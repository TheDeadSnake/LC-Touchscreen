using BepInEx;
using UnityEngine;
using com.github.zehsteam.ToilHead.MonoBehaviours;

namespace touchscreen;

public static class ToilHeadUtil {
    public static bool IsEnabled {get; private set;}

    public static void Setup() {
        ToilHeadUtil.IsEnabled = true;
    }

    public static bool CallFollowTerminalAccessibleObject(Collider collider) {
        FollowTerminalAccessibleObjectBehaviour tObject = collider.GetComponent<ScanNodeProperties>()?.GetComponentInParent<FollowTerminalAccessibleObjectBehaviour>(false);
        if (tObject) {
            tObject.CallFunctionFromTerminal();
            return true;
        }
        return false;
    }

}