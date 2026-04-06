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
        Combat,
        AwaitSecondGatePass,
        CollectOrbs,
        PickUpgrade,
        NpcSection,
        Complete
    }

    private TutorialPhase phase = TutorialPhase.Inactive;

    private bool tutorialRoomStarted;
    private bool moveInputSeen;
    private int tutorialOrbsCollected;

    private GameObject tutorialEnemyPrefab;
    private Transform tutorialEnemySpawnPoint;
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
        ShowText(false);

        PlayerController pc = GameManager.Instance?.PlayerController;
        if (pc != null) pc.SetInputLocked(false);
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
            OpenGate(gate3);
            phase = TutorialPhase.NpcSection;
            SetText("Speak with the guide (Press E), then continue to the exit.");
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
        phase = TutorialPhase.Combat;
        SetText("Left click and aim your mouse to shoot");
        StartCoroutine(SpawnCombatEnemyAfterDelay(3f));
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
        StartCoroutine(ShowMessageRoutine(message, duration));
    }

    private IEnumerator PlayIntroThenStart()
    {
        PlayerController pc = GameManager.Instance?.PlayerController;
        if (pc != null) pc.SetInputLocked(true);

        ShowText(true);
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

        // Load portrait from Resources if not set in Inspector
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
        GameObject portraitFrame  = new GameObject("PortraitFrame");
        portraitFrame.transform.SetParent(tutorialTextRoot.transform, false);
        Image frameBg             = portraitFrame.AddComponent<Image>();
        frameBg.color             = new Color(0.18f, 0.14f, 0.22f, 1f);
        RectTransform frameRt     = portraitFrame.GetComponent<RectTransform>();
        frameRt.anchorMin         = new Vector2(0f, 0.5f);
        frameRt.anchorMax         = new Vector2(0f, 0.5f);
        frameRt.pivot             = new Vector2(0f, 0.5f);
        frameRt.anchoredPosition  = new Vector2(edgePad, 0f);
        frameRt.sizeDelta         = new Vector2(portraitSz, portraitSz);

        GameObject portraitGo     = new GameObject("Portrait");
        portraitGo.transform.SetParent(portraitFrame.transform, false);
        tutorialPortraitImage     = portraitGo.AddComponent<Image>();
        tutorialPortraitImage.preserveAspect = true;
        tutorialPortraitImage.sprite         = npcPortrait;
        if (npcPortrait == null)
            tutorialPortraitImage.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);

        RectTransform portRt      = tutorialPortraitImage.GetComponent<RectTransform>();
        portRt.anchorMin          = new Vector2(0.05f, 0.05f);
        portRt.anchorMax          = new Vector2(0.95f, 0.95f);
        portRt.offsetMin          = Vector2.zero;
        portRt.offsetMax          = Vector2.zero;

        // ── Text area ─────────────────────────────────────────────────────
        float textAreaX     = edgePad + portraitSz + edgePad;
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
