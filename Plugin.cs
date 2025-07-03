using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace touchscreen;

[BepInPlugin("me.pm.TheDeadSnake", "TouchScreen", "1.1.5")]
[BepInProcess("Lethal Company.exe")]
[BepInDependency("ShaosilGaming.GeneralImprovements", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency("com.rune580.LethalCompanyInputUtils", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency("io.daxcess.lcvr", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency("com.github.zehsteam.ToilHead", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency("ScienceBird.UniversalRadar", BepInDependency.DependencyFlags.SoftDependency)]
public class Plugin : BaseUnityPlugin {
    internal static ManualLogSource LOGGER;
    internal delegate R Func<R, T>(T value);
    internal delegate R Supplier<R>();
    internal delegate void Consumer<T>(ref T value);

    // GeneralImprovements - support
    internal static Func<Bounds, GameObject> CREATE_BOUNDS;
    internal static Consumer<Vector3> ORBITAL_OFFSET;

    // 3rd party plugin support (to disable/enable this plugin)
    private static bool _override = true;
    private static bool _onPlanet;
    public static bool IsActive {
        get => _onPlanet && (_override || ConfigUtil.IGNORE_OVERRIDE);
        set {
            if (_override != value) {
                _override = value;
                MethodBase prevFrame = (new StackTrace()).GetFrame(1).GetMethod();
                LOGGER.LogInfo(String.Format("Touchscreen was {0} by {1}.{2}.{3}",
                    value ? "enabled" : "disabled",
                    prevFrame.ReflectedType.Namespace,
                    prevFrame.ReflectedType.Name,
                    prevFrame.Name
                ));
            }
        }
    }

    /*
        Enable / Disable Touchscreen when not on a planet
    */
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
        if (PlanetUtil.IsPlanet(scene)) {
            GameObject obj = StartOfRound.Instance?.mapScreen?.mesh.gameObject;
            if (obj != null && obj.GetComponent<ScreenScript>() == null) {
                // Debug
                // obj.AddComponent<ScreenScript>().dbg(false); 
                
                obj.AddComponent<ScreenScript>();
            }
            _onPlanet = true;
        }
    }

    private void OnSceneUnloaded(Scene scene) {
        if (_onPlanet)
            _onPlanet = false;
    }

    private void NOffset(ref Vector3 pos) { }

    private void GIOffset(ref Vector3 pos) {
        pos.x += 2.5f;
    }
    
    // Plugin Startup
    private void Awake() {
        LOGGER = this.Logger;
        string pluginFolder = Path.Combine(Paths.PluginPath, "TheDeadSnake-Touchscreen");
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;

        // Load config values
        ConfigUtil.Setup(Config, pluginFolder);
        InputUtil.Setup();

        // GeneralImprovements support
        Supplier<bool> _gi = () => GeneralImprovements.Plugin.UseBetterMonitors.Value;
        if (Chainloader.PluginInfos.TryGetValue("ShaosilGaming.GeneralImprovements", out PluginInfo gi) && _gi.Invoke()) {
            CREATE_BOUNDS = x => new Bounds(
                new Vector3(
                    x.transform.position.x + -.2f,
                    x.transform.position.y + -.05f,
                    x.transform.position.z + .03f
                ),
                new Vector3(0, 1.05f, 1.36f)
            );
            ORBITAL_OFFSET = GIOffset;
            LOGGER.LogInfo($" > Hooked into GeneralImprovements {gi.Metadata.Version}");
        } else {
            CREATE_BOUNDS = x => new Bounds(
                new Vector3(
                    x.transform.position.x + .06f,
                    x.transform.position.y + -.05f,
                    x.transform.position.z + .84f
                ),
                new Vector3(0, 1.05f, 1.36f)
            );
            ORBITAL_OFFSET = NOffset;
        }

        // ToilHead support
        if (Chainloader.PluginInfos.TryGetValue("com.github.zehsteam.ToilHead", out PluginInfo ti)) {
            ToilHeadUtil.Setup();
            LOGGER.LogInfo($" > Hooked into ToilHead {ti.Metadata.Version}");
        }

        // UniversalRadar support
        if (Chainloader.PluginInfos.TryGetValue("ScienceBird.UniversalRadar", out PluginInfo ur)) {
            // Map Camera is higher ==> Ray cast needs to be longer
            ScreenScript.GROUND_DISTANCE = 30;
            LOGGER.LogInfo($" > Hooked into UniversalRadar {ur.Metadata.Version}");
        }
        
        LOGGER.LogInfo("Enabled TouchScreen");
    }

}
