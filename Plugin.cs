using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace touchscreen;

[BepInPlugin("me.pm.TheDeadSnake", "TouchScreen", "1.0.6")]
[BepInProcess("Lethal Company.exe")]
public class Plugin : BaseUnityPlugin
{
    internal static ManualLogSource LOGGER;
    public static Sprite HOVER_ICON { get; private set; }
    public static ConfigEntry<string> CONFIG_PRIMARY { get; private set; }
    public static ConfigEntry<string> CONFIG_SECONDARY { get; private set; }
    public static ConfigEntry<string> CONFIG_QUICK_SWITCH { get; private set; }
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

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
        if (scene.name.StartsWith("level", StringComparison.OrdinalIgnoreCase) || scene.name.Equals("companybuilding", StringComparison.OrdinalIgnoreCase)) {
            GameObject obj = StartOfRound.Instance?.mapScreen?.mesh.gameObject;
            if (obj != null && obj.GetComponent<ScreenScript>() == null) {
                obj.AddComponent<ScreenScript>();
            }
            _onPlanet = true;
        }
    }

    private void OnSceneUnloaded(Scene scene) {
        if (scene.name.StartsWith("level", StringComparison.OrdinalIgnoreCase) || scene.name.Equals("companybuilding", StringComparison.OrdinalIgnoreCase))
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

        // Register Terminal node
        //HUDManager.Instance.terminalScript.terminalNodes.allKeywords.
        LOGGER.LogInfo("Enabled TouchScreen");
    }

}
