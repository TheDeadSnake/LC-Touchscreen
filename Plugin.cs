using System.IO;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace touchscreen;

[BepInPlugin("me.pm.TheDeadSnake", "TouchScreen", "1.0.4")]
[BepInProcess("Lethal Company.exe")]
public class Plugin : BaseUnityPlugin
{
    internal static ManualLogSource LOGGER;
    public static Sprite HOVER_ICON { get; private set; }
    public static ConfigEntry<string> CONFIG_PRIMARY { get; private set; }
    public static ConfigEntry<string> CONFIG_SECONDARY { get; private set; }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
        if (scene.name.StartsWith("level", System.StringComparison.OrdinalIgnoreCase)) {
            GameObject obj = StartOfRound.Instance?.mapScreen?.mesh.gameObject;
            if (obj != null && obj.GetComponent<ScreenScript>() == null) {
                obj.AddComponent<ScreenScript>();
            }
        }
    }

    private void Awake() {
        Plugin.LOGGER = this.Logger;
        SceneManager.sceneLoaded += OnSceneLoaded;

        // Load config values
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

        // Load hover icon
        string path = Path.Combine(Paths.PluginPath, "TheDeadSnake-Touchscreen", "HoverIcon.png"); // Only .png and .jpg are supported
        if (File.Exists(path)) {
            UnityWebRequest req = UnityWebRequestTexture.GetTexture(Utility.ConvertToWWWFormat(path));
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
            LOGGER.LogWarning(" > Unable to locate hover icon at path: " + path);
        LOGGER.LogInfo("Enabled TouchScreen");
    }

}
