using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Editor tool: CS4483 → 2 - Setup Scene Managers
/// Creates all manager GameObjects, the Canvas UI, Camera rig, and a
/// placeholder Player in the active scene.
/// Run AFTER BuildLevel.
/// </summary>
public static class SceneSetup
{
    [MenuItem("CS4483/2 - Setup Scene Managers & UI")]
    public static void SetupScene()
    {
        SetupCamera();
        SetupManagers();
        SetupPlayer();
        SetupCanvas();
        SetupGateDoor();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("[SceneSetup] ✓ Scene setup complete. Wire inspector references (see SETUP_INSTRUCTIONS.md).");
    }

    // ── Camera ────────────────────────────────────────────────────────────

    static void SetupCamera()
    {
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            GameObject camGO = new GameObject("Main Camera");
            camGO.tag = "MainCamera";
            mainCam = camGO.AddComponent<Camera>();
        }
        // Position and angle: top-down with slight forward tilt
        mainCam.transform.position = new Vector3(0, 16f, -9f);
        mainCam.transform.rotation = Quaternion.Euler(60f, 0f, 0f);
        mainCam.fieldOfView = 60f;

        if (mainCam.GetComponent<CameraController>() == null)
            mainCam.gameObject.AddComponent<CameraController>();

        // Subtle fog
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.1f, 0.1f, 0.1f);
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 30f;
        RenderSettings.fogEndDistance   = 60f;
    }

    // ── Managers ──────────────────────────────────────────────────────────

    static void SetupManagers()
    {
        // All managers on one root GO to keep the hierarchy clean
        GameObject mgr = new GameObject("=== MANAGERS ===");

        GameManager    gm = mgr.AddComponent<GameManager>();
        WaveManager    wm = mgr.AddComponent<WaveManager>();
        EnemySpawner   es = mgr.AddComponent<EnemySpawner>();
        HUDManager     hd = mgr.AddComponent<HUDManager>();
        UpgradeManager um = mgr.AddComponent<UpgradeManager>();
        PlaytestLogger pl = mgr.AddComponent<PlaytestLogger>();

        // Wire WaveManager -> EnemySpawner reference in the spawner
        // (actual inspector wiring for prefab refs done manually)
        Debug.Log("[SceneSetup] Managers created. Open Inspector and wire all prefab/reference fields.");
    }

    // ── Player ────────────────────────────────────────────────────────────

    static void SetupPlayer()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Debug.Log("[SceneSetup] Player already exists, skipping creation.");
            return;
        }

        // Capsule body
        player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player";
        player.tag  = "Player";
        player.transform.position = new Vector3(0, 1.1f, 0);

        // Remove default capsule collider and add CharacterController
        Object.DestroyImmediate(player.GetComponent<CapsuleCollider>());
        CharacterController cc = player.AddComponent<CharacterController>();
        cc.height = 2f;
        cc.radius = 0.4f;
        cc.center = Vector3.zero;

        // Player components
        player.AddComponent<PlayerController>();
        player.AddComponent<PlayerHealth>();
        player.AddComponent<PlayerWeapon>();
        player.AddComponent<PlayerXP>();

        // Blue material for player
        Renderer r = player.GetComponent<Renderer>();
        if (r != null)
        {
            Material mat = new Material(Shader.Find("Standard"))
                { color = new Color(0.2f, 0.4f, 0.9f) };
            r.material = mat;
        }

        // Fire point child
        GameObject fp = new GameObject("FirePoint");
        fp.transform.SetParent(player.transform);
        fp.transform.localPosition = new Vector3(0f, 0.5f, 0.6f);

        Debug.Log("[SceneSetup] Player created. Assign FirePoint to PlayerWeapon in Inspector.");
    }

    // ── Canvas / HUD ──────────────────────────────────────────────────────

    static void SetupCanvas()
    {
        // Main HUD canvas
        GameObject canvasGO = new GameObject("Canvas_HUD");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        // -- HP Bar --
        CreateSliderInCanvas(canvasGO.transform, "HP_Slider",
            new Vector2(-350f, -450f), new Vector2(350f, 35f),
            new Color(1f, 0.2f, 0.2f));

        // HP text
        CreateTMPText(canvasGO.transform, "HP_Text",
            new Vector2(-350f, -420f), new Vector2(150f, 30f), "100 / 100");

        // -- XP Bar --
        CreateSliderInCanvas(canvasGO.transform, "XP_Slider",
            new Vector2(300f, -160f), new Vector2(250f, 20f),
            new Color(0.2f, 0.8f, 0.2f));

        // Level text
        CreateTMPText(canvasGO.transform, "Level_Text",
            new Vector2(300f, -140f), new Vector2(80f, 20f), "Lv 1");

        // -- Wave / Timer --
        CreateTMPText(canvasGO.transform, "Wave_Text",
            new Vector2(0f, 170f), new Vector2(200f, 30f), "Wave 1");
        CreateTMPText(canvasGO.transform, "Timer_Text",
            new Vector2(0f, 140f), new Vector2(150f, 25f), "60s");

        // -- Transition Banner --
        TMP_Text trans = CreateTMPText(canvasGO.transform, "Transition_Text",
            new Vector2(0f, 30f), new Vector2(600f, 60f), "");
        trans.fontSize = 36;
        trans.color    = Color.yellow;

        // -- Damage Overlay (full screen red flash) --
        GameObject overlay = new GameObject("DamageOverlay");
        overlay.transform.SetParent(canvasGO.transform, false);
        Image img = overlay.AddComponent<Image>();
        img.color = new Color(1f, 0f, 0f, 0f);
        RectTransform rt = overlay.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;

        // -- Upgrade Panel --
        CreateUpgradePanel(canvasGO.transform);

        // -- Game Over Panel --
        CreateGameOverPanel(canvasGO.transform);

        Debug.Log("[SceneSetup] Canvas created. Wire TMP_Text and Slider fields in HUDManager/UpgradeUI/GameOverUI.");
    }

    static Slider CreateSliderInCanvas(Transform parent, string name, Vector2 anchoredPos,
                                       Vector2 size, Color fillColor)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        Slider slider = go.AddComponent<Slider>();
        slider.minValue  = 0f;
        slider.maxValue  = 1f;
        slider.value     = 1f;
        slider.interactable = false;

        // Background
        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(go.transform, false);
        Image bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.15f, 0.15f, 0.15f);
        RectTransform bgRt = bg.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.sizeDelta = Vector2.zero;

        // Fill Area
        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(go.transform, false);
        RectTransform faRt = fillArea.AddComponent<RectTransform>();
        faRt.anchorMin = Vector2.zero;
        faRt.anchorMax = Vector2.one;
        faRt.sizeDelta = Vector2.zero;

        // Fill
        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        Image fillImg = fill.AddComponent<Image>();
        fillImg.color = fillColor;
        RectTransform fillRt = fill.GetComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.sizeDelta = Vector2.zero;

        slider.fillRect = fillRt;

        // Position the whole slider
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        return slider;
    }

    static TMP_Text CreateTMPText(Transform parent, string name, Vector2 anchoredPos,
                                  Vector2 size, string defaultText)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        TMP_Text txt = go.AddComponent<TextMeshProUGUI>();
        txt.text      = defaultText;
        txt.fontSize  = 18;
        txt.alignment = TextAlignmentOptions.Center;
        txt.color     = Color.white;

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        return txt;
    }

    static void CreateUpgradePanel(Transform parent)
    {
        GameObject panel = new GameObject("UpgradePanel");
        panel.transform.SetParent(parent, false);
        Image panelBg = panel.AddComponent<Image>();
        panelBg.color = new Color(0f, 0f, 0f, 0.75f);
        RectTransform pRt = panel.GetComponent<RectTransform>();
        pRt.anchorMin = Vector2.zero;
        pRt.anchorMax = Vector2.one;
        pRt.sizeDelta = Vector2.zero;

        CreateTMPText(panel.transform, "UpgradeTitle",
            new Vector2(0, 80), new Vector2(600, 40), "Level Up! Choose an Upgrade");

        // 3 upgrade card buttons
        Vector2[] cardPositions = {
            new Vector2(-230, -20), new Vector2(0, -20), new Vector2(230, -20)
        };
        string[] cardNames = { "Card0", "Card1", "Card2" };
        for (int i = 0; i < 3; i++)
            CreateUpgradeCard(panel.transform, cardNames[i], cardPositions[i]);

        panel.SetActive(false); // UpgradeUI.Show() activates it

        // Add UpgradeUI script to this panel
        panel.AddComponent<UpgradeUI>();
    }

    static void CreateUpgradeCard(Transform parent, string name, Vector2 pos)
    {
        GameObject card = new GameObject(name);
        card.transform.SetParent(parent, false);
        Image bg = card.AddComponent<Image>();
        bg.color = new Color(0.3f, 0.3f, 0.3f);
        Button btn = card.AddComponent<Button>();

        RectTransform rt = card.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(200, 120);

        CreateTMPText(card.transform, "CardName",
            new Vector2(0, 30), new Vector2(190, 30), "Upgrade Name").fontSize = 16;
        CreateTMPText(card.transform, "CardDesc",
            new Vector2(0, -10), new Vector2(190, 50), "Description text").fontSize = 13;
    }

    static void CreateGameOverPanel(Transform parent)
    {
        GameObject panel = new GameObject("GameOverPanel");
        panel.transform.SetParent(parent, false);
        Image bg = panel.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.85f);
        RectTransform rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;

        TMP_Text title = CreateTMPText(panel.transform, "GameOver_Title",
            new Vector2(0, 80), new Vector2(400, 50), "GAME OVER");
        title.fontSize = 42;
        title.color    = new Color(0.9f, 0.2f, 0.2f);

        CreateTMPText(panel.transform, "Stats_Time",   new Vector2(0,  20), new Vector2(350, 30), "Time Survived: 00:00");
        CreateTMPText(panel.transform, "Stats_Waves",  new Vector2(0, -10), new Vector2(350, 30), "Waves Cleared: 0");
        CreateTMPText(panel.transform, "Stats_Kills",  new Vector2(0, -40), new Vector2(350, 30), "Total Kills: 0");

        // Restart button
        GameObject btnGO = new GameObject("RestartButton");
        btnGO.transform.SetParent(panel.transform, false);
        Image btnBg = btnGO.AddComponent<Image>();
        btnBg.color = new Color(0.2f, 0.6f, 0.2f);
        btnGO.AddComponent<Button>();
        RectTransform brt = btnGO.GetComponent<RectTransform>();
        brt.anchorMin = new Vector2(0.5f, 0.5f);
        brt.anchorMax = new Vector2(0.5f, 0.5f);
        brt.anchoredPosition = new Vector2(0, -90);
        brt.sizeDelta = new Vector2(200, 50);
        CreateTMPText(btnGO.transform, "RestartLabel",
            Vector2.zero, new Vector2(200, 50), "RESTART").fontSize = 22;

        panel.SetActive(false);

        // Add GameOverUI script to this panel
        panel.AddComponent<GameOverUI>();
    }

    // ── Gate Door ─────────────────────────────────────────────────────────

    static void SetupGateDoor()
    {
        GameObject gate = GameObject.Find("GateDoor");
        if (gate != null && gate.GetComponent<GateDoor>() == null)
        {
            gate.AddComponent<GateDoor>();
            Debug.Log("[SceneSetup] GateDoor component added.");
        }
    }
}
