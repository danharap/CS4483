using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Tutorial room flow controller.
/// Runs only after entering the tutorial room from lobby.
/// </summary>
public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    [Header("UI (auto-created if missing)")]
    [SerializeField] private TMP_Text   tutorialText;
    [SerializeField] private GameObject tutorialTextRoot;
    [SerializeField] private TMP_Text   tutorialSpeakerText;
    [SerializeField] private Image      tutorialPortraitImage;

    [Header("Dialogue Portrait")]
    [Tooltip("Portrait shown in the dialogue box. Auto-loaded from Resources/Portraits/TutorialNPC_Portrait if empty.")]
    [SerializeField] private Sprite npcPortrait;
    [SerializeField] private string npcSpeakerName = "The Watcher";

    private RawImage tutorialPortraitRaw;
    private Texture2D texGuidePortrait;
    private Texture2D texDevilPortrait;

    [Header("Timing")]
    [SerializeField] private float introLineDuration = 1.8f;
    [SerializeField] private float introGap = 0.35f;

    [Header("Tutorial")]
    [SerializeField] private int tutorialOrbTarget = 6;
    [SerializeField] private float tutorialEnemyHpMultiplier = 2f;

    private enum TutorialPhase
    {
        Inactive,
        Intro,
        MoveToGate1,
        AwaitFirstGatePass,
        DashTutorial,
        Combat,
        AwaitSecondGatePass,
        CollectOrbs,
        PickUpgrade,
        MedkitTutorial,
        SkillTreeExplanation,
        TrapExplanation,
        NpcSection,
        Complete
    }

    private TutorialPhase phase = TutorialPhase.Inactive;

    [Header("Medkit Tutorial")]
    [Tooltip("HealthPack prefab to spawn for the medkit tutorial step. Wired by TutorialRoomManager.")]
    [SerializeField] public GameObject healthPackPrefab;
    [Tooltip("Damage dealt before the tutorial medkit spawns. Medkit heal amount is matched so the player returns to full HP.")]
    [SerializeField] private float tutorialMedkitLessonDamage = 20f;

    private bool tutorialRoomStarted;
    private bool moveInputSeen;
    private int tutorialOrbsCollected;

    private GameObject tutorialEnemyPrefab;
    private Transform tutorialEnemySpawnPoint;
    private Transform tutorialMedkitSpawnPoint;
    private GameObject tutorialXpOrbPrefab;
    private bool combatSpawnQueued;

    private TutorialGate gate1;
    private TutorialGate gate2;
    private TutorialGate gate3;

    private static readonly string[] IntroLines =
    {
        "You are awake.",
        "Good. That means you are still useful.",
        "You were taken.",
        "No names. No past. Only purpose.",
        "This is the Colosseum.",
        "They watch from above.",
        "They cheer. They bet. They wait for you to fail.",
        "You will fight.",
        "You will survive.",
        "Or you will be replaced.",
        "Move."
    };

    // Register once (before first scene loads) to create a fresh instance on every scene load.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneCallback()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (Instance != null) return;
        GameObject go = new GameObject("TutorialManager");
        go.AddComponent<TutorialManager>();
    }

    void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) { Destroy(gameObject); return; }

        EnsureTutorialUI();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void BeginTutorialRoom(GameObject enemyPrefab, Transform enemySpawnPoint, GameObject xpOrbPrefab)
    {
        StopAllCoroutines();

        tutorialRoomStarted = true;
        tutorialEnemyPrefab = enemyPrefab;
        tutorialEnemySpawnPoint = enemySpawnPoint;
        tutorialXpOrbPrefab = xpOrbPrefab;
        moveInputSeen = false;
        tutorialOrbsCollected = 0;
        combatSpawnQueued = false;

        ResolveTutorialRefs();
        CloseTutorialGates();

        phase = TutorialPhase.Intro;
        StartCoroutine(PlayIntroThenStart());
    }

    public void EndTutorialRoom()
    {
        StopAllCoroutines();
        tutorialRoomStarted = false;
        phase = TutorialPhase.Inactive;
        ApplyDialogueSpeaker(devil: false);
        ShowText(false);

        PlayerController pc = GameManager.Instance?.PlayerController;
        if (pc != null) pc.SetInputLocked(false);
    }

    /// <summary>
    /// Called when the player steps through the tutorial exit portal back to the lobby.
    /// Ends the tutorial and briefly shows a welcome message directing them to the Guide NPC.
    /// </summary>
    public void TransitionToLobby()
    {
        StopAllCoroutines();
        tutorialRoomStarted = false;
        phase = TutorialPhase.Complete;

        PlayerController pc = GameManager.Instance?.PlayerController;
        if (pc != null) pc.SetInputLocked(false);

        StartCoroutine(ShowLobbyReturnMessage());
    }

    private IEnumerator ShowLobbyReturnMessage()
    {
        ShowText(true);
        ApplyDialogueSpeaker(devil: false);
        SetText("Speak with the Guide (Press E) if you have any questions, then head into the arena when you're ready!");
        yield return new WaitForSecondsRealtime(5f);
        ShowText(false);
        phase = TutorialPhase.Inactive;
    }

    public void NotifyTrigger(TutorialTriggerType trigger)
    {
        if (!tutorialRoomStarted) return;

        if (trigger == TutorialTriggerType.Move && phase == TutorialPhase.MoveToGate1)
            moveInputSeen = true;

        if (trigger == TutorialTriggerType.KillEnemy && phase == TutorialPhase.Combat)
            OnCombatEnemyKilled();

        if (trigger == TutorialTriggerType.SelectUpgrade && phase == TutorialPhase.PickUpgrade)
        {
            // Gate 3 stays CLOSED until the medkit is picked up.
            phase = TutorialPhase.MedkitTutorial;
            StartCoroutine(PlayMedkitTutorial());
        }
    }

    public void OnReachedFirstGateApproach()
    {
        if (!tutorialRoomStarted || phase != TutorialPhase.MoveToGate1) return;

        if (!moveInputSeen)
        {
            SetText("Move with WASD.");
            return;
        }

        OpenGate(gate1);
        phase = TutorialPhase.AwaitFirstGatePass;
        SetText("Walk through the opened gate.");
    }

    public void OnPassedFirstGate()
    {
        if (!tutorialRoomStarted || phase != TutorialPhase.AwaitFirstGatePass) return;
        if (combatSpawnQueued) return;

        combatSpawnQueued = true;
        phase = TutorialPhase.DashTutorial;
        StartCoroutine(PlayDashTutorial());
    }

    public void OnPassedSecondGate()
    {
        if (!tutorialRoomStarted || phase != TutorialPhase.AwaitSecondGatePass) return;

        phase = TutorialPhase.CollectOrbs;
        SpawnTutorialOrbs();
        SetText("Pick up all xp orbs to get an upgrade.");
    }

    public void OnEnteredOrbArea()
    {
        if (phase == TutorialPhase.AwaitSecondGatePass)
            OnPassedSecondGate();
    }

    public void NotifyTutorialOrbCollected()
    {
        if (!tutorialRoomStarted || phase != TutorialPhase.CollectOrbs) return;

        tutorialOrbsCollected++;
        if (tutorialOrbsCollected >= tutorialOrbTarget)
        {
            phase = TutorialPhase.PickUpgrade;
            SetText("Choose an upgrade.");
            GameManager.Instance?.PauseForUpgrade(false);
        }
        else
        {
            int remaining = tutorialOrbTarget - tutorialOrbsCollected;
            SetText($"Pick up all xp orbs to get an upgrade. ({remaining} left)");
        }
    }

    public void OnUpgradeOpened() { }
    public void OnSkillTreeOpened() { }

    public void ShowTemporaryMessage(string message, float duration = 3f)
    {
        if (!tutorialRoomStarted) return;
        if (phase == TutorialPhase.SkillTreeExplanation) return;
        StartCoroutine(ShowMessageRoutine(message, duration));
    }

    // ── Dash Tutorial ─────────────────────────────────────────────────────

    private IEnumerator PlayDashTutorial()
    {
        ShowText(true);
        ApplyDialogueSpeaker(devil: false);
        SetText("Before you fight — learn to move like your life depends on it.");
        yield return new WaitForSecondsRealtime(2.2f);
        SetText("Press Left Shift to dash. Use it to dodge attacks and close gaps quickly.");
        yield return new WaitForSecondsRealtime(1.8f);
        SetText("Try it now. Press Left Shift.");

        // Wait for the player to dash. Time out after 25 s so they can't get stuck.
        PlayerController pc = GameManager.Instance?.PlayerController
                              ?? FindFirstObjectByType<PlayerController>();
        float waited = 0f;
        bool dashSeen = false;
        while (waited < 25f && !dashSeen)
        {
            if (pc != null && pc.IsDashing)
                dashSeen = true;
            waited += Time.deltaTime;
            yield return null;
        }

        if (dashSeen)
        {
            SetText("Good. Keep moving — a stationary fighter is a dead fighter.");
            yield return new WaitForSecondsRealtime(2.0f);
        }

        // Hand off to the combat phase — spawn the enemy with a short delay.
        phase = TutorialPhase.Combat;
        SetText("Left click and aim your mouse to shoot.");
        StartCoroutine(SpawnCombatEnemyAfterDelay(2.5f));
    }

    // ── Medkit Tutorial ───────────────────────────────────────────────────

    private IEnumerator PlayMedkitTutorial()
    {
        ShowText(true);
        ApplyDialogueSpeaker(devil: false);

        // 1. Let the upgrade screen fully close before doing anything visible.
        yield return new WaitForSecondsRealtime(0.5f);

        // 2. Deal a fixed amount of damage so the medkit (same heal amount) brings them back to full.
        // Avoids leaving the tutorial at partial HP for their first arena run.
        PlayerHealth ph = GameManager.Instance?.PlayerController?.GetComponent<PlayerHealth>()
                          ?? FindFirstObjectByType<PlayerHealth>();
        if (ph != null)
        {
            float dmg = tutorialMedkitLessonDamage;
            // Never lethal if HP was already lowered by something else.
            dmg = Mathf.Min(dmg, Mathf.Max(0f, ph.CurrentHP - 1f));
            if (dmg > 0f)
                ph.TakeDamage(dmg);
        }

        SetText("You fought. You survived. But in the arena, that will not always be enough.");
        yield return new WaitForSecondsRealtime(2.5f);
        SetText("Look. Your body is already failing you.");
        yield return new WaitForSecondsRealtime(2.2f);

        // 3. Spawn the medkit.
        GameObject spawnedPack = null;
        Vector3 spawnPos = tutorialMedkitSpawnPoint != null
            ? tutorialMedkitSpawnPoint.position
            : new Vector3(-16f, 0.5f, FindCz());

        if (healthPackPrefab != null)
        {
            spawnedPack = Instantiate(healthPackPrefab, spawnPos, Quaternion.identity);
            HealthPack hp = spawnedPack.GetComponent<HealthPack>();
            if (hp != null)
            {
                hp.lifetime = 120f;
                // Match damage taken so pickup returns player to full HP (same as prefab default 20).
                hp.healAmount = tutorialMedkitLessonDamage;
            }
        }

        // 4. Prompt player to pick it up.
        SetText("A medkit has dropped ahead. Walk over it — it will restore your health.");
        yield return new WaitForSecondsRealtime(2.0f);
        SetText("Pick it up.");

        // 5. Wait for the medkit to be collected (HealthPack destroys itself on pickup).
        yield return StartCoroutine(WaitForMedkitPickup(spawnedPack));

        // Top off HP so the first real run never starts below max (medkit should already restore full).
        if (ph != null)
            ph.HealHP(ph.maxHP);

        // 6. Explain the mechanic.
        SetText("Good.");
        yield return new WaitForSecondsRealtime(1.5f);
        SetText("Enemies inside the arena have a small chance to drop medkits when they fall.");
        yield return new WaitForSecondsRealtime(2.8f);
        SetText("Do not count on them. But when they appear — use them.");
        yield return new WaitForSecondsRealtime(2.5f);

        // 7. Open gate 3 and hand off to the trap demo section (then devil, then portal).
        OpenGate(gate3);
        phase = TutorialPhase.TrapExplanation;
        StartCoroutine(PlayTrapExplanation());
    }

    /// <summary>
    /// Polls until the medkit GameObject is destroyed (i.e. the player picked it up).
    /// Falls back gracefully if the prefab was null or already gone.
    /// </summary>
    private IEnumerator WaitForMedkitPickup(GameObject medkitGo)
    {
        if (medkitGo == null) yield break;

        while (medkitGo != null)
            yield return null;
    }

    /// <summary>Returns the tutorial corridor center Z (matches TutorialPrisonLayout).</summary>
    private float FindCz()
    {
        // TutorialPrisonLayout.CorridorCenterZ is the authoritative value.
        // Fallback to -40 (default) if the type is unavailable at runtime.
        return TutorialPrisonLayout.CorridorCenterZ;
    }

    private IEnumerator PlaySkillTreeDialogue()
    {
        ShowText(true);
        ApplyDialogueSpeaker(devil: true);

        string[] lines =
        {
            "Hold on — before you sprint back into the meat grinder, listen.",
            "Every run earns you Account XP. The longer you survive, the more you bank.",
            "When your account levels up, you get a Skill Point.",
            "Back in the lobby, find the red devil by the Skill Tree.",
            "Spend those points on permanent upgrades. They carry into every future run — and make the next trip down here a little less one-sided.",
            "I'll see you by the Skill Tree. Now move — portal's waiting."
        };

        const float lineDur = 2.35f;
        const float gap     = 0.28f;
        foreach (string line in lines)
        {
            SetText(line);
            yield return new WaitForSecondsRealtime(lineDur);
            yield return new WaitForSecondsRealtime(gap);
        }

        ApplyDialogueSpeaker(devil: false);
        phase = TutorialPhase.NpcSection;
        SetText("Head through the portal to return to the lobby.");
    }

    // ── Trap Explanation ──────────────────────────────────────────────────

    private IEnumerator PlayTrapExplanation()
    {
        ShowText(true);
        ApplyDialogueSpeaker(devil: false);

        // Spawn demo traps in the corridor just past gate 3 (x=-10).
        // SpikeTrap on the left, FlameTrap on the right — side by side so the
        // player can see both warning rings at a safe distance.
        float cz            = FindCz();
        Vector3 spikePos    = new Vector3(-5.5f, 0.05f, cz - 1.2f);
        Vector3 flamePos    = new Vector3(-3.0f, 0.05f, cz + 1.2f);

        GameObject spikeGo  = SpawnDemoTrap<SpikeTrap>(spikePos);
        GameObject flameGo  = SpawnDemoTrap<FlameTrap>(flamePos);

        // ── Spike trap section ────────────────────────────────────────────
        SetText("Before you go — look at the floor ahead.");
        yield return new WaitForSecondsRealtime(2.2f);

        SetText("That pulsing red ring on the ground is a spike trap warning.\nStep out of the circle before it closes.");
        yield return new WaitForSecondsRealtime(3.0f);

        SetText("When the ring disappears, spikes erupt. If you are still inside, you take damage.");
        yield return new WaitForSecondsRealtime(3.0f);

        SetText("Watch it cycle. The timing is always the same — short warning, brief spike, then it resets.");
        yield return new WaitForSecondsRealtime(4.5f);

        // ── Flame trap section ────────────────────────────────────────────
        SetText("The orange ring beside it is a flame trap. Same idea — different danger.");
        yield return new WaitForSecondsRealtime(2.8f);

        SetText("Flames stay active longer than spikes. Standing inside burns you repeatedly.");
        yield return new WaitForSecondsRealtime(2.8f);

        SetText("One second in the fire will not kill you. Five seconds will.\nSee the ring — move.");
        yield return new WaitForSecondsRealtime(3.0f);

        SetText("The arena is full of them. Watch the ground, not just the enemies.");
        yield return new WaitForSecondsRealtime(2.8f);

        // Clean up demo traps before the devil speaks.
        if (spikeGo != null) Object.Destroy(spikeGo);
        if (flameGo != null) Object.Destroy(flameGo);

        // ── Hand off to devil's skill-tree speech ─────────────────────────
        phase = TutorialPhase.SkillTreeExplanation;
        StartCoroutine(PlaySkillTreeDialogue());
    }

    /// <summary>
    /// Spawns a demo trap GameObject, configures its cycle to be snappier so the
    /// player sees the warning ring and activation within a few seconds.
    /// </summary>
    private static GameObject SpawnDemoTrap<T>(Vector3 position) where T : ArenaTrap
    {
        GameObject go = new GameObject($"DemoTrap_{typeof(T).Name}");
        go.transform.position = position;

        // Sphere trigger collider — needed by ArenaTrap damage checks but set
        // to a tiny radius so the demo trap cannot hurt the player unless they
        // walk directly into it.
        SphereCollider col = go.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius    = 0.5f;

        T trap = go.AddComponent<T>();

        // Shorten the initial delay so the warn ring appears quickly.
        // We access via ArenaTrap's protected field through reflection-free approach:
        // ArenaTrap.initialDelay is set in Start(), so override it via serialized
        // defaults isn't possible at Add-time — instead set the component values
        // before Start() runs via a MonoBehaviour that resets them on Awake.
        go.AddComponent<DemoTrapFastCycle>();

        return go;
    }

    private IEnumerator PlayIntroThenStart()
    {
        PlayerController pc = GameManager.Instance?.PlayerController;
        if (pc != null) pc.SetInputLocked(true);

        ShowText(true);
        ApplyDialogueSpeaker(devil: false);
        for (int i = 0; i < IntroLines.Length; i++)
        {
            SetText(IntroLines[i]);
            yield return new WaitForSecondsRealtime(introLineDuration);
            yield return new WaitForSecondsRealtime(introGap);
        }

        if (pc != null) pc.SetInputLocked(false);

        phase = TutorialPhase.MoveToGate1;
        SetText("Move with WASD.");
    }

    private IEnumerator SpawnCombatEnemyAfterDelay(float seconds)
    {
        yield return new WaitForSecondsRealtime(seconds);

        GameObject enemyGo = null;
        EnemySpawner spawner = FindFirstObjectByType<EnemySpawner>();
        if (spawner != null && tutorialEnemySpawnPoint != null)
            enemyGo = spawner.SpawnFirstArenaEnemyAt(tutorialEnemySpawnPoint.position);

        if (enemyGo == null && tutorialEnemyPrefab != null && tutorialEnemySpawnPoint != null)
            enemyGo = Instantiate(tutorialEnemyPrefab, tutorialEnemySpawnPoint.position, Quaternion.identity);

        if (enemyGo != null)
        {
            EnemyBase enemy = enemyGo.GetComponent<EnemyBase>();
            if (enemy != null)
                enemy.ScaleMaxHP(tutorialEnemyHpMultiplier);
        }
    }

    private void OnCombatEnemyKilled()
    {
        OpenGate(gate2);
        phase = TutorialPhase.AwaitSecondGatePass;
        SetText("Continue down the hall.");
    }

    private void SpawnTutorialOrbs()
    {
        Transform root = FindChildByName("Tutorial_OrbSpawns");
        if (root == null || tutorialXpOrbPrefab == null) return;

        foreach (Transform spawn in root)
        {
            GameObject orb = Instantiate(tutorialXpOrbPrefab, spawn.position, Quaternion.identity);
            XPOrb xp = orb.GetComponent<XPOrb>();
            if (xp != null)
            {
                xp.MarkAsTutorialOrb(true);
                xp.xpValue = 0f;
            }
        }
    }

    private void ResolveTutorialRefs()
    {
        gate1 = FindGate("Tutorial_Gate_1");
        gate2 = FindGate("Tutorial_Gate_2");
        gate3 = FindGate("Tutorial_Gate_3");

        if (tutorialEnemySpawnPoint == null)
        {
            Transform t = FindChildByName("Tutorial_EnemySpawn");
            if (t != null) tutorialEnemySpawnPoint = t;
        }

        if (tutorialMedkitSpawnPoint == null)
        {
            Transform t = FindChildByName("Tutorial_MedkitSpawn");
            if (t != null) tutorialMedkitSpawnPoint = t;
        }

        // Editor-time prefab fallback so the medkit tutorial works without running SetupAll.
#if UNITY_EDITOR
        if (healthPackPrefab == null)
            healthPackPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/HealthPack.prefab");
#endif
    }

    private TutorialGate FindGate(string name)
    {
        Transform t = FindChildByName(name);
        return t != null ? t.GetComponent<TutorialGate>() : null;
    }

    private Transform FindChildByName(string name)
    {
        GameObject root = GameObject.Find("=== LEVEL (Tutorial) ===");
        if (root == null) return null;
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            if (t.name == name) return t;
        return null;
    }

    private void CloseTutorialGates()
    {
        if (gate1 != null) gate1.CloseGate();
        if (gate2 != null) gate2.CloseGate();
        if (gate3 != null) gate3.CloseGate();
    }

    private void OpenGate(TutorialGate gate)
    {
        if (gate != null) gate.OpenGate();
    }

    private IEnumerator ShowMessageRoutine(string message, float duration)
    {
        ShowText(true);
        string restore = tutorialText != null ? tutorialText.text : string.Empty;
        SetText(message);
        yield return new WaitForSecondsRealtime(duration);

        if (tutorialRoomStarted && phase != TutorialPhase.Inactive)
            SetText(restore);
        else
            ShowText(false);
    }

    private void EnsureTutorialUI()
    {
        if (tutorialText != null)
            return;

        GameObject canvasGo = GameObject.Find("Canvas_HUD");
        if (canvasGo == null)
        {
            Canvas c = FindFirstObjectByType<Canvas>();
            if (c != null) canvasGo = c.gameObject;
        }
        if (canvasGo == null) return;

        // Load portrait — prefer the Guide NPC sprite asset directly (editor), fall back to Resources.
        if (npcPortrait == null)
        {
#if UNITY_EDITOR
            npcPortrait = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Guide NPC/0.png");
#endif
        }
        if (npcPortrait == null)
            npcPortrait = Resources.Load<Sprite>("Portraits/TutorialNPC_Portrait");

        // ── Stardew-style bottom dialogue panel ───────────────────────────
        const float panelH     = 200f;
        const float panelW     = 1260f;
        const float portraitSz = 164f;
        const float edgePad    = 18f;

        tutorialTextRoot = new GameObject("TutorialDialoguePanel");
        tutorialTextRoot.transform.SetParent(canvasGo.transform, false);

        Image panelBg = tutorialTextRoot.AddComponent<Image>();
        panelBg.color = new Color(0.08f, 0.06f, 0.10f, 0.92f);

        RectTransform panelRt = tutorialTextRoot.GetComponent<RectTransform>();
        panelRt.anchorMin        = new Vector2(0.5f, 0f);
        panelRt.anchorMax        = new Vector2(0.5f, 0f);
        panelRt.pivot            = new Vector2(0.5f, 0f);
        panelRt.anchoredPosition = new Vector2(0f, edgePad);
        panelRt.sizeDelta        = new Vector2(panelW, panelH);

        // Gold top border accent
        GameObject border    = new GameObject("TopBorder");
        border.transform.SetParent(tutorialTextRoot.transform, false);
        Image borderImg      = border.AddComponent<Image>();
        borderImg.color      = new Color(0.82f, 0.65f, 0.18f, 1f);
        RectTransform borderRt = border.GetComponent<RectTransform>();
        borderRt.anchorMin        = new Vector2(0f, 1f);
        borderRt.anchorMax        = new Vector2(1f, 1f);
        borderRt.pivot            = new Vector2(0.5f, 1f);
        borderRt.anchoredPosition = Vector2.zero;
        borderRt.sizeDelta        = new Vector2(0f, 4f);

        // ── Portrait frame ────────────────────────────────────────────────
        // Frame fills most of the panel height; gold border matches the top accent.
        const float frameSz = 172f; // slightly smaller than panelH so there's breathing room
        const float frameBorderThickness = 3f;

        GameObject portraitFrame  = new GameObject("PortraitFrame");
        portraitFrame.transform.SetParent(tutorialTextRoot.transform, false);
        Image frameBg             = portraitFrame.AddComponent<Image>();
        frameBg.color             = new Color(0.82f, 0.65f, 0.18f, 1f); // gold border colour
        RectTransform frameRt     = portraitFrame.GetComponent<RectTransform>();
        frameRt.anchorMin         = new Vector2(0f, 0.5f);
        frameRt.anchorMax         = new Vector2(0f, 0.5f);
        frameRt.pivot             = new Vector2(0f, 0.5f);
        frameRt.anchoredPosition  = new Vector2(edgePad, 0f);
        frameRt.sizeDelta         = new Vector2(frameSz, frameSz);

        // Dark inner background (sits inside the gold border)
        GameObject frameInner     = new GameObject("PortraitInner");
        frameInner.transform.SetParent(portraitFrame.transform, false);
        Image innerBg             = frameInner.AddComponent<Image>();
        innerBg.color             = new Color(0.12f, 0.10f, 0.16f, 1f);
        RectTransform innerRt     = frameInner.GetComponent<RectTransform>();
        innerRt.anchorMin         = Vector2.zero;
        innerRt.anchorMax         = Vector2.one;
        innerRt.offsetMin         = new Vector2( frameBorderThickness,  frameBorderThickness);
        innerRt.offsetMax         = new Vector2(-frameBorderThickness, -frameBorderThickness);

        // Portrait image — RawImage so uvRect can crop legs and show only head + chest.
        GameObject portraitGo     = new GameObject("Portrait");
        portraitGo.transform.SetParent(frameInner.transform, false);
        RawImage portraitRaw      = portraitGo.AddComponent<RawImage>();
        // Show only the top ~62 % of the sprite (head + chest; hides legs).
        // In Unity UV space y=0 is bottom, y=1 is top, so Rect(0, 0.38, 1, 0.62)
        // starts at 38 % from the bottom and covers 62 % of the height upward.
        portraitRaw.uvRect        = new Rect(0f, 0.38f, 1f, 0.62f);

        // Fill the inner frame
        RectTransform portRt      = portraitRaw.GetComponent<RectTransform>();
        portRt.anchorMin          = Vector2.zero;
        portRt.anchorMax          = Vector2.one;
        portRt.offsetMin          = Vector2.zero;
        portRt.offsetMax          = Vector2.zero;

        tutorialPortraitRaw = portraitRaw;

#if UNITY_EDITOR
        texGuidePortrait = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Sprites/Guide NPC/2.png");
        texDevilPortrait = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Sprites/NPC/Merchant_Idle.png");
#endif
        if (texGuidePortrait == null)
            texGuidePortrait = Resources.Load<Texture2D>("Portraits/GuideNPC_Portrait");
        if (texDevilPortrait == null)
            texDevilPortrait = Resources.Load<Texture2D>("Portraits/Merchant_Idle");

        if (texGuidePortrait != null)
        {
            portraitRaw.texture = texGuidePortrait;
            portraitRaw.color   = Color.white;
        }
        else
            portraitRaw.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);

        // tutorialPortraitImage is unused now (replaced by RawImage above).
        tutorialPortraitImage = null;

        // ── Text area ─────────────────────────────────────────────────────
        float textAreaX     = edgePad + frameSz + edgePad;
        float textAreaWidth = panelW - textAreaX - edgePad;

        // Speaker name
        GameObject nameGo         = new GameObject("SpeakerName");
        nameGo.transform.SetParent(tutorialTextRoot.transform, false);
        tutorialSpeakerText       = nameGo.AddComponent<TextMeshProUGUI>();
        tutorialSpeakerText.text      = npcSpeakerName;
        tutorialSpeakerText.fontSize  = 26f;
        tutorialSpeakerText.fontStyle = FontStyles.Bold;
        tutorialSpeakerText.color     = new Color(0.95f, 0.85f, 0.45f, 1f);
        tutorialSpeakerText.alignment = TextAlignmentOptions.TopLeft;

        RectTransform nameRt          = tutorialSpeakerText.GetComponent<RectTransform>();
        nameRt.anchorMin              = new Vector2(0f, 1f);
        nameRt.anchorMax              = new Vector2(0f, 1f);
        nameRt.pivot                  = new Vector2(0f, 1f);
        nameRt.anchoredPosition       = new Vector2(textAreaX, -edgePad);
        nameRt.sizeDelta              = new Vector2(textAreaWidth, 34f);

        // Dialogue body
        GameObject bodyGo             = new GameObject("TutorialText");
        bodyGo.transform.SetParent(tutorialTextRoot.transform, false);
        tutorialText                  = bodyGo.AddComponent<TextMeshProUGUI>();
        tutorialText.fontSize         = 28f;
        tutorialText.color            = Color.white;
        tutorialText.alignment        = TextAlignmentOptions.TopLeft;
        tutorialText.enableWordWrapping = true;

        RectTransform textRt          = tutorialText.GetComponent<RectTransform>();
        textRt.anchorMin              = new Vector2(0f, 0f);
        textRt.anchorMax              = new Vector2(0f, 1f);
        textRt.pivot                  = new Vector2(0f, 1f);
        textRt.anchoredPosition       = new Vector2(textAreaX, -(edgePad + 34f + 6f));
        textRt.sizeDelta              = new Vector2(textAreaWidth, -(edgePad * 2f + 34f + 6f));

        ShowText(false);
    }

    /// <summary>Watcher (Guide) vs. lobby devil (Merchant) portrait + label.</summary>
    void ApplyDialogueSpeaker(bool devil)
    {
        if (tutorialSpeakerText != null)
            tutorialSpeakerText.text = devil ? "The Devil" : npcSpeakerName;

        if (tutorialPortraitRaw == null) return;

        Texture2D tex = devil ? texDevilPortrait : texGuidePortrait;
        if (tex != null)
        {
            tutorialPortraitRaw.texture = tex;
            tutorialPortraitRaw.color   = Color.white;
        }

        // Crop: devil sprite is shorter/wider — tweak UVs so face reads well in frame.
        tutorialPortraitRaw.uvRect = devil
            ? new Rect(0f, 0.22f, 1f, 0.78f)
            : new Rect(0f, 0.38f, 1f, 0.62f);
    }

    private void SetText(string text)
    {
        if (tutorialText != null)
            tutorialText.text = text;
    }

    private void ShowText(bool show)
    {
        if (tutorialTextRoot != null)
            tutorialTextRoot.SetActive(show);
    }
}
