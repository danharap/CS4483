using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Drives the main menu scene.
/// NEW GAME  → loads MainScene and auto-starts the tutorial.
/// LOAD GAME → loads MainScene straight into the lobby (skips tutorial).
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    [Header("Scene Name")]
    [SerializeField] private string gameSceneName = "MainScene";

    [Header("UI References")]
    [SerializeField] private Button    newGameButton;
    [SerializeField] private Button    loadGameButton;
    [SerializeField] private Button    settingsButton;
    [SerializeField] private TMP_Text  titleText;
    [SerializeField] private TMP_Text  subtitleText;
    [SerializeField] private TMP_Text  bestRunText;
    [SerializeField] private TMP_Text  flavorText;
    [SerializeField] private TMP_Text  versionText;

    // Keep the old field name so SetupAll doesn't break if it still wires "playButton"
    [SerializeField] private Button    playButton;

    [Header("Title Pulse")]
    [SerializeField] private float pulseSpeed  = 1.05f;
    [SerializeField] private float pulseMin    = 0.82f;
    [SerializeField] private float pulseMax    = 1.0f;

    Button _profileFooterButton;

    /// <summary>
    /// Static flag read by GameManager/TutorialRoomManager after MainScene loads.
    /// True  = new game → run tutorial immediately.
    /// False = load game → land in lobby.
    /// </summary>
    public static bool ShouldRunTutorial { get; private set; }

    /// <summary>Used when hydrating an account save so tutorial auto-start matches save data.</summary>
    public static void SetShouldRunTutorialForLoadedSave(bool runTutorial) => ShouldRunTutorial = runTutorial;

    private static readonly string[] LoreLines = {
        "\"Many warriors have entered the Fractured Grounds. None have broken the cycle.\"\n— Watcher Nara",
        "\"The Shard of Chaos shattered more than walls. It shattered time.\"\n— Architect's final log",
        "\"Thorn was the greatest of us. Now it is the worst of us.\"\n— Watcher Drak",
        "\"Survive the descent. Grow stronger. Face what waits below.\nRepeat—unless you can end it.\"",
        "\"The Pit remembers every challenger. Their echoes become your enemies.\"\n— Watcher Vex",
    };

    private int loreIndex;

    void Awake()
    {
        if (GetComponent<MainMenuLocalAccountPanel>() == null)
            gameObject.AddComponent<MainMenuLocalAccountPanel>();

        // Self-heal: find buttons by their GameObject name if serialized refs were lost
        if (newGameButton  == null) newGameButton  = FindButtonByName("NewGameButton");
        if (loadGameButton == null) loadGameButton = FindButtonByName("LoadGameButton");
        if (settingsButton == null) settingsButton = FindButtonByName("SettingsButton");
        if (playButton     == null) playButton     = FindButtonByName("PlayButton");

        // Self-heal: find text elements by name
        if (titleText    == null) titleText    = FindTMPByName("Title");
        if (subtitleText == null) subtitleText = FindTMPByName("Subtitle");
        if (bestRunText  == null) bestRunText  = FindTMPByName("BestRunText");
        if (flavorText   == null) flavorText   = FindTMPByName("FlavorText");
        if (versionText  == null) versionText  = FindTMPByName("VersionText");
    }

    void Start()
    {
        ApplyMainMenuPresentation();

        WireMenuButton(newGameButton, OnNewGame);
        WireMenuButton(loadGameButton, OnLoadGame);
        WireMenuButton(settingsButton, OnSettings);
        WireMenuButton(playButton, OnNewGame);

        // Account gate panel unlocks these after a successful sign-in.
        SetMainMenuLocked(true);

        if (titleText)
        {
            titleText.text = PitMenuBranding.GameTitle;
            titleText.color = PitMenuUiTheme.GoldAccent;
            titleText.fontStyle = FontStyles.Bold;
            titleText.characterSpacing = 3f;
            titleText.enableAutoSizing = true;
            titleText.fontSizeMin = 40f;
            titleText.fontSizeMax = 108f;
            titleText.outlineWidth = 0.22f;
            titleText.outlineColor = new Color(0f, 0f, 0f, 0.88f);
        }
        if (subtitleText)
        {
            subtitleText.text = PitMenuBranding.GameSubtitle;
            subtitleText.color = PitMenuUiTheme.TextMuted;
            subtitleText.fontSize = Mathf.Max(subtitleText.fontSize, 20f);
        }
        if (versionText) versionText.text = PitMenuBranding.VersionFooter;

        UpdateBestRun();
        RotateLore();
        StartCoroutine(PulseTitle());
        StartCoroutine(CycleLore());

        StartCoroutine(DeferredFooterRefresh());
    }

    IEnumerator DeferredFooterRefresh()
    {
        yield return null;
        RefreshFooterAfterAccount();
    }

    /// <summary>Call when sign-in state changes so footer buttons match the session.</summary>
    public void RefreshFooterAfterAccount()
    {
        if (settingsButton == null)
        {
            settingsButton = CreateFooterTextButton("Pit_SettingsButton", "Settings", new Vector2(-18f, 18f));
            PitMenuUiTheme.ApplyNeutralPanelButton(settingsButton, settingsButton.GetComponent<Image>());
            WireMenuButton(settingsButton, OnSettings);
        }

        if (!LocalSaveRuntime.IsSignedIn)
        {
            if (_profileFooterButton != null)
            {
                Destroy(_profileFooterButton.gameObject);
                _profileFooterButton = null;
            }
            return;
        }

        if (_profileFooterButton != null) return;

        _profileFooterButton = CreateFooterTextButton("Pit_ProfileButton", "Profile & saves", new Vector2(-18f, 58f));
        PitMenuUiTheme.ApplyGoldGhostButton(_profileFooterButton, _profileFooterButton.GetComponent<Image>());
        WireMenuButton(_profileFooterButton, OnProfileSaves);
    }

    void ApplyMainMenuPresentation()
    {
        var canvas = GetComponent<Canvas>();
        if (canvas != null)
        {
            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null) scaler.matchWidthOrHeight = 0.52f;
        }

        var vig = transform.Find("Vignette")?.GetComponent<Image>();
        if (vig != null)
            vig.color = new Color(0f, 0f, 0f, 0.58f);

        EnsureAtmosphereOverlay();
        EnsureEdgeVignetteBars();

        if (bestRunText != null)
        {
            bestRunText.color = PitMenuUiTheme.TextPrimary;
            bestRunText.fontStyle = FontStyles.Bold;
            bestRunText.alignment = TextAlignmentOptions.Center;
        }

        Transform bestBox = bestRunText != null ? bestRunText.transform.parent : null;
        if (bestBox != null)
        {
            var boxImg = bestBox.GetComponent<Image>();
            if (boxImg != null)
            {
                boxImg.color = new Color(0.04f, 0.045f, 0.055f, 0.82f);
                var o = bestBox.GetComponent<Outline>();
                if (o == null) o = bestBox.gameObject.AddComponent<Outline>();
                o.effectColor = new Color(PitMenuUiTheme.GoldAccent.r, PitMenuUiTheme.GoldAccent.g, PitMenuUiTheme.GoldAccent.b, 0.35f);
                o.effectDistance = new Vector2(1f, -1f);
            }
            var rt = bestBox.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.sizeDelta = new Vector2(640f, 88f);
                rt.anchoredPosition = new Vector2(0f, 8f);
            }
        }

        if (flavorText != null)
        {
            flavorText.color = new Color(0.62f, 0.68f, 0.78f, 0.92f);
            flavorText.fontStyle = FontStyles.Italic;
            flavorText.fontSize = Mathf.Max(flavorText.fontSize, 17f);
            flavorText.lineSpacing = 4f;
        }

        if (versionText != null)
        {
            versionText.color = new Color(0.42f, 0.45f, 0.52f, 1f);
            versionText.fontSize = Mathf.Clamp(versionText.fontSize, 12f, 14f);
        }

        var sep = transform.Find("Separator")?.GetComponent<Image>();
        if (sep != null)
            sep.color = new Color(PitMenuUiTheme.GoldAccent.r, PitMenuUiTheme.GoldAccent.g, PitMenuUiTheme.GoldAccent.b, 0.55f);

        StylePrimaryGameButton(newGameButton);
        StyleSecondaryGameButton(loadGameButton);

        var titleRt = titleText != null ? titleText.rectTransform : null;
        if (titleRt != null)
        {
            titleRt.anchoredPosition = new Vector2(0f, 220f);
            titleRt.sizeDelta = new Vector2(920f, 180f);
        }
        if (subtitleText != null)
        {
            var srt = subtitleText.rectTransform;
            srt.anchoredPosition = new Vector2(0f, 128f);
            srt.sizeDelta = new Vector2(720f, 48f);
        }
        if (sep != null)
        {
            var ert = sep.rectTransform;
            ert.sizeDelta = new Vector2(420f, 2f);
            ert.anchoredPosition = new Vector2(0f, 92f);
        }
        if (flavorText != null)
        {
            var frt = flavorText.rectTransform;
            frt.anchoredPosition = new Vector2(0f, -58f);
            frt.sizeDelta = new Vector2(760f, 96f);
        }
        if (newGameButton != null)
        {
            var nrt = newGameButton.GetComponent<RectTransform>();
            nrt.sizeDelta = new Vector2(400f, 56f);
            nrt.anchoredPosition = new Vector2(0f, -148f);
        }
        if (loadGameButton != null)
        {
            var lrt = loadGameButton.GetComponent<RectTransform>();
            lrt.sizeDelta = new Vector2(400f, 56f);
            lrt.anchoredPosition = new Vector2(0f, -218f);
        }
        if (versionText != null)
        {
            var vrt = versionText.rectTransform;
            vrt.anchorMin = new Vector2(0.5f, 0f);
            vrt.anchorMax = new Vector2(0.5f, 0f);
            vrt.pivot = new Vector2(0.5f, 0f);
            vrt.anchoredPosition = new Vector2(0f, 28f);
            vrt.sizeDelta = new Vector2(900f, 36f);
        }
    }

    void EnsureAtmosphereOverlay()
    {
        if (transform.Find("Pit_AtmosphereWash") != null) return;
        var go = new GameObject("Pit_AtmosphereWash", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(transform, false);
        var rt = go.GetComponent<RectTransform>();
        rt.SetAsFirstSibling();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        go.GetComponent<Image>().color = new Color(0.02f, 0.04f, 0.08f, 0.28f);
        go.GetComponent<Image>().raycastTarget = false;
    }

    void EnsureEdgeVignetteBars()
    {
        if (transform.Find("Pit_VignetteEdge") != null) return;
        const float t = 72f;
        void Bar(string n, Vector2 aMin, Vector2 aMax, Vector2 pivot, Vector2 size)
        {
            var go = new GameObject(n, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(transform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = aMin;
            rt.anchorMax = aMax;
            rt.pivot = pivot;
            rt.sizeDelta = size;
            rt.anchoredPosition = Vector2.zero;
            go.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);
            go.GetComponent<Image>().raycastTarget = false;
        }
        int idx = transform.Find("Vignette") != null ? 1 : 0;
        Bar("Pit_VignetteEdge_T", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, t));
        Bar("Pit_VignetteEdge_B", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, t));
        Bar("Pit_VignetteEdge_L", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(t, 0f));
        Bar("Pit_VignetteEdge_R", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(t, 0f));
        int insert = transform.Find("Pit_AtmosphereWash") != null ? 1 : 0;
        foreach (Transform c in transform)
        {
            if (c != null && c.name.StartsWith("Pit_VignetteEdge", System.StringComparison.Ordinal))
                c.SetSiblingIndex(Mathf.Clamp(insert, 0, transform.childCount - 1));
        }
    }

    Button CreateFooterTextButton(string objName, string label, Vector2 anchoredPos)
    {
        var go = new GameObject(objName, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(transform, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(1f, 0f);
        rt.sizeDelta = new Vector2(200f, 34f);
        rt.anchoredPosition = anchoredPos;
        var img = go.GetComponent<Image>();
        img.color = new Color(0.12f, 0.13f, 0.16f, 0.85f);
        var b = go.GetComponent<Button>();
        b.targetGraphic = img;
        var txtGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        txtGo.transform.SetParent(go.transform, false);
        var tmp = txtGo.GetComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 13f;
        tmp.color = PitMenuUiTheme.TextPrimary;
        tmp.alignment = TextAlignmentOptions.Center;
        var tr = txtGo.GetComponent<RectTransform>();
        tr.anchorMin = Vector2.zero;
        tr.anchorMax = Vector2.one;
        tr.offsetMin = Vector2.zero;
        tr.offsetMax = Vector2.zero;
        return b;
    }

    void OnProfileSaves()
    {
        var panel = GetComponent<MainMenuLocalAccountPanel>();
        panel?.OpenAccountPanel();
    }

    static void StylePrimaryGameButton(Button b)
    {
        if (b == null) return;
        var img = b.GetComponent<Image>();
        if (img == null) return;
        PitMenuUiTheme.ApplyPrimaryRunButton(b, img);
        var label = b.GetComponentInChildren<TMP_Text>();
        if (label != null) label.color = PitMenuUiTheme.TextPrimary;
    }

    static void StyleSecondaryGameButton(Button b)
    {
        if (b == null) return;
        var img = b.GetComponent<Image>();
        if (img == null) return;
        PitMenuUiTheme.ApplySecondarySteelButton(b, img);
        var label = b.GetComponentInChildren<TMP_Text>();
        if (label != null) label.color = PitMenuUiTheme.TextPrimary;
    }

    static void WireMenuButton(Button b, UnityEngine.Events.UnityAction action)
    {
        if (b == null || action == null) return;
        b.onClick.AddListener(GameAudio.PlayButtonClick);
        b.onClick.AddListener(action);
        EventTrigger et = b.gameObject.GetComponent<EventTrigger>();
        if (et == null) et = b.gameObject.AddComponent<EventTrigger>();
        var hover = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        hover.callback.AddListener(_ => GameAudio.PlayButtonHover());
        et.triggers.Add(hover);
    }

    private Button FindButtonByName(string goName)
    {
        Transform t = transform.Find(goName);
        if (t != null) return t.GetComponent<Button>();
        // Wider search in case hierarchy differs
        foreach (Button b in GetComponentsInChildren<Button>(true))
            if (b.gameObject.name == goName) return b;
        return null;
    }

    private TMP_Text FindTMPByName(string goName)
    {
        foreach (TMP_Text t in GetComponentsInChildren<TMP_Text>(true))
            if (t.gameObject.name == goName) return t;
        return null;
    }

    void UpdateBestRun()
    {
        if (bestRunText == null) return;
        var hs = HighScoreManager.Instance;
        string gold = ColorUtility.ToHtmlStringRGB(PitMenuUiTheme.GoldAccent);
        string dim = ColorUtility.ToHtmlStringRGB(PitMenuUiTheme.TextMuted);
        if (hs == null || !hs.HasAnyRecord())
        {
            bestRunText.text = $"<size=15><color=#{dim}>PERSONAL BEST</color></size>\n<size=14>No recorded descent yet. Survive your first run to claim a record.</size>";
            return;
        }
        bestRunText.text =
            $"<size=15><color=#{dim}>PERSONAL BEST</color></size>\n" +
            $"<color=#{gold}>{hs.BestWaves}</color> <size=13><color=#{dim}>waves</color></size>   " +
            $"<color=#{gold}>{HighScoreManager.FormatTime(hs.BestTime)}</color> <size=13><color=#{dim}>time</color></size>   " +
            $"<color=#{gold}>{hs.BestKills}</color> <size=13><color=#{dim}>strikes</color></size>";
    }

    void OnNewGame()
    {
        LocalSaveRuntime.ActiveSaveId = null;
        LocalSaveRuntime.PendingHydrate = null;
        PlayerPrefs.SetInt("Meta_TutorialCompleted", 0);
        PlayerPrefs.Save();
        ShouldRunTutorial = true;
        SceneManager.LoadScene(gameSceneName);
    }

    void OnLoadGame()
    {
        LocalSaveRuntime.ActiveSaveId = null;
        LocalSaveRuntime.PendingHydrate = null;
        ShouldRunTutorial = false;
        SceneManager.LoadScene(gameSceneName);
    }

    /// <summary>Used after setting <see cref="LocalSaveRuntime.PendingHydrate"/>.</summary>
    public void LoadMainSceneFromAccountSave()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    public void SetMainMenuLocked(bool locked)
    {
        bool enabled = !locked;
        if (newGameButton != null) newGameButton.interactable = enabled;
        if (loadGameButton != null) loadGameButton.interactable = enabled;
        if (playButton != null) playButton.interactable = enabled;
    }

    void OnSettings()
    {
        SettingsManager sm = Object.FindFirstObjectByType<SettingsManager>(FindObjectsInactive.Include);
        if (sm != null) sm.Show();
        else
        {
            // Create SettingsManager on the fly if it wasn't placed in the scene.
            GameObject go = new GameObject("SettingsManager");
            sm = go.AddComponent<SettingsManager>();
            sm.Show();
        }
    }

    void RotateLore()
    {
        if (flavorText == null) return;
        flavorText.text = LoreLines[loreIndex % LoreLines.Length];
    }

    private IEnumerator CycleLore()
    {
        while (true)
        {
            yield return new WaitForSeconds(6f);
            loreIndex = (loreIndex + 1) % LoreLines.Length;
            float t = 0f;
            while (t < 0.4f)
            {
                if (flavorText) flavorText.alpha = Mathf.Lerp(1f, 0f, t / 0.4f);
                t += Time.deltaTime;
                yield return null;
            }
            RotateLore();
            t = 0f;
            while (t < 0.4f)
            {
                if (flavorText) flavorText.alpha = Mathf.Lerp(0f, 1f, t / 0.4f);
                t += Time.deltaTime;
                yield return null;
            }
            if (flavorText) flavorText.alpha = 1f;
        }
    }

    private IEnumerator PulseTitle()
    {
        while (true)
        {
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * pulseSpeed;
                float a = Mathf.Lerp(pulseMin, pulseMax, Mathf.PingPong(t, 1f));
                if (titleText) titleText.alpha = a;
                yield return null;
            }
        }
    }
}
