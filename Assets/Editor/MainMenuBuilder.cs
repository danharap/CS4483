using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// CS4483 → 5 - Build Main Menu Scene
/// Creates Assets/Scenes/MainMenu.unity with title, New Game / Load Game, lore, best run, HighScoreManager.
/// Also invoked automatically from SetupAll (SETUP EVERYTHING).
/// </summary>
public static class MainMenuBuilder
{
    public const string MainMenuPath = "Assets/Scenes/MainMenu.unity";
    public const string MainScenePath = "Assets/Scenes/MainScene.unity";

    [MenuItem("CS4483/5 - Build Main Menu Scene")]
    public static void BuildMainMenu()
    {
        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        EnsureScenesFolder();
        Scene menuScene = CreateAndPopulateMainMenuScene();
        EditorSceneManager.SaveScene(menuScene, MainMenuPath);
        ApplyBuildAndPlayModeSettings();
        Debug.Log($"[MainMenuBuilder] ✓ MainMenu scene saved to {MainMenuPath}");
    }

    /// <summary>
    /// Used by SetupAll after MainScene is saved: rebuild MainMenu, then return the editor to MainScene.
    /// </summary>
    public static void BuildMainMenuAndReturnTo(string returnScenePath)
    {
        EnsureScenesFolder();
        Scene menuScene = CreateAndPopulateMainMenuScene();
        EditorSceneManager.SaveScene(menuScene, MainMenuPath);
        ApplyBuildAndPlayModeSettings();
        Debug.Log($"[MainMenuBuilder] ✓ MainMenu scene saved to {MainMenuPath} (from SETUP EVERYTHING)");

        if (!string.IsNullOrEmpty(returnScenePath) && File.Exists(returnScenePath))
        {
            EditorSceneManager.OpenScene(returnScenePath, OpenSceneMode.Single);
            Debug.Log($"[MainMenuBuilder] Re-opened {returnScenePath}");
        }
    }

    /// <summary>
    /// MainMenu first in Build Settings + Play Mode Start Scene = MainMenu so pressing Play opens the menu
    /// even when the editor has MainScene open (Unity 2022.2+).
    /// </summary>
    public static void ApplyBuildAndPlayModeSettings()
    {
        var list = new List<EditorBuildSettingsScene>();
        if (File.Exists(MainMenuPath))
            list.Add(new EditorBuildSettingsScene(MainMenuPath, true));
        if (File.Exists(MainScenePath))
            list.Add(new EditorBuildSettingsScene(MainScenePath, true));
        if (list.Count > 0)
            EditorBuildSettings.scenes = list.ToArray();

        SceneAsset menuAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(MainMenuPath);
        if (menuAsset != null)
            EditorSceneManager.playModeStartScene = menuAsset;
    }

    static void EnsureScenesFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            AssetDatabase.CreateFolder("Assets", "Scenes");
    }

    static Scene CreateAndPopulateMainMenuScene()
    {
        Scene menuScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        SetupMenuCamera();
        SetupMenuLighting();
        SetupBackgroundProps();
        SetupMenuCanvas();
        SetupEventSystem();
        SetupHighScoreManager();
        return menuScene;
    }

    static void SetupEventSystem()
    {
        GameObject es = new GameObject("EventSystem");
        es.AddComponent<EventSystem>();
        es.AddComponent<StandaloneInputModule>();
    }

    // ── Camera ────────────────────────────────────────────────────────────

    static void SetupMenuCamera()
    {
        GameObject camGO = new GameObject("Main Camera");
        camGO.tag = "MainCamera";
        Camera cam = camGO.AddComponent<Camera>();
        if (camGO.GetComponent<AudioListener>() == null)
            camGO.AddComponent<AudioListener>();
        cam.transform.position = new Vector3(0f, 8f, -12f);
        cam.transform.rotation = Quaternion.Euler(30f, 0f, 0f);
        cam.backgroundColor    = new Color(0.03f, 0.02f, 0.04f);
        cam.clearFlags         = CameraClearFlags.SolidColor;
        cam.fieldOfView        = 55f;
    }

    // ── Lighting ──────────────────────────────────────────────────────────

    static void SetupMenuLighting()
    {
        RenderSettings.ambientLight = new Color(0.05f, 0.04f, 0.06f);
        RenderSettings.fog          = true;
        RenderSettings.fogColor     = new Color(0.04f, 0.03f, 0.05f);
        RenderSettings.fogMode      = FogMode.Linear;
        RenderSettings.fogStartDistance = 15f;
        RenderSettings.fogEndDistance   = 40f;

        // Slow pulsing blue key light
        GameObject lightGO = new GameObject("KeyLight");
        Light l = lightGO.AddComponent<Light>();
        l.type      = LightType.Directional;
        l.color     = new Color(0.3f, 0.4f, 0.8f);
        l.intensity = 0.6f;
        lightGO.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        // Atmospheric point light (red glow from below — suggests danger)
        GameObject redLight = new GameObject("DangerGlow");
        redLight.transform.position = new Vector3(0f, -1f, 2f);
        Light rl = redLight.AddComponent<Light>();
        rl.type      = LightType.Point;
        rl.color     = new Color(0.7f, 0.1f, 0.1f);
        rl.intensity = 1.5f;
        rl.range     = 15f;
    }

    // ── Background Props (atmospheric arena silhouette) ────────────────────

    static void SetupBackgroundProps()
    {
        GameObject props = new GameObject("BackgroundProps");

        // Dark arena floor
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "MenuFloor";
        floor.transform.SetParent(props.transform);
        floor.transform.position   = new Vector3(0f, -0.5f, 5f);
        floor.transform.localScale = new Vector3(40f, 0.4f, 30f);
        SetColor(floor, new Color(0.06f, 0.05f, 0.07f));

        // Silhouette pillars in background
        (Vector3 pos, Vector3 scale)[] pillars = {
            (new Vector3(-8f, 4f, 8f),   new Vector3(0.8f, 8f, 0.8f)),
            (new Vector3( 8f, 4f, 8f),   new Vector3(0.8f, 8f, 0.8f)),
            (new Vector3(-14f, 3f, 10f), new Vector3(0.6f, 6f, 0.6f)),
            (new Vector3( 14f, 3f, 10f), new Vector3(0.6f, 6f, 0.6f)),
            (new Vector3(0f,   5f, 12f), new Vector3(1.0f, 10f, 1.0f)),
        };
        foreach (var (pos, scale) in pillars)
        {
            GameObject p = GameObject.CreatePrimitive(PrimitiveType.Cube);
            p.transform.SetParent(props.transform);
            p.transform.position   = pos;
            p.transform.localScale = scale;
            SetColor(p, new Color(0.08f, 0.07f, 0.10f));
        }

        // Faint point lights on silhouette pillars
        Color[] lightColors = {
            new Color(0.2f, 0.3f, 0.9f),
            new Color(0.2f, 0.3f, 0.9f),
            new Color(0.1f, 0.6f, 0.2f),
            new Color(0.8f, 0.7f, 0.1f),
            new Color(0.9f, 0.2f, 0.1f),
        };
        for (int i = 0; i < pillars.Length; i++)
        {
            GameObject lg = new GameObject($"PillarGlow_{i}");
            lg.transform.SetParent(props.transform);
            lg.transform.position = pillars[i].pos + Vector3.up * 4.5f;
            Light l = lg.AddComponent<Light>();
            l.type      = LightType.Point;
            l.color     = lightColors[i % lightColors.Length];
            l.intensity = 1.2f;
            l.range     = 6f;
        }
    }

    // ── Main Menu Canvas ──────────────────────────────────────────────────

    static void SetupMenuCanvas()
    {
        GameObject canvasGO = new GameObject("Canvas_MainMenu");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler cs = canvasGO.AddComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();

        Transform root = canvasGO.transform;

        // Full-screen dark vignette overlay
        GameObject vig = new GameObject("Vignette");
        vig.transform.SetParent(root, false);
        Image vigImg = vig.AddComponent<Image>();
        vigImg.color = new Color(0f, 0f, 0f, 0.45f);
        StretchRect(vig.GetComponent<RectTransform>());

        // ── Title ─────────────────────────────────────────────────────────
        TMP_Text title = MakeTMP(root, "Title",
            new Vector2(0, 200), new Vector2(900, 160), PitMenuBranding.GameTitle, 80);
        title.fontStyle = FontStyles.Bold;
        title.color     = new Color(1f, 0.88f, 0.3f);

        TMP_Text subtitle = MakeTMP(root, "Subtitle",
            new Vector2(0, 110), new Vector2(600, 40), PitMenuBranding.GameSubtitle, 22);
        subtitle.color = new Color(0.65f, 0.65f, 0.75f);

        // Decorative separator
        MakeSeparator(root, new Vector2(0, 80), new Vector2(500, 2));

        // ── Best Run box ──────────────────────────────────────────────────
        GameObject bestBox = new GameObject("BestRunBox");
        bestBox.transform.SetParent(root, false);
        bestBox.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.5f);
        var bbrt = bestBox.GetComponent<RectTransform>();
        bbrt.anchorMin = new Vector2(0.5f, 0.5f);
        bbrt.anchorMax = new Vector2(0.5f, 0.5f);
        bbrt.anchoredPosition = new Vector2(0, 20);
        bbrt.sizeDelta = new Vector2(560, 65);

        TMP_Text bestRun = MakeTMP(bestBox.transform, "BestRunText",
            new Vector2(0, 0), new Vector2(540, 60), "No runs recorded yet.", 18);
        bestRun.color = new Color(0.85f, 0.85f, 0.85f);

        // ── Flavor / Lore text ────────────────────────────────────────────
        TMP_Text flavor = MakeTMP(root, "FlavorText",
            new Vector2(0, -65), new Vector2(640, 70),
            "\"Many warriors have entered. None have broken the cycle.\"", 17);
        flavor.color     = new Color(0.55f, 0.55f, 0.7f);
        flavor.fontStyle = FontStyles.Italic;

        // ── NEW GAME button ───────────────────────────────────────────────
        GameObject newGameGO = new GameObject("NewGameButton");
        newGameGO.transform.SetParent(root, false);
        Image newGameBg = newGameGO.AddComponent<Image>();
        newGameBg.color = new Color(0.15f, 0.55f, 0.2f);
        Button newGameBtn = newGameGO.AddComponent<Button>();
        var ngrt = newGameGO.GetComponent<RectTransform>();
        ngrt.anchorMin = new Vector2(0.5f, 0.5f);
        ngrt.anchorMax = new Vector2(0.5f, 0.5f);
        ngrt.anchoredPosition = new Vector2(0, -150);
        ngrt.sizeDelta = new Vector2(320, 70);
        TMP_Text newGameLabel = MakeTMP(newGameGO.transform, "NewGameLabel",
            Vector2.zero, new Vector2(320, 70), "NEW GAME", 28);
        newGameLabel.fontStyle = FontStyles.Bold;

        // ── LOAD GAME button ──────────────────────────────────────────────
        GameObject loadGameGO = new GameObject("LoadGameButton");
        loadGameGO.transform.SetParent(root, false);
        Image loadGameBg = loadGameGO.AddComponent<Image>();
        loadGameBg.color = new Color(0.15f, 0.30f, 0.55f);
        Button loadGameBtn = loadGameGO.AddComponent<Button>();
        var lgrt = loadGameGO.GetComponent<RectTransform>();
        lgrt.anchorMin = new Vector2(0.5f, 0.5f);
        lgrt.anchorMax = new Vector2(0.5f, 0.5f);
        lgrt.anchoredPosition = new Vector2(0, -240);
        lgrt.sizeDelta = new Vector2(320, 70);
        TMP_Text loadGameLabel = MakeTMP(loadGameGO.transform, "LoadGameLabel",
            Vector2.zero, new Vector2(320, 70), "LOAD GAME", 28);
        loadGameLabel.fontStyle = FontStyles.Bold;

        // ── Version / credits ─────────────────────────────────────────────
        TMP_Text version = MakeTMP(root, "VersionText",
            new Vector2(0, -490), new Vector2(600, 24),
            PitMenuBranding.VersionFooter, 13);
        version.color = new Color(0.4f, 0.4f, 0.4f);

        // ── Wire MainMenuManager ──────────────────────────────────────────
        MainMenuManager mm = canvasGO.AddComponent<MainMenuManager>();
        var so = new SerializedObject(mm);
        so.FindProperty("newGameButton").objectReferenceValue  = newGameBtn;
        so.FindProperty("loadGameButton").objectReferenceValue = loadGameBtn;
        so.FindProperty("titleText").objectReferenceValue      = title;
        so.FindProperty("subtitleText").objectReferenceValue   = subtitle;
        so.FindProperty("bestRunText").objectReferenceValue    = bestRun;
        so.FindProperty("flavorText").objectReferenceValue     = flavor;
        so.FindProperty("versionText").objectReferenceValue    = version;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ── Persistent HighScoreManager in menu scene ─────────────────────────

    static void SetupHighScoreManager()
    {
        GameObject go = new GameObject("HighScoreManager");
        go.AddComponent<HighScoreManager>();
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    static TMP_Text MakeTMP(Transform parent, string name, Vector2 pos, Vector2 size,
                             string text, float fontSize)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        TMP_Text t = go.AddComponent<TextMeshProUGUI>();
        t.text = text; t.fontSize = fontSize;
        t.alignment = TextAlignmentOptions.Center;
        t.color = Color.white;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return t;
    }

    static void MakeSeparator(Transform parent, Vector2 pos, Vector2 size)
    {
        GameObject go = new GameObject("Separator");
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = new Color(0.5f, 0.45f, 0.2f, 0.8f);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    static void SetColor(GameObject go, Color color)
    {
        var r = go.GetComponent<Renderer>();
        if (r != null) r.material = new Material(Shader.Find("Standard")) { color = color };
    }

    static void StretchRect(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.sizeDelta = Vector2.zero;
    }
}

/// <summary>
/// Ensures Build Settings + Play Mode Start Scene apply after script reload / on new clone.
/// </summary>
[InitializeOnLoad]
static class MainMenuPlayModeBootstrap
{
    static MainMenuPlayModeBootstrap()
    {
        if (File.Exists(MainMenuBuilder.MainMenuPath))
            MainMenuBuilder.ApplyBuildAndPlayModeSettings();
    }
}
