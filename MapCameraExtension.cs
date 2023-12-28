using GameNetcodeStuff;

namespace touchscreen {
    internal static class MapCameraExtension {

        public static int GetPreviousValidRadarTarget(this ManualCameraRenderer renderer) {
            TransformAndName t = null;
            for (int i = renderer.targetTransformIndex > 0 ? (renderer.targetTransformIndex - 1) : (renderer.radarTargets.Count - 1); i != renderer.targetTransformIndex; i--) {
                if (i < 0)
                    i = renderer.radarTargets.Count - 1;
                t = renderer.radarTargets[i];
                if (t?.transform.gameObject.activeSelf == true && (t.isNonPlayer || (t.transform.gameObject.GetComponent<PlayerControllerB>()?.isPlayerControlled == true)))
                    return i;
            }
            return renderer.targetTransformIndex;
        }

        public static void SwitchRadarTargetBackwards(this ManualCameraRenderer renderer, bool callRPC) {
            if (renderer.updateMapCameraCoroutine != null)
                renderer.StopCoroutine(renderer.updateMapCameraCoroutine);
            renderer.updateMapCameraCoroutine = renderer.StartCoroutine(
                renderer.updateMapTarget(renderer.GetPreviousValidRadarTarget(), !callRPC));
        }

    }
}
