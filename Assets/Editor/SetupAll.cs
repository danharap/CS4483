using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// CS4483 → ▶ SETUP EVERYTHING
/// One-click tool that:
///   1. Creates all prefabs (Projectile, XPOrb, 3 enemies)
///   2. Builds the full graybox level geometry
///   3. Creates player, camera, managers, and full UI canvas
///   4. Auto-wires EVERY inspector reference via SerializedObject
///   5. Adds a NavMeshSurface and bakes the NavMesh
///   6. Saves the scene
///
/// After running this, just press Play.
/// </summary>
public static class SetupAll
{
    // ── Held across the setup methods ─────────────────────────────────────
    private static GameObject       playerGO;
    private static Transform        firePoint;
    private static GameManager      gmComp;
    private static WaveManager      wmComp;
    private static EnemySpawner     esComp;
    private static HUDManager       hudComp;
    private static UpgradeManager   umComp;
    private static PlaytestLogger   plComp;
    private static UpgradeUI        upgradeUIComp;
    private static GameOverUI       gameOverUIComp;
    private static Transform[]      spawnPointTransforms;

    // UI
    private static Slider   hpSlider, xpSlider;
    private static TMP_Text hpText, levelText, waveText, timerText, transitionText;
    private static Image    damageOverlay;
    private static GameObject upgradePanel, gameOverPanel;
    private static Button     card0Btn, card1Btn, card2Btn;
    private static TMP_Text   card0Name, card1Name, card2Name;
    private static TMP_Text   card0Desc, card1Desc, card2Desc;
    private static Image      card0Bg,  card1Bg,  card2Bg;
    private static TMP_Text   upgradeTitle;
    private static TMP_Text   statTime, statWaves, statKills;
    private static Button     restartBtn;

    // ── Entry Point ───────────────────────────────────────────────────────

    [MenuItem("CS4483/▶  SETUP EVERYTHING  (Run This First!)")]
    public static void SetupEverything()
    {
        // Safety check
        if (!EditorSceneManager.GetActiveScene().isLoaded)
        {
            Debug.LogError("[SetupAll] Open or create a scene first.");
            return;
        }

        Debug.Log("[SetupAll] Starting full setup...");

        Step1_ClearExistingSetup();
        Step2_CreatePrefabs();
        Step3_BuildLevel();
        Step4_SetupCamera();
        Step5_CreateManagers();
        Step6_CreatePlayer();
        Step7_CreateCanvas();
        Step8_WireAllReferences();
        Step9_SetupGateDoor();
        Step10_WireEnemyPrefabOrbs();
        Step11_BakeNavMesh();

        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        Debug.Log("[SetupAll] ✓ Done! Press Play to test.\n" +
                  "If enemies don't navigate walls: Window → AI → Navigation → Bake (retry).");
    }

    // ── Step 1: Clear old setup objects ──────────────────────────────────

    static void Step1_ClearExistingSetup()
    {
        string[] toRemove = { "=== MANAGERS ===", "Canvas_HUD", "=== LEVEL ===" };
        foreach (string n in toRemove)
        {
            GameObject g = GameObject.Find(n);
            if (g != null) Object.DestroyImmediate(g);
        }
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) Object.DestroyImmediate(p);
    }

    // ── Step 2: Create prefab assets ─────────────────────────────────────

    static void Step2_CreatePrefabs()
    {
        PrefabBuilder.CreateAllPrefabs();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[SetupAll] Prefabs ready.");
    }

    // ── Step 3: Build level geometry ─────────────────────────────────────

    static void Step3_BuildLevel()
    {
        ProBuilderLevelBuilder.BuildLevel();
        Debug.Log("[SetupAll] Level geometry built with ProBuilder.");
    }

    // ── Step 4: Camera ────────────────────────────────────────────────────

    static void Step4_SetupCamera()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            GameObject go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            cam    = go.AddComponent<Camera>();
        }
        cam.transform.position = new Vector3(0, 16f, -9f);
        cam.transform.rotation = Quaternion.Euler(60f, 0f, 0f);
        cam.fieldOfView = 60f;

        if (cam.GetComponent<CameraController>() == null)
            cam.gameObject.AddComponent<CameraController>();

        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.08f, 0.08f, 0.08f);
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 30f;
        RenderSettings.fogEndDistance   = 65f;
    }

    // ── Step 5: Managers ──────────────────────────────────────────────────

    static void Step5_CreateManagers()
    {
        GameObject mgr = new GameObject("=== MANAGERS ===");
        gmComp  = mgr.AddComponent<GameManager>();
        wmComp  = mgr.AddComponent<WaveManager>();
        esComp  = mgr.AddComponent<EnemySpawner>();
        hudComp = mgr.AddComponent<HUDManager>();
        umComp  = mgr.AddComponent<UpgradeManager>();
        plComp  = mgr.AddComponent<PlaytestLogger>();
    }

    // ── Step 6: Player ────────────────────────────────────────────────────

    static void Step6_CreatePlayer()
    {
        playerGO = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        playerGO.name = "Player";
        playerGO.tag  = "Player";
        playerGO.transform.position = new Vector3(0, 1.1f, 0);

        Object.DestroyImmediate(playerGO.GetComponent<CapsuleCollider>());
        CharacterController cc = playerGO.AddComponent<CharacterController>();
        cc.height = 2f;
        cc.radius = 0.4f;

        playerGO.AddComponent<PlayerController>();
        playerGO.AddComponent<PlayerHealth>();
        playerGO.AddComponent<PlayerWeapon>();
        playerGO.AddComponent<PlayerXP>();

        Material mat = new Material(Shader.Find("Standard")) { color = new Color(0.2f, 0.4f, 0.9f) };
        playerGO.GetComponent<Renderer>().material = mat;

        GameObject fp = new GameObject("FirePoint");
        fp.transform.SetParent(playerGO.transform);
        fp.transform.localPosition = new Vector3(0f, 0.5f, 0.7f);
        firePoint = fp.transform;
    }

    // ── Step 7: Canvas + HUD ─────────────────────────────────────────────

    static void Step7_CreateCanvas()
    {
        // EventSystem for UI interactions
        if (GameObject.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject eventSys = new GameObject("EventSystem");
            eventSys.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSys.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        GameObject canvasGO = new GameObject("Canvas_HUD");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 0;
        CanvasScaler cs = canvasGO.AddComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();

        Transform root = canvasGO.transform;

        // ── HP bar (bottom-left) ──────────────────────────────────────────
        hpSlider = MakeSlider(root, "HP_Slider",
            new Vector2(-760, -490), new Vector2(300, 30), new Color(1f, 0.2f, 0.2f));
        hpText = MakeTMP(root, "HP_Text",
            new Vector2(-760, -455), new Vector2(300, 35), "100 / 100", 24);
        hpText.fontStyle = FontStyles.Bold;
        hpText.color = new Color(1f, 0.3f, 0.3f);

        // ── XP bar (bottom-right) ─────────────────────────────────────────
        xpSlider = MakeSlider(root, "XP_Slider",
            new Vector2(760, -490), new Vector2(300, 30), new Color(0.2f, 0.9f, 0.3f));
        levelText = MakeTMP(root, "Level_Text",
            new Vector2(760, -455), new Vector2(150, 35), "Lv 1", 24);
        levelText.fontStyle = FontStyles.Bold;
        levelText.color = new Color(0.4f, 1f, 0.5f);

        // ── Wave / timer (top-center) ─────────────────────────────────────
        waveText  = MakeTMP(root, "Wave_Text",  new Vector2(0, 490), new Vector2(220, 34), "Wave 1", 24);
        timerText = MakeTMP(root, "Timer_Text", new Vector2(0, 455), new Vector2(180, 28), "60s", 20);

        // ── Transition banner (center) ────────────────────────────────────
        transitionText = MakeTMP(root, "Transition_Text", new Vector2(0, 60), new Vector2(700, 70), "", 38);
        transitionText.color = Color.yellow;
        transitionText.fontStyle = FontStyles.Bold;

        // ── Full-screen damage overlay ────────────────────────────────────
        GameObject overlayGO = new GameObject("DamageOverlay");
        overlayGO.transform.SetParent(root, false);
        damageOverlay = overlayGO.AddComponent<Image>();
        damageOverlay.color = new Color(1f, 0f, 0f, 0f);
        RectTransform ort = overlayGO.GetComponent<RectTransform>();
        ort.anchorMin = Vector2.zero; ort.anchorMax = Vector2.one; ort.sizeDelta = Vector2.zero;

        // ── Upgrade panel ─────────────────────────────────────────────────
        upgradePanel = MakePanel(root, "UpgradePanel", new Color(0f, 0f, 0f, 0.9f));
        // Force this panel to render on top by moving it to last sibling
        upgradePanel.transform.SetAsLastSibling();
        
        upgradeTitle = MakeTMP(upgradePanel.transform, "UpgradeTitle",
            new Vector2(0, 280), new Vector2(900, 80), "LEVEL UP! CHOOSE AN UPGRADE", 48);
        upgradeTitle.color = Color.yellow;
        upgradeTitle.fontStyle = FontStyles.Bold;
        MakeUpgradeCard(upgradePanel.transform, "Card0", new Vector2(-380, 0),
            out card0Btn, out card0Name, out card0Desc, out card0Bg);
        MakeUpgradeCard(upgradePanel.transform, "Card1", new Vector2(0, 0),
            out card1Btn, out card1Name, out card1Desc, out card1Bg);
        MakeUpgradeCard(upgradePanel.transform, "Card2", new Vector2(380, 0),
            out card2Btn, out card2Name, out card2Desc, out card2Bg);
        upgradeUIComp = upgradePanel.AddComponent<UpgradeUI>();
        // Panel starts active so Unity initializes it properly - UpgradeUI.Awake() will set it to false
        upgradePanel.SetActive(true);

        // ── Game over panel ───────────────────────────────────────────────
        gameOverPanel = MakePanel(root, "GameOverPanel", new Color(0f, 0f, 0f, 0.85f));
        TMP_Text goTitle = MakeTMP(gameOverPanel.transform, "GO_Title",
            new Vector2(0, 200), new Vector2(500, 70), "GAME OVER", 52);
        goTitle.color = new Color(0.9f, 0.15f, 0.15f);
        goTitle.fontStyle = FontStyles.Bold;
        statTime   = MakeTMP(gameOverPanel.transform, "Stats_Time",  new Vector2(0, 80), new Vector2(400, 36), "Time Survived: 00:00", 24);
        statWaves  = MakeTMP(gameOverPanel.transform, "Stats_Waves", new Vector2(0, 35), new Vector2(400, 36), "Waves Cleared: 0",     24);
        statKills  = MakeTMP(gameOverPanel.transform, "Stats_Kills", new Vector2(0,-10), new Vector2(400, 36), "Total Kills: 0",       24);
        restartBtn = MakeButton(gameOverPanel.transform, "RestartButton",
            new Vector2(0, -100), new Vector2(220, 56), "RESTART", new Color(0.1f, 0.55f, 0.1f));
        gameOverUIComp = gameOverPanel.AddComponent<GameOverUI>();
        gameOverPanel.SetActive(false);
    }

    // ── Step 8: Wire ALL references via SerializedObject ─────────────────

    static void Step8_WireAllReferences()
    {
        // Collect spawn point transforms from built level
        GameObject spawnRoot = GameObject.Find("SpawnPoints");
        if (spawnRoot != null)
        {
            var pts = new List<Transform>();
            foreach (Transform child in spawnRoot.transform)
                pts.Add(child);
            spawnPointTransforms = pts.ToArray();
        }
        else { spawnPointTransforms = new Transform[0]; }

        // Load created prefabs from disk
        GameObject projPrefab   = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Projectile.prefab");
        GameObject chaserPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy_Chaser.prefab");
        GameObject fastPrefab   = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy_Fast.prefab");
        GameObject bossPrefab   = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy_Boss.prefab");

        // ── GameManager ───────────────────────────────────────────────────
        Wire(gmComp, "waveManager",  wmComp);
        Wire(gmComp, "hudManager",   hudComp);
        Wire(gmComp, "upgradeUI",    upgradeUIComp);
        Wire(gmComp, "gameOverUI",   gameOverUIComp);
        Wire(gmComp, "logger",       plComp);
        Wire(gmComp, "playerObject", playerGO);

        // ── WaveManager ───────────────────────────────────────────────────
        Wire(wmComp, "spawner", esComp);

        // ── EnemySpawner ──────────────────────────────────────────────────
        {
            var so = new SerializedObject(esComp);
            var prop = so.FindProperty("spawnPoints");
            prop.arraySize = spawnPointTransforms.Length;
            for (int i = 0; i < spawnPointTransforms.Length; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = spawnPointTransforms[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        Wire(esComp, "chaserPrefab", chaserPrefab);
        Wire(esComp, "fastPrefab",   fastPrefab);
        Wire(esComp, "bossPrefab",   bossPrefab);

        // ── PlayerWeapon ──────────────────────────────────────────────────
        var weapon = playerGO.GetComponent<PlayerWeapon>();
        Wire(weapon, "projectilePrefab", projPrefab);
        Wire(weapon, "firePoint", firePoint);

        // ── PlayerHealth ──────────────────────────────────────────────────
        var health = playerGO.GetComponent<PlayerHealth>();
        Wire(health, "damageOverlay", damageOverlay);

        // ── CameraController ──────────────────────────────────────────────
        var cam = Camera.main?.GetComponent<CameraController>();
        if (cam != null) Wire(cam, "target", playerGO.transform);

        // ── HUDManager ────────────────────────────────────────────────────
        Wire(hudComp, "hpSlider",        hpSlider);
        Wire(hudComp, "hpText",          hpText);
        Wire(hudComp, "xpSlider",        xpSlider);
        Wire(hudComp, "levelText",       levelText);
        Wire(hudComp, "waveText",        waveText);
        Wire(hudComp, "timerText",       timerText);
        Wire(hudComp, "transitionText",  transitionText);

        // ── UpgradeUI ─────────────────────────────────────────────────────
        Wire(upgradeUIComp, "upgradePanel", upgradePanel);
        Wire(upgradeUIComp, "titleText",    upgradeTitle);
        Wire(upgradeUIComp, "card0Button",  card0Btn);
        Wire(upgradeUIComp, "card0Name",    card0Name);
        Wire(upgradeUIComp, "card0Desc",    card0Desc);
        Wire(upgradeUIComp, "card0Bg",      card0Bg);
        Wire(upgradeUIComp, "card1Button",  card1Btn);
        Wire(upgradeUIComp, "card1Name",    card1Name);
        Wire(upgradeUIComp, "card1Desc",    card1Desc);
        Wire(upgradeUIComp, "card1Bg",      card1Bg);
        Wire(upgradeUIComp, "card2Button",  card2Btn);
        Wire(upgradeUIComp, "card2Name",    card2Name);
        Wire(upgradeUIComp, "card2Desc",    card2Desc);
        Wire(upgradeUIComp, "card2Bg",      card2Bg);

        // ── GameOverUI ────────────────────────────────────────────────────
        Wire(gameOverUIComp, "gameOverPanel",      gameOverPanel);
        Wire(gameOverUIComp, "timeSurvivedText",   statTime);
        Wire(gameOverUIComp, "wavesClearedText",   statWaves);
        Wire(gameOverUIComp, "totalKillsText",     statKills);
        Wire(gameOverUIComp, "restartButton",      restartBtn);

        Debug.Log("[SetupAll] All references wired.");
    }

    // ── Step 9: Gate door component ───────────────────────────────────────

    static void Step9_SetupGateDoor()
    {
        GameObject gate = GameObject.Find("GateDoor");
        if (gate != null && gate.GetComponent<GateDoor>() == null)
            gate.AddComponent<GateDoor>();
    }

    // ── Step 10: XP orb reference in enemy prefabs ────────────────────────

    static void Step10_WireEnemyPrefabOrbs()
    {
        GameObject xpOrbPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/XPOrb.prefab");
        if (xpOrbPrefab == null) { Debug.LogWarning("[SetupAll] XPOrb prefab not found."); return; }

        string[] enemyPaths = {
            "Assets/Prefabs/Enemy_Chaser.prefab",
            "Assets/Prefabs/Enemy_Fast.prefab",
            "Assets/Prefabs/Enemy_Boss.prefab"
        };
        foreach (string path in enemyPaths)
        {
            using (var scope = new PrefabUtility.EditPrefabContentsScope(path))
            {
                EnemyBase eb = scope.prefabContentsRoot.GetComponent<EnemyBase>();
                if (eb != null)
                {
                    var so = new SerializedObject(eb);
                    so.FindProperty("xpOrbPrefab").objectReferenceValue = xpOrbPrefab;
                    so.ApplyModifiedProperties();
                }
            }
        }
        AssetDatabase.SaveAssets();
        Debug.Log("[SetupAll] XPOrb prefab wired to all enemy prefabs.");
    }

    // ── Step 11: NavMesh bake ─────────────────────────────────────────────

    static void Step11_BakeNavMesh()
    {
        // Use the new AI Navigation package (NavMeshSurface)
        GameObject levelRoot = GameObject.Find("=== LEVEL ===");
        if (levelRoot == null) { Debug.LogWarning("[SetupAll] Level root not found — NavMesh skipped."); return; }

        // Remove old surface if any
        NavMeshSurface existing = levelRoot.GetComponent<NavMeshSurface>();
        if (existing != null) Object.DestroyImmediate(existing);

        NavMeshSurface surface = levelRoot.AddComponent<NavMeshSurface>();
        surface.collectObjects = CollectObjects.All;
        surface.useGeometry    = NavMeshCollectGeometry.PhysicsColliders;
        surface.BuildNavMesh();
        Debug.Log("[SetupAll] NavMesh baked.");
    }

    // ── UI Factory Helpers ────────────────────────────────────────────────

    static Slider MakeSlider(Transform parent, string name, Vector2 pos, Vector2 size, Color fillColor)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Slider slider = go.AddComponent<Slider>();
        slider.interactable = false;

        // Background
        GameObject bg = new GameObject("BG");
        bg.transform.SetParent(go.transform, false);
        bg.AddComponent<Image>().color = new Color(0.12f, 0.12f, 0.12f);
        StretchRect(bg.GetComponent<RectTransform>());

        // Fill area
        GameObject fa = new GameObject("FillArea");
        fa.transform.SetParent(go.transform, false);
        StretchRect(fa.AddComponent<RectTransform>());

        // Fill
        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fa.transform, false);
        fill.AddComponent<Image>().color = fillColor;
        RectTransform fillRt = fill.GetComponent<RectTransform>();
        StretchRect(fillRt);

        slider.fillRect = fillRt;

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot     = new Vector2(0.5f, 0f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        return slider;
    }

    static TMP_Text MakeTMP(Transform parent, string name, Vector2 pos, Vector2 size,
                             string text, float fontSize = 18f)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        TMP_Text t = go.AddComponent<TextMeshProUGUI>();
        t.text      = text;
        t.fontSize  = fontSize;
        t.alignment = TextAlignmentOptions.Center;
        t.color     = Color.white;

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        return t;
    }

    static GameObject MakePanel(Transform parent, string name, Color bgColor)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = bgColor;
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.sizeDelta = Vector2.zero;
        return go;
    }

    static void MakeUpgradeCard(Transform parent, string name, Vector2 pos,
                                 out Button btn, out TMP_Text cardName,
                                 out TMP_Text cardDesc, out Image cardBg)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        cardBg = go.AddComponent<Image>();
        cardBg.color = new Color(0.2f, 0.2f, 0.2f);
        btn = go.AddComponent<Button>();
        
        // Set button colors for better visibility
        ColorBlock colors = btn.colors;
        colors.normalColor = new Color(0.2f, 0.2f, 0.2f);
        colors.highlightedColor = new Color(0.4f, 0.4f, 0.4f);
        colors.pressedColor = new Color(0.6f, 0.6f, 0.6f);
        btn.colors = colors;

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(320, 220);

        cardName = MakeTMP(go.transform, "CardName", new Vector2(0, 55), new Vector2(300, 45), "Upgrade", 22);
        cardName.fontStyle = FontStyles.Bold;
        cardName.color = Color.yellow;
        cardDesc = MakeTMP(go.transform, "CardDesc", new Vector2(0, -15), new Vector2(300, 100), "Description", 16);
        cardDesc.color = new Color(0.9f, 0.9f, 0.9f);
    }

    static Button MakeButton(Transform parent, string name, Vector2 pos, Vector2 size,
                              string label, Color bgColor)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = bgColor;
        Button btn = go.AddComponent<Button>();

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        MakeTMP(go.transform, "Label", Vector2.zero, size, label, 24).fontStyle = FontStyles.Bold;

        return btn;
    }

    static void StretchRect(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.sizeDelta = Vector2.zero;
    }

    // ── Generic wire helper via SerializedObject ──────────────────────────

    static void Wire(Object target, string fieldName, Object value)
    {
        if (target == null) { Debug.LogWarning($"[Wire] target is null for field '{fieldName}'"); return; }
        var so = new SerializedObject(target);
        var prop = so.FindProperty(fieldName);
        if (prop == null) { Debug.LogWarning($"[Wire] Field '{fieldName}' not found on {target.GetType().Name}"); return; }
        prop.objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
