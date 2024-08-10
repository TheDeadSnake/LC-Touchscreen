using BepInEx;
using BepInEx.Bootstrap;
using UnityEngine.SceneManagement;


namespace touchscreen;

public static class PlanetUtil {

    public static bool IsPlanet(Scene scene) {
        // Make sure players are in-game
        if (StartOfRound.Instance) {
            // Check for base levels and LLL
            foreach(SelectableLevel x in StartOfRound.Instance.levels) {
                if (scene.name.Equals(x.sceneName))
                    return true;
            }
        }
        return false;
    }

}