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

[BepInPlugin("me.pm.TheDeadSnake", "TouchScreen", "1.0.10")]
[BepInProcess("Lethal Company.exe")]
[BepInDependency("LethalExpansion", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency("com.github.lethalmods.lethalexpansioncore", BepInDependency.DependencyFlags.SoftDependency)]
public class Plugin : BaseUnityPlugin
{
    internal static ManualLogSource LOGGER;
    public static Sprite HOVER_ICON { get; private set; }
    public static ConfigEntry<string> CONFIG_PRIMARY { get; private set; }
    public static ConfigEntry<string> CONFIG_SECONDARY { get; private set; }
    public static ConfigEntry<string> CONFIG_QUICK_SWITCH { get; private set; }
    public static ConfigEntry<string> CONFIG_ALT_QUICK_SWITCH { get; private set; }
    public static ConfigEntry<bool> CONFIG_ALT_REVERSE { get; private set; }
    public static ConfigEntry<bool> CONFIG_SHOW_POINTER { get; private set; }
    public static ConfigEntry<bool> CONFIG_SHOW_TOOLTIP { get; private set; }
    private static bool _config_ignore_override = false;

    private static bool _override = true;
    private static bool _onPlanet = false;
    public static bool IsActive {
        get => _onPlanet && (_override || _config_ignore_override);
        set {
            if (_override != value) {
                _override = value;
                MethodBase prevFrame = (new StackTrace()).GetFrame(1).GetMethod();
                Plugin.LOGGER.LogInfo(String.Format("Touchscreen was {0} by {1}.{2}.{3}",
                    value ? "enabled" : "disabled",
                    prevFrame.ReflectedType.Namespace,
                    prevFrame.ReflectedType.Name,
                    prevFrame.Name
                ));
            }
        }
    }
    internal delegate R Supplier<R, T>(T value);
    private static Supplier<bool, string> _onPlanetCheck = _ => false;


    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
        if (scene.name.StartsWith("level", StringComparison.OrdinalIgnoreCase) || scene.name.Equals("companybuilding", StringComparison.OrdinalIgnoreCase) || _onPlanetCheck.Invoke(scene.name)) {
            GameObject obj = StartOfRound.Instance?.mapScreen?.mesh.gameObject;
            if (obj != null && obj.GetComponent<ScreenScript>() == null) {
                obj.AddComponent<ScreenScript>();
            }
            _onPlanet = true;
        }
    }

    private void OnSceneUnloaded(Scene scene) {
        if (_onPlanet)
            _onPlanet = false;
    }

    private void Awake() {
        Plugin.LOGGER = this.Logger;
        string pluginFolder = Path.Combine(Paths.PluginPath, "TheDeadSnake-Touchscreen");
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;

        // Load config values
            // Keybinds
        Plugin.CONFIG_PRIMARY = this.Config.Bind(
            "Layout", "Primary",
            "<Keyboard>/e",
            """
            Name of the key mapping for the primary (switch, ping, trigger) actions
            Allowed value format: "<Keyboard>/KEY", "<Mouse>/BUTTON", "<Gamepad>/BUTTON"
            Examples: "<Keyboard>/f" "<Mouse>/leftButton" "<Gamepad>/buttonNorth"
            For in depth instructions see: https://docs.unity3d.com/Packages/com.unity.inputsystem@1.0/api/UnityEngine.InputSystem.InputControlPath.html
            """
        );
        Plugin.CONFIG_SECONDARY = this.Config.Bind(
            "Layout", "Secondary",
            "<Mouse>/leftButton",
            """
            Name of the key mapping for the secondary (Flash) action
            Allowed value format: "<Keyboard>/KEY", "<Mouse>/BUTTON", "<Gamepad>/BUTTON"
            Examples: "<Keyboard>/g" "<Mouse>/rightButton" "<Gamepad>/buttonWest"
            For in depth instructions see: https://docs.unity3d.com/Packages/com.unity.inputsystem@1.0/api/UnityEngine.InputSystem.InputControlPath.html
            """
        );
        Plugin.CONFIG_QUICK_SWITCH = this.Config.Bind(
            "Layout", "Switch",
            "",
            """
            Name of the key mapping for the quick switch action
            Allowed value format: "<Keyboard>/KEY", "<Mouse>/BUTTON", "<Gamepad>/BUTTON"
            Examples: "<Keyboard>/g" "<Mouse>/rightButton" "<Gamepad>/buttonWest"
            For in depth instructions see: https://docs.unity3d.com/Packages/com.unity.inputsystem@1.0/api/UnityEngine.InputSystem.InputControlPath.html
            """
        );
        Plugin.CONFIG_ALT_REVERSE = this.Config.Bind(
            "Layout", "ReverseSwitch",
            true,
            """
            Decides what the "SwitchAlternative" does when pressed
            true: When the alternative key is pressed, the quick switch will go through the reverse order
            false: When the alternative key is pressed the previous radar target will be selected
            """
        );
        Plugin.CONFIG_ALT_QUICK_SWITCH = this.Config.Bind(
            "Layout", "SwitchAlternative",
            "",
            """
            Name of the key mapping for the alternative quick switch action
            The behaviour of the key is dependent on the "ReverseSwitch" option
            Allowed value format: "<Keyboard>/KEY", "<Mouse>/BUTTON", "<Gamepad>/BUTTON"
            Examples: "<Keyboard>/g" "<Mouse>/rightButton" "<Gamepad>/buttonWest"
            For in depth instructions see: https://docs.unity3d.com/Packages/com.unity.inputsystem@1.0/api/UnityEngine.InputSystem.InputControlPath.html
            """
        );

        // Image
        _config_ignore_override = this.Config.Bind(
            "Features", "IgnoreOverride",
            false,
            """
            Set if other plugins can disable / enable the Touchscreen feature.
             > "true": Other plugins can no longer toggle it
             > "false": Other plugins may disable / enable it
            """
        ).Value;
        ConfigEntry<string> imagePath = this.Config.Bind(
            "UI", "PointerIcon",
            "HoverIcon.png",
            String.Format("""
            Accepts a file name relative to the plugin name or a full system path
            You can either choose one of the three default icons "HoverIcon.png", "CrossIcon.png", "DotIcon.png" or
            create your own (Only .png and .jpg are supported) and place it in: {0}
            Examples: "HoverIcon.png" or "X:\Images\SomeImage.png"
            """, pluginFolder)
        );
        Plugin.CONFIG_SHOW_POINTER = this.Config.Bind(
            "UI", "ShowPointer",
            true,
            String.Format("""
            Enable / Disable the pointer when hovering over the monitor
            """, pluginFolder)
        );
        Plugin.CONFIG_SHOW_TOOLTIP = this.Config.Bind(
            "UI", "ShowTooltip",
            true,
            String.Format("""
            Enable / Disable the keybind tooltip when hovering over the monitor
            """, pluginFolder)
        );

        // Try to resolve imagePath to full path
        string iconPath;
        if (imagePath.Value.EndsWith(".png", StringComparison.OrdinalIgnoreCase) || imagePath.Value.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)) {
            // Check if provided imagePath is relative file name or full path
            iconPath = (Path.GetFileName(imagePath.Value) == imagePath.Value) ?
                Path.Combine(pluginFolder, imagePath.Value) :
                imagePath.Value;
        } else {
            LOGGER.LogWarning("The provided icon file extension is not supported. Please make sure it's either a .png or .jpg file. Trying to use default icon...");
            iconPath = Path.Combine(pluginFolder, imagePath.DefaultValue.ToString());
        }

        // Load hover icon
        if (File.Exists(iconPath)) {
            UnityWebRequest req = UnityWebRequestTexture.GetTexture(Utility.ConvertToWWWFormat(iconPath));
            req.SendWebRequest().completed += _ => {
                Texture2D tex = DownloadHandlerTexture.GetContent(req);
                Plugin.HOVER_ICON = Sprite.Create(
                    tex,
                    new Rect(0f, 0f, tex.width, tex.height),
                    new Vector2(.5f, .5f),
                    100f
                );
            };
        } else
            LOGGER.LogWarning(" > Unable to locate hover icon at provided path: " + iconPath);

        // Lethal Expansion support
        if (Chainloader.PluginInfos.TryGetValue("com.github.lethalmods.lethalexpansioncore", out PluginInfo lec) && !LethalExpansionCore.LethalExpansion.Settings.UseOriginalLethalExpansion.Value) {
            _onPlanetCheck = x => x.Equals("InitSceneLaunchOptions") && LethalExpansionCore.LethalExpansion.isInGame;
            LOGGER.LogInfo($" > Hooked into LethalExpansionCore {lec.Metadata.Version}");
        } else if (Chainloader.PluginInfos.TryGetValue("LethalExpansion", out PluginInfo le)) {
            _onPlanetCheck = x => x.Equals("InitSceneLaunchOptions") && LethalExpansion.LethalExpansion.isInGame;
            LOGGER.LogInfo($" > Hooked into LethalExpansion {le.Metadata.Version}");
        }

        LOGGER.LogInfo("Enabled TouchScreen");
    }

}
