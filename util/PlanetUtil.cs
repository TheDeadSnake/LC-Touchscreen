using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace touchscreen;

public static class PlanetUtil {
    private static Plugin.Func<bool, string> _onPlanetCheck = _ => false;

    public static bool IsPlanet(Scene scene) {
        // Make sure players are in-game
        if (StartOfRound.Instance) {
            // Check for LE and LEC
            if (_onPlanetCheck.Invoke(scene.name))
                return true;

            // Check for base levels and LLL
            foreach(SelectableLevel x in StartOfRound.Instance.levels) {
                if (scene.name.Equals(x.sceneName))
                    return true;
            }
        }
        return false;
    }

    public static void checkPlugins() {
        Plugin.Supplier<bool> _lec = () => !LethalExpansionCore.LethalExpansion.Settings.UseOriginalLethalExpansion.Value;
        if (Chainloader.PluginInfos.TryGetValue("com.github.lethalmods.lethalexpansioncore", out PluginInfo lec) && _lec.Invoke()) {
            _onPlanetCheck = x => x.Equals("InitSceneLaunchOptions") && LethalExpansionCore.LethalExpansion.isInGame;
            Plugin.LOGGER.LogInfo($" > Hooked into LethalExpansionCore {lec.Metadata.Version}");
        } else if (Chainloader.PluginInfos.TryGetValue("LethalExpansion", out PluginInfo le)) {
            _onPlanetCheck = x => x.Equals("InitSceneLaunchOptions") && LethalExpansion.LethalExpansion.isInGame;
            Plugin.LOGGER.LogInfo($" > Hooked into LethalExpansion {le.Metadata.Version}");
        }
    }

}