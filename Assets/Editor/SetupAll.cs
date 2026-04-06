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
/// Also rebuilds MainMenu.unity (title + New Game / Load Game) and leaves MainScene open in the editor.
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
    private static RespawnUI        respawnUIComp;
    private static SkillTreeUI      skillTreeUIComp;
    private static LobbyMerchant    lobbyMerchantComp;
    private static Transform[]      spawnPointTransforms;

    // UI
    private static Slider      hpSlider, xpSlider;
    private static TMP_Text    hpText, levelText, waveText, timerText, transitionText;
    private static Image       damageOverlay;
    // Boss HP bar
    private static GameObject  bossHpPanel;
    private static Slider      bossHpSlider;
    private static TMP_Text    bossHpText, bossNameText;

    private const string LevelUpButtonSpritePath = "Assets/Sprites/LevelUpButton.png";
    private static Sprite s_levelUpButtonSprite;
    private static GameObject upgradePanel, gameOverPanel, respawnPanel;
    private static Button     respawnBtn;
    private static Button     card0Btn, card1Btn, card2Btn;
    private static TMP_Text   card0Name, card1Name, card2Name;
    private static TMP_Text   card0Desc, card1Desc, card2Desc;
    private static Image      card0Bg,  card1Bg,  card2Bg;
    private static TMP_Text   upgradeTitle;
    private static TMP_Text   statTime, statWaves, statKills;
    private static Button     restartBtn;

    // ── Entry Point ───────────────────────────────────────────────────────

    // Called from FullSetupOneClick; use that single menu entry instead.
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
        Step3_BuildLobby();
        Step3_BuildTutorial();
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
        Step12_SetupSatanArenaIntro();

        // Ensure only the Lobby level is active by default; arenas are built but inactive.
        GameObject lobbyRoot  = GameObject.Find("=== LEVEL (Lobby) ===");
        GameObject tutorialRoot = GameObject.Find("=== LEVEL (Tutorial) ===");
        GameObject arena1Root = GameObject.Find("=== LEVEL (ProBuilder) ===");
        GameObject arena2Root = GameObject.Find("=== LEVEL (ProBuilder) Arena2 ===");

        if (lobbyRoot  != null) lobbyRoot.SetActive(true);
        if (tutorialRoot != null) tutorialRoot.SetActive(false);
        if (arena1Root != null) arena1Root.SetActive(false);
        if (arena2Root != null) arena2Root.SetActive(false);

        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

        MainMenuBuilder.BuildMainMenuAndReturnTo(mainScenePath);

        Debug.Log("[SetupAll] ✓ Done! Press Play to test.\n" +
                  "If enemies don't navigate walls: Window → AI → Navigation → Bake (retry).");
    }

    // ── Step 1: Clear old setup objects ──────────────────────────────────

    static void Step1_ClearExistingSetup()
    {
        // Remove managers and UI (single instances)
        foreach (string n in new[] { "=== MANAGERS ===", "Canvas_HUD", "=== LEVEL ===", "=== LEVEL (Lobby) ===", "=== LEVEL (Tutorial) ===" })
        {
            GameObject g = GameObject.Find(n);
            if (g != null) Object.DestroyImmediate(g);
        }
        // Remove ALL ProBuilder level roots (there may be more than one)
        GameObject[] roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (GameObject go in roots)
        {
            if (go != null && go.name == "=== LEVEL (ProBuilder) ===")
                Object.DestroyImmediate(go);
            if (go != null && go.name == "=== LEVEL (ProBuilder) Arena2 ===")
                Object.DestroyImmediate(go);
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

    static void Step3_BuildLobby()
    {
        ProBuilderLevelBuilder.BuildLobbyLevel();
        Debug.Log("[SetupAll] Lobby level built.");
    }

    static void Step3_BuildTutorial()
    {
        ProBuilderLevelBuilder.BuildTutorialLevel();
        Debug.Log("[SetupAll] Tutorial level built.");
    }

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
        // Use orthographic camera for crisp pixel art scaling.
        cam.orthographic = true;
        cam.orthographicSize = 12f;
        cam.transform.position = new Vector3(0f, 18f, -14f);
        cam.transform.rotation = Quaternion.Euler(48f, 0f, 0f);

        CameraController cc = cam.GetComponent<CameraController>();
        if (cc == null)
            cc = cam.gameObject.AddComponent<CameraController>();

        // Reset follow settings so camera reliably tracks the player from an angled top-down.
        var so = new SerializedObject(cc);
        so.FindProperty("offset").vector3Value = new Vector3(0f, 18f, -14f);
        so.FindProperty("defaultZoom").floatValue = 1.0f;
        so.FindProperty("followTargetY").boolValue = false;
        so.ApplyModifiedPropertiesWithoutUndo();

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
        if (mgr.GetComponent<LobbyPortalManager>() == null)
            mgr.AddComponent<LobbyPortalManager>();
        if (mgr.GetComponent<TutorialRoomManager>() == null)
            mgr.AddComponent<TutorialRoomManager>();
        if (mgr.GetComponent<ArenaThemeController>() == null)
            mgr.AddComponent<ArenaThemeController>();
        // Optional debug spawner hotkeys (only active in editor)
        if (mgr.GetComponent<DebugSpawnHotkeys>() == null)
            mgr.AddComponent<DebugSpawnHotkeys>();

        // ── Account / Meta progression (persists across sessions) ─────────
        // Must be its own DontDestroyOnLoad singleton; putting it on the
        // MANAGERS object keeps it tidy and easy to find in the hierarchy.
        if (mgr.GetComponent<AccountProgression>() == null)
            mgr.AddComponent<AccountProgression>();
    }

    // ── Step 6: Player ────────────────────────────────────────────────────

    static void Step6_CreatePlayer()
    {
        playerGO = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        playerGO.name = "Player";
        playerGO.tag  = "Player";
        // Spawn in Lobby level center; portal leads into Arena 1
        playerGO.transform.position = new Vector3(0f, 1.1f, 0f);

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

        // ── Boss HP bar (top-center, hidden until a boss is active) ──────
        bossHpPanel = BuildBossHpBar(root, out bossHpSlider, out bossHpText, out bossNameText);
        bossHpPanel.SetActive(false); // HUDManager.ShowBossHP() activates it

        // ── Screen-fade overlay (black, used for arena transition) ────────
        // Renders above everything; ScreenFadeController manages its alpha.
        GameObject fadeGO = new GameObject("ScreenFade");
        fadeGO.transform.SetParent(root, false);
        Image fadeImg = fadeGO.AddComponent<Image>();
        fadeImg.color = new Color(0f, 0f, 0f, 0f); // start transparent
        RectTransform frt = fadeGO.GetComponent<RectTransform>();
        frt.anchorMin = Vector2.zero; frt.anchorMax = Vector2.one; frt.sizeDelta = Vector2.zero;
        fadeGO.SetActive(false); // hidden until first fade

        // Attach controller to the canvas root so it persists easily
        ScreenFadeController fadeCtrl = canvasGO.GetComponent<ScreenFadeController>();
        if (fadeCtrl == null) fadeCtrl = canvasGO.AddComponent<ScreenFadeController>();
        var soFade = new SerializedObject(fadeCtrl);
        soFade.FindProperty("fadeImage").objectReferenceValue = fadeImg;
        soFade.ApplyModifiedPropertiesWithoutUndo();

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

        // ── Respawn panel (shown on death) ────────────────────────────────
        respawnPanel = MakePanel(root, "RespawnPanel", new Color(0f, 0f, 0f, 0.85f));
        TMP_Text respawnTitle = MakeTMP(respawnPanel.transform, "Respawn_Title",
            new Vector2(0, 120), new Vector2(600, 80), "YOU DIED", 64);
        respawnTitle.color = new Color(0.9f, 0.15f, 0.15f);
        respawnTitle.fontStyle = FontStyles.Bold;
        MakeTMP(respawnPanel.transform, "Respawn_Sub",
            new Vector2(0, 40), new Vector2(500, 44), "Your run has ended.", 28);
        respawnBtn = MakeButton(respawnPanel.transform, "RespawnButton",
            new Vector2(0, -60), new Vector2(280, 64), "RESPAWN IN LOBBY", new Color(0.1f, 0.45f, 0.8f));
        respawnUIComp = respawnPanel.AddComponent<RespawnUI>();
        // Start active so Awake() runs and hides it
        respawnPanel.SetActive(true);

        // ── Skill Tree panel (shown by lobby merchant) ─────────────────────
        GameObject skillTreePanel = MakePanel(root, "SkillTreePanel", new Color(0.06f, 0.06f, 0.12f, 0.97f));
        skillTreePanel.transform.SetAsLastSibling();

        // Header
        TMP_Text stTitle = MakeTMP(skillTreePanel.transform, "ST_Title",
            new Vector2(0, 460), new Vector2(700, 64), "SKILL TREE", 52);
        stTitle.fontStyle = FontStyles.Bold;
        stTitle.color     = new Color(1f, 0.85f, 0.3f);

        TMP_Text stLevelText = MakeTMP(skillTreePanel.transform, "ST_Level",
            new Vector2(-420, 400), new Vector2(340, 40), "Account Level 1", 24);
        stLevelText.alignment = TextAlignmentOptions.Left;
        stLevelText.color     = new Color(0.85f, 0.85f, 1f);

        TMP_Text stSPText = MakeTMP(skillTreePanel.transform, "ST_SP",
            new Vector2(420, 400), new Vector2(340, 40), "0 Skill Points", 24);
        stSPText.alignment = TextAlignmentOptions.Right;
        stSPText.color     = new Color(0.4f, 1f, 0.5f);

        Slider stXPBar = MakeSlider(skillTreePanel.transform, "ST_XPBar",
            new Vector2(0, 360), new Vector2(860, 22), new Color(0.25f, 0.55f, 1f));

        TMP_Text stXPLabel = MakeTMP(skillTreePanel.transform, "ST_XPLabel",
            new Vector2(0, 338), new Vector2(860, 22), "0 / 500 Account XP", 14);
        stXPLabel.alignment = TextAlignmentOptions.Center;
        stXPLabel.color     = new Color(0.6f, 0.6f, 0.8f);

        // ── Three track columns — simple anchor-positioned, NO nested layout ──
        // Each column: anchors span ~31% of the panel width, fills from below
        // the header (82% down from top) to above the close button (8% from bottom).
        Transform strikerCol    = BuildSimpleTrackColumn(skillTreePanel.transform,
            "STRIKER",    new Color(1f, 0.4f, 0.3f),  new Color(0.40f, 0.06f, 0.04f, 0.60f),
            new Vector2(0.02f, 0.08f), new Vector2(0.33f, 0.80f));
        Transform survivorCol   = BuildSimpleTrackColumn(skillTreePanel.transform,
            "SURVIVOR",   new Color(0.3f, 1f, 0.45f), new Color(0.04f, 0.30f, 0.06f, 0.60f),
            new Vector2(0.345f, 0.08f), new Vector2(0.655f, 0.80f));
        Transform skirmisherCol = BuildSimpleTrackColumn(skillTreePanel.transform,
            "SKIRMISHER", new Color(0.4f, 0.6f, 1f),  new Color(0.04f, 0.10f, 0.38f, 0.60f),
            new Vector2(0.67f, 0.08f), new Vector2(0.98f, 0.80f));

        Button stCloseBtn = MakeButton(skillTreePanel.transform, "ST_CloseBtn",
            new Vector2(-130, -462), new Vector2(220, 54), "CLOSE", new Color(0.45f, 0.08f, 0.08f));

        Button stResetBtn = MakeButton(skillTreePanel.transform, "ST_ResetBtn",
            new Vector2(130, -462), new Vector2(220, 54), "RESET TREE", new Color(0.50f, 0.30f, 0.05f));

        skillTreeUIComp = skillTreePanel.AddComponent<SkillTreeUI>();
        skillTreePanel.AddComponent<SkillTreeTutorialHook>();

        var soST = new SerializedObject(skillTreeUIComp);
        soST.FindProperty("panel").objectReferenceValue            = skillTreePanel;
        soST.FindProperty("levelText").objectReferenceValue        = stLevelText;
        soST.FindProperty("xpBar").objectReferenceValue            = stXPBar;
        soST.FindProperty("xpLabel").objectReferenceValue          = stXPLabel;
        soST.FindProperty("skillPointsText").objectReferenceValue  = stSPText;
        soST.FindProperty("closeButton").objectReferenceValue      = stCloseBtn;
        soST.FindProperty("resetButton").objectReferenceValue     = stResetBtn;
        soST.FindProperty("strikerColumn").objectReferenceValue    = strikerCol;
        soST.FindProperty("survivorColumn").objectReferenceValue   = survivorCol;
        soST.FindProperty("skirmisherColumn").objectReferenceValue = skirmisherCol;
        soST.ApplyModifiedPropertiesWithoutUndo();

        skillTreePanel.SetActive(false);
    }

    /// <summary>
    /// Builds one track column anchored directly to the panel.
    /// Header (50px) + body with VerticalLayoutGroup.
    /// No ScrollRect — 4 nodes fit without scrolling.
    /// Returns the body Transform where SkillTreeUI adds node buttons at runtime.
    /// </summary>
    static Transform BuildSimpleTrackColumn(Transform panel, string label, Color labelColor,
                                             Color bgColor, Vector2 anchorMin, Vector2 anchorMax)
    {
        // ── Column container — anchored directly to the panel ─────────────
        GameObject colGO = new GameObject($"Track_{label}", typeof(RectTransform));
        colGO.transform.SetParent(panel, false);
        RectTransform colRT = colGO.GetComponent<RectTransform>();
        colRT.anchorMin = anchorMin;
        colRT.anchorMax = anchorMax;
        colRT.offsetMin = Vector2.zero;
        colRT.offsetMax = Vector2.zero;
        Image colBg = colGO.AddComponent<Image>();
        colBg.color = bgColor;

        // ── Header (top 50px) ─────────────────────────────────────────────
        GameObject hdrBgGO = new GameObject("HeaderBg", typeof(RectTransform));
        hdrBgGO.transform.SetParent(colGO.transform, false);
        RectTransform hdrBgRT = hdrBgGO.GetComponent<RectTransform>();
        hdrBgRT.anchorMin = new Vector2(0f, 1f);
        hdrBgRT.anchorMax = new Vector2(1f, 1f);
        hdrBgRT.pivot     = new Vector2(0.5f, 1f);
        hdrBgRT.offsetMin = new Vector2(0f, -50f);
        hdrBgRT.offsetMax = Vector2.zero;
        Image hdrImg = hdrBgGO.AddComponent<Image>();
        hdrImg.color = new Color(0f, 0f, 0f, 0.45f);

        GameObject hdrTxtGO = new GameObject("Label", typeof(RectTransform));
        hdrTxtGO.transform.SetParent(hdrBgGO.transform, false);
        RectTransform hdrTxtRT = hdrTxtGO.GetComponent<RectTransform>();
        hdrTxtRT.anchorMin = Vector2.zero;
        hdrTxtRT.anchorMax = Vector2.one;
        hdrTxtRT.offsetMin = Vector2.zero;
        hdrTxtRT.offsetMax = Vector2.zero;
        TMP_Text hdrTxt = hdrTxtGO.AddComponent<TextMeshProUGUI>();
        hdrTxt.text      = label;
        hdrTxt.fontSize  = 24f;
        hdrTxt.fontStyle = FontStyles.Bold;
        hdrTxt.alignment = TextAlignmentOptions.Center;
        hdrTxt.color     = labelColor;

        // ── Body area below header (full-stretch minus top 50px) ──────────
        // No LayoutGroup — SkillTreeUI positions cards with direct anchors.
        GameObject bodyGO = new GameObject("Body", typeof(RectTransform));
        bodyGO.transform.SetParent(colGO.transform, false);
        RectTransform bodyRT = bodyGO.GetComponent<RectTransform>();
        bodyRT.anchorMin = Vector2.zero;
        bodyRT.anchorMax = Vector2.one;
        bodyRT.offsetMin = new Vector2(6f, 6f);
        bodyRT.offsetMax = new Vector2(-6f, -54f);

        return bodyGO.transform;
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
        GameObject xpOrbPrefab   = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/XPOrb.prefab");
        GameObject satanPrefab   = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy_Satan.prefab");
        GameObject heavyPrefab   = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy_Heavy.prefab");
        GameObject bigBatPrefab  = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy_BigBat.prefab");

        // ── GameManager ───────────────────────────────────────────────────
        Wire(gmComp, "waveManager",    wmComp);
        Wire(gmComp, "hudManager",     hudComp);
        Wire(gmComp, "upgradeUI",      upgradeUIComp);
        Wire(gmComp, "gameOverUI",     gameOverUIComp);
        Wire(gmComp, "respawnUI",      respawnUIComp);
        Wire(gmComp, "logger",         plComp);
        Wire(gmComp, "playerObject",   playerGO);
        Wire(gmComp, "lobbyLevelRoot",  GameObject.Find("=== LEVEL (Lobby) ==="));
        Wire(gmComp, "tutorialLevelRoot", GameObject.Find("=== LEVEL (Tutorial) ==="));
        Wire(gmComp, "arena1LevelRoot", GameObject.Find("=== LEVEL (ProBuilder) ==="));
        Wire(gmComp, "arena2LevelRoot", GameObject.Find("=== LEVEL (ProBuilder) Arena2 ==="));

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
        if (satanPrefab != null)
            Wire(esComp, "satanPrefab", satanPrefab);

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

        // ── LobbyPortalManager (transition from Lobby level into Arena 1) ─
        LobbyPortalManager lobbyMgr = Object.FindFirstObjectByType<LobbyPortalManager>();
        if (lobbyMgr != null)
        {
            var soLobby = new SerializedObject(lobbyMgr);
            soLobby.FindProperty("lobbyRoot").objectReferenceValue = GameObject.Find("=== LEVEL (Lobby) ===");
            soLobby.FindProperty("arena1Root").objectReferenceValue = GameObject.Find("=== LEVEL (ProBuilder) ===");
            soLobby.ApplyModifiedPropertiesWithoutUndo();
        }

        // ── TutorialRoomManager (lobby -> tutorial -> arena1) ───────────────
        TutorialRoomManager tutorialMgr = Object.FindFirstObjectByType<TutorialRoomManager>();
        if (tutorialMgr != null)
        {
            var soTut = new SerializedObject(tutorialMgr);
            soTut.FindProperty("lobbyRoot").objectReferenceValue = GameObject.Find("=== LEVEL (Lobby) ===");
            soTut.FindProperty("tutorialRoot").objectReferenceValue = GameObject.Find("=== LEVEL (Tutorial) ===");
            soTut.FindProperty("arena1Root").objectReferenceValue = GameObject.Find("=== LEVEL (ProBuilder) ===");
            soTut.FindProperty("arena2Root").objectReferenceValue = GameObject.Find("=== LEVEL (ProBuilder) Arena2 ===");
            soTut.FindProperty("tutorialEnemyPrefab").objectReferenceValue = chaserPrefab;
            soTut.FindProperty("tutorialXpOrbPrefab").objectReferenceValue = xpOrbPrefab;
            GameObject tutorialEnemySpawn = GameObject.Find("Tutorial_EnemySpawn");
            if (tutorialEnemySpawn != null)
                soTut.FindProperty("tutorialEnemySpawnPoint").objectReferenceValue = tutorialEnemySpawn.transform;
            soTut.ApplyModifiedPropertiesWithoutUndo();
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
        Wire(hudComp, "bossHpPanel",     bossHpPanel);
        Wire(hudComp, "bossHpSlider",    bossHpSlider);
        Wire(hudComp, "bossHpText",      bossHpText);
        Wire(hudComp, "bossNameText",    bossNameText);

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

        // ── RespawnUI ─────────────────────────────────────────────────────
        Wire(respawnUIComp, "respawnPanel",  respawnPanel);
        Wire(respawnUIComp, "respawnButton", respawnBtn);

        // ── Lobby Merchant NPC ────────────────────────────────────────────
        // Place an NPC in the lobby with a prompt label and link it to SkillTreeUI.
        CreateLobbyMerchant(skillTreeUIComp);

        Debug.Log("[SetupAll] All references wired.");
    }

    static void CreateLobbyMerchant(SkillTreeUI skillTreeUI)
    {
        // Remove any pre-existing merchant
        GameObject existing = GameObject.Find("LobbyMerchant");
        if (existing != null) Object.DestroyImmediate(existing);

        // Find lobby root (may be inactive — must search all roots)
        GameObject lobbyRoot = null;
        foreach (GameObject r in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            if (r.name == "=== LEVEL (Lobby) ===") { lobbyRoot = r; break; }

        // Create empty NPC object (sprite-based, not a primitive)
        GameObject merchant = new GameObject("LobbyMerchant");
        merchant.transform.position = new Vector3(4f, 1f, -4f);

        // Interaction trigger (no visual collider needed — sprite handles visuals)
        SphereCollider trigger = merchant.AddComponent<SphereCollider>();
        trigger.radius    = 2.5f;
        trigger.isTrigger = true;

        // Auto-import merchant sprites as Sprite type if needed
        ConfigureMerchantSprite("Assets/Sprites/NPC/Merchant_Idle.png");
        ConfigureMerchantSprite("Assets/Sprites/NPC/Merchant_Blink.png");

        Sprite idleSprite  = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/NPC/Merchant_Idle.png");
        Sprite blinkSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/NPC/Merchant_Blink.png");

        if (idleSprite == null)
            Debug.LogWarning("[SetupAll] Merchant_Idle.png not found at Assets/Sprites/NPC/.");
        if (blinkSprite == null)
            Debug.LogWarning("[SetupAll] Merchant_Blink.png not found at Assets/Sprites/NPC/.");

        // "Press E" world-space canvas
        GameObject promptGO    = new GameObject("MerchantPrompt");
        promptGO.transform.SetParent(merchant.transform, false);
        promptGO.transform.localPosition = new Vector3(0f, 2f, 0f);
        Canvas wCanvas = promptGO.AddComponent<Canvas>();
        wCanvas.renderMode = RenderMode.WorldSpace;
        wCanvas.worldCamera = Camera.main;
        promptGO.AddComponent<UnityEngine.UI.CanvasScaler>();
        promptGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        RectTransform wRT = promptGO.GetComponent<RectTransform>();
        wRT.sizeDelta = new Vector2(200f, 50f);
        wRT.localScale = Vector3.one * 0.01f;

        GameObject promptText = new GameObject("PromptText", typeof(RectTransform));
        promptText.transform.SetParent(promptGO.transform, false);
        RectTransform pRT = promptText.GetComponent<RectTransform>();
        pRT.anchorMin = Vector2.zero; pRT.anchorMax = Vector2.one;
        pRT.offsetMin = Vector2.zero; pRT.offsetMax = Vector2.zero;
        TMP_Text pTMP = promptText.AddComponent<TextMeshProUGUI>();
        pTMP.text      = "[E] Open Skill Tree";
        pTMP.fontSize  = 14f;
        pTMP.alignment = TextAlignmentOptions.Center;
        pTMP.color     = Color.white;

        // LobbyMerchant component with sprite references
        lobbyMerchantComp = merchant.AddComponent<LobbyMerchant>();
        var soM = new SerializedObject(lobbyMerchantComp);
        soM.FindProperty("skillTreeUI").objectReferenceValue  = skillTreeUI;
        soM.FindProperty("promptText").objectReferenceValue   = pTMP;
        soM.FindProperty("idleSprite").objectReferenceValue   = idleSprite;
        soM.FindProperty("blinkSprite").objectReferenceValue  = blinkSprite;
        soM.ApplyModifiedPropertiesWithoutUndo();

        // Parent under lobby if found (so it hides with the lobby)
        if (lobbyRoot != null)
            merchant.transform.SetParent(lobbyRoot.transform, true);

        Debug.Log("[SetupAll] Lobby merchant created (sprite-based with blink).");
    }

    static void ConfigureMerchantSprite(string path)
    {
        TextureImporter imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp == null) return;
        if (imp.textureType == TextureImporterType.Sprite) return; // already configured
        imp.textureType          = TextureImporterType.Sprite;
        imp.spriteImportMode     = SpriteImportMode.Single;
        imp.filterMode           = FilterMode.Point;
        imp.spritePixelsPerUnit  = 20f;
        imp.mipmapEnabled        = false;
        imp.textureCompression   = TextureImporterCompression.Uncompressed;
        imp.crunchedCompression  = false;
        imp.npotScale            = TextureImporterNPOTScale.None;
        imp.alphaIsTransparency  = true;
        imp.SaveAndReimport();
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
        
        // Wire damageNumberPrefab to Satan boss prefab (not EnemyBase, wired separately)
        string satanPath = "Assets/Prefabs/Enemy_Satan.prefab";
        if (!string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(satanPath)) && damageNumberPrefab != null)
        {
            using (var scope = new PrefabUtility.EditPrefabContentsScope(satanPath))
            {
                if (scope.prefabContentsRoot != null)
                {
                    SatanBossController sb = scope.prefabContentsRoot.GetComponent<SatanBossController>();
                    if (sb != null)
                    {
                        var so = new SerializedObject(sb);
                        so.FindProperty("damageNumberPrefab").objectReferenceValue = damageNumberPrefab;
                        so.ApplyModifiedProperties();
                    }
                }
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

        // Use Children so the player capsule and lobby geometry are excluded from the Arena 1 bake.
        NavMeshSurface surface = levelRoot.AddComponent<NavMeshSurface>();
        surface.collectObjects = CollectObjects.Children;
        surface.useGeometry    = NavMeshCollectGeometry.PhysicsColliders;
        surface.BuildNavMesh();
        Debug.Log("[SetupAll] NavMesh baked (CollectObjects.Children).");
    }

    // ── Step 12: Satan arena intro controller (throne watch + wave-10 jump) ──

    static void Step12_SetupSatanArenaIntro()
    {
        GameObject arena2Root = GameObject.Find("=== LEVEL (ProBuilder) Arena2 ===");
        if (arena2Root == null)
        {
            Debug.LogWarning("[SetupAll] Arena 2 root not found — SatanArenaIntroController skipped.");
            return;
        }

        // Remove any stale controller from a previous setup run.
        Transform existing = arena2Root.transform.Find("Satan_Arena_Intro_Manager");
        if (existing != null) Object.DestroyImmediate(existing.gameObject);

        // Create a dedicated child GO so it activates/deactivates with the Arena 2 root.
        GameObject host = new GameObject("Satan_Arena_Intro_Manager");
        host.transform.SetParent(arena2Root.transform, false);

        SatanArenaIntroController introCtrl = host.AddComponent<SatanArenaIntroController>();
        var so = new SerializedObject(introCtrl);

        // Satan prefab
        GameObject satanPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy_Satan.prefab");
        if (satanPrefab != null)
            so.FindProperty("satanPrefab").objectReferenceValue = satanPrefab;
        else
            Debug.LogWarning("[SetupAll] Enemy_Satan.prefab not found — assign it manually on Satan_Arena_Intro_Manager.");

        // Throne position (matches ProBuilderLevelBuilder: baseTopY=10, throneZ=42)
        // satanWatchPos = (0, baseTopY + 0.9, throneZ - 0.2) = (0, 10.9, 41.8)
        so.FindProperty("thronePosition").vector3Value  = new Vector3(0f, 10.9f, 41.8f);
        so.FindProperty("landingPosition").vector3Value = new Vector3(0f,  1.1f, 12f);

        // Wave 9 (0-based) = displayed as Wave 10
        so.FindProperty("satanWaveTrigger").intValue = 9;
        so.FindProperty("introDelay").floatValue     = 0.8f;

        so.ApplyModifiedPropertiesWithoutUndo();

        Debug.Log("[SetupAll] ✓ SatanArenaIntroController created on 'Satan_Arena_Intro_Manager' inside Arena 2. " +
                  "Satan will sit on the throne (0, 10.9, 41.8) from wave 6 onward and jump down at wave 10.");
    }

    // ── UI Factory Helpers ────────────────────────────────────────────────

    static GameObject BuildBossHpBar(Transform parent,
                                      out Slider slider, out TMP_Text hpText, out TMP_Text nameText)
    {
        // Panel: dark semi-transparent background, anchored to top-center
        GameObject panel = new GameObject("BossHP_Panel");
        panel.transform.SetParent(parent, false);
        Image panelImg = panel.AddComponent<Image>();
        panelImg.color = new Color(0f, 0f, 0f, 0.7f);
        RectTransform panelRt = panel.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.5f, 1f);
        panelRt.anchorMax = new Vector2(0.5f, 1f);
        panelRt.pivot     = new Vector2(0.5f, 1f);
        panelRt.anchoredPosition = new Vector2(0f, -8f);
        panelRt.sizeDelta = new Vector2(600f, 60f);

        // Boss name label
        nameText = MakeTMP(panel.transform, "Boss_Name",
            new Vector2(0f, -8f), new Vector2(580f, 22f), "BOSS", 20f);
        nameText.color = new Color(1f, 0.85f, 0.2f);
        nameText.fontStyle = FontStyles.Bold;

        // The red HP slider
        GameObject sliderGO = new GameObject("BossHP_Slider");
        sliderGO.transform.SetParent(panel.transform, false);
        slider = sliderGO.AddComponent<Slider>();
        slider.interactable = false;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value    = 1f;

        RectTransform sliderRt = sliderGO.GetComponent<RectTransform>();
        sliderRt.anchorMin = new Vector2(0.5f, 0f);
        sliderRt.anchorMax = new Vector2(0.5f, 0f);
        sliderRt.pivot     = new Vector2(0.5f, 0f);
        sliderRt.anchoredPosition = new Vector2(0f, 6f);
        sliderRt.sizeDelta = new Vector2(560f, 22f);

        GameObject bg = new GameObject("BG");
        bg.transform.SetParent(sliderGO.transform, false);
        bg.AddComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f);
        StretchRect(bg.GetComponent<RectTransform>());

        GameObject fa = new GameObject("FillArea");
        fa.transform.SetParent(sliderGO.transform, false);
        StretchRect(fa.AddComponent<RectTransform>());

        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fa.transform, false);
        fill.AddComponent<Image>().color = new Color(0.85f, 0.1f, 0.1f);
        RectTransform fillRt = fill.GetComponent<RectTransform>();
        StretchRect(fillRt);
        slider.fillRect = fillRt;

        // HP text over slider
        hpText = MakeTMP(sliderGO.transform, "BossHP_Text",
            Vector2.zero, new Vector2(560f, 22f), "", 14f);
        hpText.color = Color.white;

        return panel;
    }

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
