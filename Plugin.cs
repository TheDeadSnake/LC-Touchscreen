using System.IO;
using BepInEx;
using BepInEx.Logging;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace touchscreen;

[BepInPlugin("me.pm.TheDeadSnake", "TouchScreen", "1.0.1")]
public class Plugin : BaseUnityPlugin
{
    internal static ManualLogSource LOGGER;
    internal static Sprite hoverIcon;

    public void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
        // Level1Experimentation
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

        // Load hover icon
        string path = Path.Combine(Paths.PluginPath, "TheDeadSnake-Touchscreen", "HoverIcon.png");
        if (File.Exists(path))
        {
            UnityWebRequest req = UnityWebRequestTexture.GetTexture(path);
            req.SendWebRequest().completed += _ => {
                Texture2D tex = DownloadHandlerTexture.GetContent(req);
                Plugin.hoverIcon = Sprite.Create(
                    tex,
                    new Rect(0f, 0f, tex.width, tex.height),
                    new Vector2(.5f, .5f),
                    100f
                );
            };
        }
        else
            LOGGER.LogWarning(" > Unable to locate hover icon at path: " + path);
        LOGGER.LogInfo("Enabled TouchScreen");
    }

}
