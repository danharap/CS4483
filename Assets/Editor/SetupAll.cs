using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.AI;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// CS4483 → ▶ SETUP EVERYTHING
/// One-click tool that:
///   1. Creates all prefabs (Projectile, XPOrb, 3 enemies)
///   2. Builds the full graybox level geometry (Arena 1 + Arena 2 for portal after wave 5)
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

    private const string LevelUpButtonSpritePath = "Assets/Sprites/LevelUpButton.png";
    private static Sprite s_levelUpButtonSprite;
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
        // ALWAYS work in MainScene, never in MainMenu!
        string mainScenePath = "Assets/Scenes/MainScene.unity";
        
        // Ensure Scenes folder exists
        if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            AssetDatabase.CreateFolder("Assets", "Scenes");
        
        // Force open MainScene (closes all other scenes)
        Scene mainScene;
        if (System.IO.File.Exists(mainScenePath))
        {
            Debug.Log("[SetupAll] Opening existing MainScene...");
            mainScene = EditorSceneManager.OpenScene(mainScenePath, OpenSceneMode.Single);
        }
        else
        {
            Debug.Log("[SetupAll] Creating new MainScene...");
            mainScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(mainScene, mainScenePath);
        }
        
        EditorSceneManager.SetActiveScene(mainScene);
        Debug.Log($"[SetupAll] ✓ Now working in scene: {mainScene.name} at {mainScene.path}");

        Step1_ClearExistingSetup();
        Step2_CreatePrefabs();
        Step3_BuildLevel();
        Step3_BuildArena2();
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
        string[] toRemove = { "=== MANAGERS ===", "Canvas_HUD", "=== LEVEL ===", "=== LEVEL (ProBuilder) ===" };
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

    static void Step3_BuildArena2()
    {
        ProBuilderLevelBuilder.BuildArena2();
        Debug.Log("[SetupAll] Arena 2 built (for portal after wave 5).");
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
            if (go.GetComponent<AudioListener>() == null)
                go.AddComponent<AudioListener>();
        }
        else if (cam.GetComponent<AudioListener>() == null)
        {
            cam.gameObject.AddComponent<AudioListener>();
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
        if (mgr.GetComponent<ArenaPortalManager>() == null)
            mgr.AddComponent<ArenaPortalManager>();
        if (mgr.GetComponent<ArenaThemeController>() == null)
            mgr.AddComponent<ArenaThemeController>();
        // Optional debug spawner hotkeys (only active in editor)
        if (mgr.GetComponent<DebugSpawnHotkeys>() == null)
            mgr.AddComponent<DebugSpawnHotkeys>();
    }

    // ── Step 6: Player ────────────────────────────────────────────────────

    static void Step6_CreatePlayer()
    {
        playerGO = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        playerGO.name = "Player";
        playerGO.tag  = "Player";
        // Spawn away from center pillar to avoid collision on start
        playerGO.transform.position = new Vector3(3f, 1.1f, 3f);

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

        // ── HP bar: big red bar at bottom with black outline ───────────────
        hpSlider = MakeHealthBarBottom(root, out hpText);

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
        upgradePanel = MakePanel(root, "UpgradePanel", new Color(0f, 0f, 0f, 0f)); // Transparent: just the buttons, no overlay
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
        GameObject projPrefab    = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Projectile.prefab");
        GameObject chaserPrefab  = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy_Chaser.prefab");
        GameObject fastPrefab    = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy_Fast.prefab");
        GameObject bossPrefab    = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy_Boss.prefab");
        GameObject heavyPrefab   = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy_Heavy.prefab");
        GameObject bigBatPrefab  = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy_BigBat.prefab");

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

            // If Arena 2 exists, wire its spawn points for wave 6+
            GameObject arena2 = GameObject.Find("=== LEVEL (ProBuilder) Arena2 ===");
            if (arena2 != null)
            {
                Transform arena2SpawnRoot = arena2.transform.Find("SpawnPoints");
                if (arena2SpawnRoot != null)
                {
                    var list = new List<Transform>();
                    foreach (Transform c in arena2SpawnRoot) list.Add(c);
                    var a2Prop = so.FindProperty("spawnPointsArena2");
                    a2Prop.arraySize = list.Count;
                    for (int i = 0; i < list.Count; i++)
                        a2Prop.GetArrayElementAtIndex(i).objectReferenceValue = list[i];
                }
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        Wire(esComp, "chaserPrefab", chaserPrefab);
        Wire(esComp, "fastPrefab",   fastPrefab);
        Wire(esComp, "heavyPrefab",  heavyPrefab);
        Wire(esComp, "bossPrefab",   bossPrefab);
        Wire(esComp, "bigBatPrefab", bigBatPrefab);

        // Debug spawn hotkeys (1/2/3 and function keys) live on the MANAGERS object
        GameObject mgrRoot = GameObject.Find("=== MANAGERS ===");
        if (mgrRoot != null)
        {
            var debugHotkeys = mgrRoot.GetComponent<DebugSpawnHotkeys>();
            if (debugHotkeys != null)
                Wire(debugHotkeys, "spawner", esComp);
        }

        // ── ArenaPortalManager (portal after wave 5, transition to Arena 2) ─
        ArenaPortalManager portalMgr = Object.FindFirstObjectByType<ArenaPortalManager>();
        if (portalMgr != null)
        {
            var so = new SerializedObject(portalMgr);
            so.FindProperty("arena1Root").objectReferenceValue = GameObject.Find("=== LEVEL (ProBuilder) ===");
            so.FindProperty("arena2Root").objectReferenceValue = GameObject.Find("=== LEVEL (ProBuilder) Arena2 ===");
            so.FindProperty("spawner").objectReferenceValue = esComp;
            AudioClip portalSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/PortalTransition.mp3");
            if (portalSound != null)
                so.FindProperty("portalTransitionSound").objectReferenceValue = portalSound;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

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
        GameObject healthPackPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/HealthPack.prefab");
        GameObject damageNumberPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/DamageNumber.prefab");
        AudioClip deathSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/aDeath.wav");
        AudioClip bulletSound = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/aBullet.wav");

        if (xpOrbPrefab == null) { Debug.LogWarning("[SetupAll] XPOrb prefab not found."); return; }
        if (healthPackPrefab == null) { Debug.LogWarning("[SetupAll] HealthPack prefab not found."); }
        if (damageNumberPrefab == null) { Debug.LogWarning("[SetupAll] DamageNumber prefab not found."); }

        string[] enemyPaths = {
            "Assets/Prefabs/Enemy_Chaser.prefab",
            "Assets/Prefabs/Enemy_Fast.prefab",
            "Assets/Prefabs/Enemy_Boss.prefab",
            "Assets/Prefabs/Enemy_Heavy.prefab"
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
                    if (healthPackPrefab != null)
                        so.FindProperty("healthPackPrefab").objectReferenceValue = healthPackPrefab;
                    if (damageNumberPrefab != null)
                        so.FindProperty("damageNumberPrefab").objectReferenceValue = damageNumberPrefab;
                    if (deathSound != null)
                        so.FindProperty("deathSound").objectReferenceValue = deathSound;
                    so.ApplyModifiedProperties();
                }
            }
        }
        
        // Wire audio to player weapon
        if (playerGO != null && bulletSound != null)
        {
            PlayerWeapon weapon = playerGO.GetComponent<PlayerWeapon>();
            if (weapon != null)
            {
                var so = new SerializedObject(weapon);
                so.FindProperty("shootSound").objectReferenceValue = bulletSound;
                so.ApplyModifiedProperties();
            }
        }
        
        AssetDatabase.SaveAssets();
        Debug.Log("[SetupAll] XPOrb, HealthPack, and Audio wired to all enemy prefabs and player weapon.");
    }

    // ── Step 11: NavMesh bake ─────────────────────────────────────────────

    static void Step11_BakeNavMesh()
    {
        // Prefer ProBuilder level root, fallback to legacy name
        GameObject levelRoot = GameObject.Find("=== LEVEL (ProBuilder) ===");
        if (levelRoot == null) levelRoot = GameObject.Find("=== LEVEL ===");
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

    static Slider MakeHealthBarBottom(Transform parent, out TMP_Text hpText)
    {
        const float barHeight = 48f;
        const float outlineThickness = 4f;
        const float barWidth = 1600f;

        GameObject outlineGO = new GameObject("HP_Bar_Outline");
        outlineGO.transform.SetParent(parent, false);
        Image outlineImg = outlineGO.AddComponent<Image>();
        outlineImg.color = Color.black;
        RectTransform outlineRt = outlineGO.GetComponent<RectTransform>();
        outlineRt.anchorMin = new Vector2(0.5f, 0f);
        outlineRt.anchorMax = new Vector2(0.5f, 0f);
        outlineRt.pivot = new Vector2(0.5f, 0f);
        outlineRt.anchoredPosition = new Vector2(0f, (barHeight * 0.5f) + outlineThickness);
        outlineRt.sizeDelta = new Vector2(barWidth + outlineThickness * 2f, barHeight + outlineThickness * 2f);

        GameObject sliderGO = new GameObject("HP_Slider");
        sliderGO.transform.SetParent(outlineGO.transform, false);
        Slider slider = sliderGO.AddComponent<Slider>();
        slider.interactable = false;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;

        RectTransform sliderRt = sliderGO.GetComponent<RectTransform>();
        sliderRt.anchorMin = Vector2.zero;
        sliderRt.anchorMax = Vector2.one;
        sliderRt.offsetMin = new Vector2(outlineThickness, outlineThickness);
        sliderRt.offsetMax = new Vector2(-outlineThickness, -outlineThickness);

        GameObject bg = new GameObject("BG");
        bg.transform.SetParent(sliderGO.transform, false);
        bg.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.15f);
        StretchRect(bg.GetComponent<RectTransform>());

        GameObject fa = new GameObject("FillArea");
        fa.transform.SetParent(sliderGO.transform, false);
        StretchRect(fa.AddComponent<RectTransform>());

        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fa.transform, false);
        fill.AddComponent<Image>().color = new Color(0.9f, 0.15f, 0.15f);
        RectTransform fillRt = fill.GetComponent<RectTransform>();
        StretchRect(fillRt);
        slider.fillRect = fillRt;

        GameObject textGO = new GameObject("HP_Text");
        textGO.transform.SetParent(outlineGO.transform, false);
        hpText = textGO.AddComponent<TextMeshProUGUI>();
        hpText.text = "100 / 100";
        hpText.fontSize = 18f;
        hpText.alignment = TextAlignmentOptions.Center;
        hpText.color = new Color(1f, 0.9f, 0.9f);
        RectTransform textRt = textGO.GetComponent<RectTransform>();
        textRt.anchorMin = new Vector2(0.5f, 1f);
        textRt.anchorMax = new Vector2(0.5f, 1f);
        textRt.pivot = new Vector2(0.5f, 1f);
        textRt.anchoredPosition = new Vector2(0f, 4f);
        textRt.sizeDelta = new Vector2(200f, 24f);

        return slider;
    }

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

    /// <summary>Loads the standalone level-up button sprite (rounded light grey block). Imports as Sprite if needed.</summary>
    static Sprite GetLevelUpButtonSprite()
    {
        if (s_levelUpButtonSprite != null) return s_levelUpButtonSprite;
        const string path = LevelUpButtonSpritePath;
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null && (importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single))
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Point;
            importer.spritePixelsPerUnit = 100f;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
        foreach (Object o in assets)
        {
            if (o is Sprite s)
            {
                s_levelUpButtonSprite = s;
                break;
            }
        }
        return s_levelUpButtonSprite;
    }

    static void MakeUpgradeCard(Transform parent, string name, Vector2 pos,
                                 out Button btn, out TMP_Text cardName,
                                 out TMP_Text cardDesc, out Image cardBg)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        cardBg = go.AddComponent<Image>();
        Sprite levelUpSprite = GetLevelUpButtonSprite();
        if (levelUpSprite != null)
        {
            cardBg.sprite = levelUpSprite;
            cardBg.color = Color.white;
            cardBg.type = Image.Type.Simple;
        }
        else
            cardBg.color = new Color(0.2f, 0.2f, 0.2f);
        btn = go.AddComponent<Button>();

        // Set button colors for better visibility (tint when no sprite; with sprite keep normal/highlight/pressed subtle)
        ColorBlock colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.95f, 0.95f, 0.95f);
        colors.pressedColor = new Color(0.9f, 0.9f, 0.9f);
        btn.colors = colors;

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(220, 220); // Square to match the button, no extra background

        // Text sized to fit inside the button with padding; larger font, wraps in box
        const float textWidth = 190f;
        const float pad = 14f;
        cardName = MakeTMP(go.transform, "CardName", new Vector2(0, 50), new Vector2(textWidth, 44), "Upgrade", 26);
        cardName.fontStyle = FontStyles.Bold;
        cardName.color = Color.black;
        cardName.enableWordWrapping = true;
        cardDesc = MakeTMP(go.transform, "CardDesc", new Vector2(0, -35), new Vector2(textWidth, 100), "Description", 20);
        cardDesc.color = Color.black;
        cardDesc.enableWordWrapping = true;
        cardDesc.alignment = TextAlignmentOptions.Center;
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
