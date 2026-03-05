using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// CS4483 → 4 - Setup Narrative & Worldbuilding
/// Adds Activity 3 elements to the existing Main scene:
///
///   1. PROP-BASED STORYTELLING — "Echo" props (scattered warrior remnants)
///   2. LIGHT & ATMOSPHERE     — Flickering lights in danger zones, warm glow in safe zone
///   3. LORE PILLARS           — NarrativeTrigger on each colored landmark pillar
///   4. AMBIENT STORY ZONES    — Boss gate and hub ambient text triggers
///   5. WORLD BEACONS          — Dynamic wave-activated beacon system
///   6. NARRATIVE HUD PANEL    — Shared pop-up panel for lore text
///   7. HIGH SCORE MANAGER     — Persistent GO that survives scene transitions
///   8. GAME OVER UI UPDATE    — Adds Main Menu button + best run display
///
/// Run AFTER "CS4483 → ▶ SETUP EVERYTHING"
/// </summary>
public static class NarrativeSetup
{
    private static TMP_Text narrativeTitleUI;
    private static TMP_Text narrativeBodyUI;
    private static TMP_Text ambientTextUI;
    private static GameObject narrativePanel;

    [MenuItem("CS4483/4 - Setup Narrative & Worldbuilding")]
    public static void SetupNarrative()
    {
        AddHighScoreManager();
        AddNarrativeHUDPanel();
        AddEchoProps();
        AddLorePillars();
        AddAmbientZones();
        AddWorldBeacons();
        SetupFlickeringLights();
        UpdateGameOverUI();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        Debug.Log("[NarrativeSetup] ✓ Narrative elements added. Run CS4483 → 5 to build the Main Menu scene.");
    }

    // ── 1. HighScoreManager ────────────────────────────────────────────────

    static void AddHighScoreManager()
    {
        if (GameObject.Find("HighScoreManager") != null) return;
        GameObject go = new GameObject("HighScoreManager");
        go.AddComponent<HighScoreManager>();
        Debug.Log("[NarrativeSetup] HighScoreManager added (DontDestroyOnLoad).");
    }

    // ── 2. Narrative HUD Panel ────────────────────────────────────────────

    static void AddNarrativeHUDPanel()
    {
        GameObject canvas = GameObject.Find("Canvas_HUD");
        if (canvas == null) { Debug.LogWarning("[NarrativeSetup] Canvas_HUD not found. Run Setup Everything first."); return; }

        // Remove existing if re-running
        Transform existing = canvas.transform.Find("NarrativePanel");
        if (existing != null) Object.DestroyImmediate(existing.gameObject);

        // Semi-transparent dark panel (sits in the top portion of screen)
        narrativePanel = new GameObject("NarrativePanel");
        narrativePanel.transform.SetParent(canvas.transform, false);
        Image bg = narrativePanel.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.72f);
        RectTransform rt = narrativePanel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot     = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0, -10);
        rt.sizeDelta = new Vector2(700, 170);

        // Title
        narrativeTitleUI = MakeTMP(narrativePanel.transform, "NarrativeTitle",
            new Vector2(0, -18), new Vector2(680, 30), "", 18);
        narrativeTitleUI.fontStyle = TMPro.FontStyles.Bold;
        narrativeTitleUI.color = new Color(1f, 0.85f, 0.3f);

        // Body
        narrativeBodyUI = MakeTMP(narrativePanel.transform, "NarrativeBody",
            new Vector2(0, -80), new Vector2(680, 100), "", 14);
        narrativeBodyUI.color = new Color(0.9f, 0.9f, 0.9f);

        narrativePanel.SetActive(false);

        // Ambient text (bottom-center, used by AmbientStoryZone)
        Transform existing2 = canvas.transform.Find("AmbientText");
        if (existing2 != null) Object.DestroyImmediate(existing2.gameObject);
        GameObject ambGO = new GameObject("AmbientText");
        ambGO.transform.SetParent(canvas.transform, false);
        ambientTextUI = ambGO.AddComponent<TextMeshProUGUI>();
        ambientTextUI.text      = "";
        ambientTextUI.fontSize  = 16;
        ambientTextUI.fontStyle = TMPro.FontStyles.Italic;
        ambientTextUI.color     = new Color(0.7f, 0.7f, 1f, 0f);
        ambientTextUI.alignment = TMPro.TextAlignmentOptions.Center;
        RectTransform art = ambGO.GetComponent<RectTransform>();
        art.anchorMin = new Vector2(0.5f, 0f);
        art.anchorMax = new Vector2(0.5f, 0f);
        art.anchoredPosition = new Vector2(0, -120);
        art.sizeDelta = new Vector2(600, 30);
    }

    // ── 3. Echo Props (prop-based storytelling) ───────────────────────────

    static void AddEchoProps()
    {
        Transform parent = GetOrCreateParent("=== NARRATIVE ===", "EchoProps");
        ClearChildren(parent);

        Material echoMat = GetOrCreateMat("M_Echo", new Color(0.35f, 0.32f, 0.28f));

        // Broken weapon remnants — thin elongated cubes scattered around the arena
        (Vector3 pos, Vector3 scale, float yRot, string name)[] props = {
            (new Vector3( 3f,  0.3f,  5f),  new Vector3(0.15f, 0.15f, 1.2f), 35f,  "BrokenSword_A"),
            (new Vector3(-5f,  0.3f, -2f),  new Vector3(0.15f, 0.15f, 0.9f), 110f, "BrokenSword_B"),
            (new Vector3( 8f,  0.3f,  2f),  new Vector3(0.15f, 0.15f, 1.4f), 200f, "BrokenSpear_A"),
            (new Vector3(-3f,  0.3f,  9f),  new Vector3(0.15f, 0.15f, 0.8f), 75f,  "BrokenSpear_B"),
            // Cracked shield remnants — flat squares
            (new Vector3( 6f,  0.25f, -4f), new Vector3(0.8f, 0.15f, 0.8f),  20f,  "CrackedShield_A"),
            (new Vector3(-7f,  0.25f,  4f), new Vector3(0.7f, 0.15f, 0.7f),  145f, "CrackedShield_B"),
            // Ancient banners — tall thin flat shapes
            (new Vector3( 4f,  1.5f,  12f), new Vector3(0.2f, 3f, 0.08f),    0f,   "AncientBanner_A"),
            (new Vector3(-4f,  1.5f,  12f), new Vector3(0.2f, 3f, 0.08f),    0f,   "AncientBanner_B"),
            (new Vector3( 4f,  1.5f, -11f), new Vector3(0.2f, 3f, 0.08f),    0f,   "BossGateBanner_L"),
            (new Vector3(-4f,  1.5f, -11f), new Vector3(0.2f, 3f, 0.08f),    0f,   "BossGateBanner_R"),
        };

        foreach (var (pos, scale, yRot, pname) in props)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = pname;
            go.transform.SetParent(parent);
            go.transform.position = pos;
            go.transform.localScale = scale;
            go.transform.rotation = Quaternion.Euler(0f, yRot, 0f);
            go.GetComponent<Renderer>().sharedMaterial = echoMat;
            // Remove collider so they don't block movement
            Object.DestroyImmediate(go.GetComponent<BoxCollider>());
        }

        // Scorched ground marks at spawn zones — dark flat discs using cylinders
        Material scorchedMat = GetOrCreateMat("M_Scorched", new Color(0.12f, 0.08f, 0.08f));
        (Vector3 pos, string name)[] scorch = {
            (new Vector3( 20f,  0.21f, 20f), "Scorch_NE"),
            (new Vector3(-20f,  0.21f, 20f), "Scorch_NW"),
            (new Vector3( 20f,  0.21f, -5f), "Scorch_SE"),
            (new Vector3(-20f,  0.21f, -5f), "Scorch_SW"),
        };
        foreach (var (pos, sname) in scorch)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = sname;
            go.transform.SetParent(parent);
            go.transform.position = pos;
            go.transform.localScale = new Vector3(3f, 0.02f, 3f);
            go.GetComponent<Renderer>().sharedMaterial = scorchedMat;
            Object.DestroyImmediate(go.GetComponent<CapsuleCollider>());
        }
    }

    // ── 4. Lore Pillars ───────────────────────────────────────────────────

    static void AddLorePillars()
    {
        (string goName, LorePillar.PillarType type, Vector3 triggerSize)[] pillars = {
            ("Hub_Pillar",    LorePillar.PillarType.Central, new Vector3(5f, 4f, 5f)),
            ("Green_Pillar",  LorePillar.PillarType.North,   new Vector3(4f, 4f, 4f)),
            ("Yellow_Pillar", LorePillar.PillarType.East,    new Vector3(4f, 4f, 4f)),
            ("Red_Pillar",    LorePillar.PillarType.West,    new Vector3(4f, 4f, 4f)),
            ("Boss_Pillar",   LorePillar.PillarType.Boss,    new Vector3(5f, 4f, 5f)),
        };

        foreach (var (goName, ptype, trigSize) in pillars)
        {
            GameObject go = GameObject.Find(goName);
            if (go == null) { Debug.LogWarning($"[NarrativeSetup] Pillar '{goName}' not found."); continue; }

            // Add trigger collider (larger than the visual pillar)
            BoxCollider bc = go.AddComponent<BoxCollider>();
            bc.size      = trigSize;
            bc.isTrigger = true;

            // Add LorePillar script
            LorePillar lp = go.AddComponent<LorePillar>();

            // Set pillar type via SerializedObject
            var so = new SerializedObject(lp);
            so.FindProperty("pillarType").enumValueIndex = (int)ptype;
            so.ApplyModifiedPropertiesWithoutUndo();

            // Wire UI references
            if (narrativePanel != null)
                lp.SetUIRefs(narrativePanel, narrativeTitleUI, narrativeBodyUI);
        }
    }

    // ── 5. Ambient Story Zones ────────────────────────────────────────────

    static void AddAmbientZones()
    {
        Transform parent = GetOrCreateParent("=== NARRATIVE ===", "AmbientZones");
        ClearChildren(parent);

        (string zoneName, Vector3 pos, Vector3 size, string text)[] zones = {
            (
                "Zone_BossGate",
                new Vector3(0f, 2f, -10f),
                new Vector3(6f, 4f, 3f),
                "A dark presence stirs beyond the gate. Thorn remembers you."
            ),
            (
                "Zone_HubCenter",
                new Vector3(0f, 2f, 0f),
                new Vector3(7f, 4f, 7f),
                "The Grounds stir. Another warrior enters the cycle."
            ),
        };

        foreach (var (zoneName, pos, size, text) in zones)
        {
            GameObject go = new GameObject(zoneName);
            go.transform.SetParent(parent);
            go.transform.position = pos;

            BoxCollider bc = go.AddComponent<BoxCollider>();
            bc.size      = size;
            bc.isTrigger = true;

            AmbientStoryZone zone = go.AddComponent<AmbientStoryZone>();
            var so = new SerializedObject(zone);
            so.FindProperty("ambientText").stringValue = text;
            so.ApplyModifiedPropertiesWithoutUndo();

            if (ambientTextUI != null)
                zone.SetTextUI(ambientTextUI);
        }
    }

    // ── 6. World Beacons (dynamic wave-activated lighting) ────────────────

    static void AddWorldBeacons()
    {
        // Place small glowing beacon objects on the raised platforms + key spots
        Transform parent = GetOrCreateParent("=== NARRATIVE ===", "WorldBeacons");
        ClearChildren(parent);

        (Vector3 pos, int wave, Color color, string bname)[] beacons = {
            (new Vector3(17f, 3f,  10f), 1, new Color(0.2f, 0.8f, 1.0f),  "Beacon_NE_Platform"),
            (new Vector3(-17f, 3f, -10f), 2, new Color(0.6f, 0.2f, 1.0f), "Beacon_SW_Platform"),
            (new Vector3(16f, 3f,   0f), 3, new Color(1.0f, 0.9f, 0.1f),  "Beacon_East"),
            (new Vector3(-16f, 3f,  0f), 4, new Color(1.0f, 0.2f, 0.1f),  "Beacon_West"),
            (new Vector3(0f,  3f, -17f), 5, new Color(0.8f, 0.0f, 0.0f),  "Beacon_BossArena"),
        };

        foreach (var (pos, wave, color, bname) in beacons)
        {
            GameObject go = new GameObject(bname);
            go.transform.SetParent(parent);
            go.transform.position = pos;

            // Small glowing sphere
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "BeaconGlobe";
            sphere.transform.SetParent(go.transform);
            sphere.transform.localPosition = Vector3.zero;
            sphere.transform.localScale    = Vector3.one * 0.3f;
            Object.DestroyImmediate(sphere.GetComponent<SphereCollider>());
            Material mat = GetOrCreateMat($"M_Beacon_{bname}", new Color(0.1f, 0.1f, 0.1f));
            sphere.GetComponent<Renderer>().sharedMaterial = mat;

            // Point light (starts dim)
            GameObject lightGO = new GameObject("BeaconLight");
            lightGO.transform.SetParent(go.transform);
            lightGO.transform.localPosition = Vector3.zero;
            Light l = lightGO.AddComponent<Light>();
            l.type      = LightType.Point;
            l.color     = color;
            l.intensity = 0.1f;
            l.range     = 8f;

            // WorldBeacon component
            WorldBeacon wb = go.AddComponent<WorldBeacon>();
            var so = new SerializedObject(wb);
            so.FindProperty("activatesOnWave").intValue         = wave;
            so.FindProperty("activeColor").colorValue           = color;
            so.FindProperty("dormantColor").colorValue          = new Color(0.1f, 0.1f, 0.1f);
            so.FindProperty("beaconLight").objectReferenceValue = l;
            so.FindProperty("beaconRenderer").objectReferenceValue = sphere.GetComponent<Renderer>();
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    // ── 7. Atmospheric lighting tweaks ────────────────────────────────────

    static void SetupFlickeringLights()
    {
        // The boss arena light gets a flicker component
        GameObject bossLight = GameObject.Find("Light_Boss");
        if (bossLight != null && bossLight.GetComponent<LightFlicker>() == null)
            bossLight.AddComponent<LightFlicker>();

        // Reduce ambient light for atmosphere
        RenderSettings.ambientLight = new Color(0.08f, 0.06f, 0.06f);
    }

    // ── 8. Update GameOverUI with new fields ──────────────────────────────

    static void UpdateGameOverUI()
    {
        GameObject goPanel = GameObject.Find("GameOverPanel");
        if (goPanel == null) return;

        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        // Add "NEW BEST!" text if not already there
        if (goPanel.transform.Find("NewBest_Text") == null)
        {
            TMP_Text nb = MakeTMPInPanel(goPanel.transform, "NewBest_Text",
                new Vector2(0, 140), new Vector2(400, 50), "✦ NEW BEST! ✦", 28);
            nb.color     = new Color(1f, 0.85f, 0f);
            nb.fontStyle = TMPro.FontStyles.Bold;
            nb.gameObject.SetActive(false);
        }

        // Add best run line
        if (goPanel.transform.Find("BestRun_Text") == null)
        {
            TMP_Text br = MakeTMPInPanel(goPanel.transform, "BestRun_Text",
                new Vector2(0, -72), new Vector2(450, 26), "", 15);
            br.color = new Color(0.75f, 0.75f, 0.75f);
        }

        // Add Main Menu button
        if (goPanel.transform.Find("MenuButton") == null)
        {
            GameObject btnGO = new GameObject("MenuButton");
            btnGO.transform.SetParent(goPanel.transform, false);
            btnGO.AddComponent<Image>().color = new Color(0.15f, 0.25f, 0.55f);
            btnGO.AddComponent<UnityEngine.UI.Button>();
            var brt = btnGO.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0.5f, 0.5f);
            brt.anchorMax = new Vector2(0.5f, 0.5f);
            brt.anchoredPosition = new Vector2(0, -145);
            brt.sizeDelta = new Vector2(220, 50);
            MakeTMPInPanel(btnGO.transform, "MenuLabel",
                Vector2.zero, new Vector2(220, 50), "MAIN MENU", 22);
        }

        // Re-wire GameOverUI
        GameOverUI goUI = goPanel.GetComponent<GameOverUI>();
        if (goUI != null)
        {
            var so = new SerializedObject(goUI);
            so.FindProperty("newBestText").objectReferenceValue =
                goPanel.transform.Find("NewBest_Text")?.GetComponent<TMP_Text>();
            so.FindProperty("bestRunText").objectReferenceValue =
                goPanel.transform.Find("BestRun_Text")?.GetComponent<TMP_Text>();
            so.FindProperty("menuButton").objectReferenceValue =
                goPanel.transform.Find("MenuButton")?.GetComponent<UnityEngine.UI.Button>();
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    static Transform GetOrCreateParent(string rootName, string childName)
    {
        GameObject root = GameObject.Find(rootName);
        if (root == null) root = new GameObject(rootName);
        Transform child = root.transform.Find(childName);
        if (child == null)
        {
            GameObject c = new GameObject(childName);
            c.transform.SetParent(root.transform);
            return c.transform;
        }
        return child;
    }

    static void ClearChildren(Transform t)
    {
        for (int i = t.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(t.GetChild(i).gameObject);
    }

    static Material GetOrCreateMat(string name, Color color)
    {
        string path = $"Assets/Materials/{name}.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Standard")) { color = color };
            AssetDatabase.CreateAsset(mat, path);
        }
        return mat;
    }

    static TMP_Text MakeTMP(Transform parent, string name, Vector2 pos, Vector2 size,
                             string text, float fontSize)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        TMP_Text t = go.AddComponent<TextMeshProUGUI>();
        t.text = text; t.fontSize = fontSize;
        t.alignment = TMPro.TextAlignmentOptions.Center;
        t.color = Color.white;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return t;
    }

    static TMP_Text MakeTMPInPanel(Transform parent, string name, Vector2 pos, Vector2 size,
                                    string text, float fontSize)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        TMP_Text t = go.AddComponent<TextMeshProUGUI>();
        t.text = text; t.fontSize = fontSize;
        t.alignment = TMPro.TextAlignmentOptions.Center;
        t.color = Color.white;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return t;
    }
}
