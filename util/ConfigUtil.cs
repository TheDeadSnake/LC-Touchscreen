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

public static class ConfigUtil {
    public static Sprite HOVER_ICON { get; private set; }
    public static ConfigEntry<string> CONFIG_PRIMARY { get; private set; }
    public static ConfigEntry<string> CONFIG_SECONDARY { get; private set; }
    public static ConfigEntry<string> CONFIG_QUICK_SWITCH { get; private set; }
    public static ConfigEntry<string> CONFIG_ALT_QUICK_SWITCH { get; private set; }
    public static ConfigEntry<bool> CONFIG_ALT_REVERSE { get; private set; }
    public static ConfigEntry<bool> CONFIG_SHOW_POINTER { get; private set; }
    public static ConfigEntry<bool> CONFIG_SHOW_TOOLTIP { get; private set; }
    private static bool _config_ignore_override = false;
    public static bool IGNORE_OVERRIDE {
        get => _config_ignore_override;
    }

    internal static void Setup(ConfigFile config, string pluginFolder) {
        // Keybinds
        ConfigUtil.CONFIG_PRIMARY = config.Bind(
            "Layout", "Primary",
            "<Keyboard>/e",
            """
            Name of the key mapping for the primary (switch, ping, trigger) actions
            Allowed value format: "<Keyboard>/KEY", "<Mouse>/BUTTON", "<Gamepad>/BUTTON"
            Examples: "<Keyboard>/f" "<Mouse>/leftButton" "<Gamepad>/buttonNorth"
            For in depth instructions see: https://docs.unity3d.com/Packages/com.unity.inputsystem@1.0/api/UnityEngine.InputSystem.InputControlPath.html
            """
        );
        ConfigUtil.CONFIG_SECONDARY = config.Bind(
            "Layout", "Secondary",
            "<Mouse>/leftButton",
            """
            Name of the key mapping for the secondary (Flash) action
            Allowed value format: "<Keyboard>/KEY", "<Mouse>/BUTTON", "<Gamepad>/BUTTON"
            Examples: "<Keyboard>/g" "<Mouse>/rightButton" "<Gamepad>/buttonWest"
            For in depth instructions see: https://docs.unity3d.com/Packages/com.unity.inputsystem@1.0/api/UnityEngine.InputSystem.InputControlPath.html
            """
        );
        ConfigUtil.CONFIG_QUICK_SWITCH = config.Bind(
            "Layout", "Switch",
            "",
            """
            Name of the key mapping for the quick switch action
            Allowed value format: "<Keyboard>/KEY", "<Mouse>/BUTTON", "<Gamepad>/BUTTON"
            Examples: "<Keyboard>/g" "<Mouse>/rightButton" "<Gamepad>/buttonWest"
            For in depth instructions see: https://docs.unity3d.com/Packages/com.unity.inputsystem@1.0/api/UnityEngine.InputSystem.InputControlPath.html
            """
        );
        ConfigUtil.CONFIG_ALT_REVERSE = config.Bind(
            "Layout", "ReverseSwitch",
            true,
            """
            Decides what the "SwitchAlternative" does when pressed
            true: When the alternative key is pressed, the quick switch will go through the reverse order
            false: When the alternative key is pressed the previous radar target will be selected
            """
        );
        ConfigUtil.CONFIG_ALT_QUICK_SWITCH = config.Bind(
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
        ConfigUtil._config_ignore_override = config.Bind(
            "Features", "IgnoreOverride",
            false,
            """
            Set if other plugins can disable / enable the Touchscreen feature.
             > "true": Other plugins can no longer toggle it
             > "false": Other plugins may disable / enable it
            """
        ).Value;
        ConfigEntry<string> imagePath = config.Bind(
            "UI", "PointerIcon",
            "HoverIcon.png",
            String.Format("""
            Accepts a file name relative to the plugin name or a full system path
            You can either choose one of the three default icons "HoverIcon.png", "CrossIcon.png", "DotIcon.png" or
            create your own (Only .png and .jpg are supported) and place it in: {0}
            Examples: "HoverIcon.png" or "X:\Images\SomeImage.png"
            """, pluginFolder)
        );
        ConfigUtil.CONFIG_SHOW_POINTER = config.Bind(
            "UI", "ShowPointer",
            true,
            String.Format("""
            Enable / Disable the pointer when hovering over the monitor
            """, pluginFolder)
        );
        ConfigUtil.CONFIG_SHOW_TOOLTIP = config.Bind(
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
            Plugin.LOGGER.LogWarning("The provided icon file extension is not supported. Please make sure it's either a .png or .jpg file. Trying to use default icon...");
            iconPath = Path.Combine(pluginFolder, imagePath.DefaultValue.ToString());
        }

        // Load hover icon
        if (File.Exists(iconPath)) {
            UnityWebRequest req = UnityWebRequestTexture.GetTexture(Utility.ConvertToWWWFormat(iconPath));
            req.SendWebRequest().completed += _ => {
                Texture2D tex = DownloadHandlerTexture.GetContent(req);
                ConfigUtil.HOVER_ICON = Sprite.Create(
                    tex,
                    new Rect(0f, 0f, tex.width, tex.height),
                    new Vector2(.5f, .5f),
                    100f
                );
            };
        } else
            Plugin.LOGGER.LogWarning(" > Unable to locate hover icon at provided path: " + iconPath);
    }


}