using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Settings panel — accessible from the main menu or the pause menu.
///
/// Manages:
///   • Master / SFX / Music volume sliders  (data only — no audio hookup required)
///   • Basic key-rebinding display (read-only placeholders; actual rebinding is a
///     future task requiring an Input System upgrade)
///   • Saves / loads all values via PlayerPrefs
///
/// Usage:
///   Attach to a panel GameObject. Call Show() / Hide() from buttons.
///   The panel builds itself at runtime if references are not wired in the Inspector.
/// </summary>
public class SettingsManager : MonoBehaviour
{
    // ── PlayerPrefs keys ──────────────────────────────────────────────────
    public const string KeyMasterVol = "Settings_MasterVolume";
    public const string KeySFXVol    = "Settings_SFXVolume";
    public const string KeyMusicVol  = "Settings_MusicVolume";

    // ── Inspector refs (optional — built at runtime if null) ──────────────
    [Header("Volume Sliders")]
    [SerializeField] private Slider   masterSlider;
    [SerializeField] private Slider   sfxSlider;
    [SerializeField] private Slider   musicSlider;

    [Header("Volume Labels")]
    [SerializeField] private TMP_Text masterLabel;
    [SerializeField] private TMP_Text sfxLabel;
    [SerializeField] private TMP_Text musicLabel;

    [Header("Close")]
    [SerializeField] private Button   closeButton;

    // ── Defaults ──────────────────────────────────────────────────────────
    private const float DefaultMaster = 0.8f;
    private const float DefaultSFX    = 1.0f;
    private const float DefaultMusic  = 0.6f;

    // ── Colours ───────────────────────────────────────────────────────────
    private static readonly Color ColPanel   = new Color(0.06f, 0.06f, 0.08f, 0.95f);
    private static readonly Color ColBorder  = new Color(0.80f, 0.65f, 0.18f);
    private static readonly Color ColText    = new Color(0.92f, 0.90f, 0.84f);
    private static readonly Color ColFill    = new Color(0.80f, 0.65f, 0.18f);

    // ── State ─────────────────────────────────────────────────────────────
    /// <summary>Current master volume (0–1). Read by audio systems.</summary>
    public static float MasterVolume { get; private set; }
    /// <summary>Current SFX volume (0–1).</summary>
    public static float SFXVolume    { get; private set; }
    /// <summary>Current music volume (0–1).</summary>
    public static float MusicVolume  { get; private set; }

    // Singleton for easy access.
    public static SettingsManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        Load();
    }

    void Start()
    {
        EnsurePanel();
        RefreshSliderDisplay();
        HookSliders();
        if (closeButton != null) closeButton.onClick.AddListener(Hide);
    }

    // ── Public API ────────────────────────────────────────────────────────

    public void Show()
    {
        gameObject.SetActive(true);
        RefreshSliderDisplay();
    }

    public void Hide()
    {
        Save();
        gameObject.SetActive(false);
    }

    // ── Persistence ───────────────────────────────────────────────────────

    private static void Load()
    {
        MasterVolume = PlayerPrefs.GetFloat(KeyMasterVol, DefaultMaster);
        SFXVolume    = PlayerPrefs.GetFloat(KeySFXVol,    DefaultSFX);
        MusicVolume  = PlayerPrefs.GetFloat(KeyMusicVol,  DefaultMusic);
    }

    private static void Save()
    {
        PlayerPrefs.SetFloat(KeyMasterVol, MasterVolume);
        PlayerPrefs.SetFloat(KeySFXVol,    SFXVolume);
        PlayerPrefs.SetFloat(KeyMusicVol,  MusicVolume);
        PlayerPrefs.Save();
    }

    /// <summary>Apply per-save audio settings from cloud snapshot.</summary>
    public static void ApplyFromSave(SettingsSaveBlock s)
    {
        if (s == null) return;
        MasterVolume = Mathf.Clamp01(s.masterVolume);
        SFXVolume    = Mathf.Clamp01(s.sfxVolume);
        MusicVolume  = Mathf.Clamp01(s.musicVolume);
        PlayerPrefs.SetFloat(KeyMasterVol, MasterVolume);
        PlayerPrefs.SetFloat(KeySFXVol,    SFXVolume);
        PlayerPrefs.SetFloat(KeyMusicVol,  MusicVolume);
        PlayerPrefs.Save();
        Instance?.RefreshSliderDisplay();
    }

    public static SettingsSaveBlock ExportSaveBlock() =>
        new SettingsSaveBlock
        {
            masterVolume = MasterVolume,
            sfxVolume    = SFXVolume,
            musicVolume  = MusicVolume
        };

    // ── Slider callbacks ──────────────────────────────────────────────────

    private void HookSliders()
    {
        if (masterSlider) masterSlider.onValueChanged.AddListener(v => { MasterVolume = v; UpdateLabel(masterLabel, "Master", v); });
        if (sfxSlider)    sfxSlider.onValueChanged.AddListener   (v => { SFXVolume    = v; UpdateLabel(sfxLabel,    "SFX",    v); });
        if (musicSlider)  musicSlider.onValueChanged.AddListener (v => { MusicVolume  = v; UpdateLabel(musicLabel,  "Music",  v); });
    }

    public void RefreshSliderDisplay()
    {
        if (masterSlider) masterSlider.value = MasterVolume;
        if (sfxSlider)    sfxSlider.value    = SFXVolume;
        if (musicSlider)  musicSlider.value  = MusicVolume;
        UpdateLabel(masterLabel, "Master", MasterVolume);
        UpdateLabel(sfxLabel,    "SFX",    SFXVolume);
        UpdateLabel(musicLabel,  "Music",  MusicVolume);
    }

    private static void UpdateLabel(TMP_Text lbl, string name, float v)
    {
        if (lbl != null) lbl.text = $"{name}  {Mathf.RoundToInt(v * 100f)}%";
    }

    // ── Runtime panel builder ─────────────────────────────────────────────

    private void EnsurePanel()
    {
        if (masterSlider != null) return;   // already wired

        Canvas canvas = FindCanvas();
        if (canvas == null) return;

        // Outer panel
        GameObject panel = BuildImage(canvas.transform, "SettingsPanel",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, 460f, 380f, ColPanel);

        // Title
        TMP_Text title = BuildLabel(panel.transform, "SettingsTitle",
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -20f), 26f, ColBorder, TextAlignmentOptions.Center);
        title.text      = "SETTINGS";
        title.fontStyle = FontStyles.Bold;

        // Divider line (thin image)
        BuildImage(panel.transform, "Divider",
            new Vector2(0.05f, 1f), new Vector2(0.95f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -56f), 0f, 2f, ColBorder);

        // Volume sliders
        float y = -80f;
        (masterSlider, masterLabel) = BuildSliderRow(panel.transform, "Master", y);
        y -= 70f;
        (sfxSlider, sfxLabel)       = BuildSliderRow(panel.transform, "SFX", y);
        y -= 70f;
        (musicSlider, musicLabel)   = BuildSliderRow(panel.transform, "Music", y);
        y -= 70f;

        // Key bindings placeholder section
        TMP_Text bindTitle = BuildLabel(panel.transform, "BindTitle",
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, y), 16f, ColBorder, TextAlignmentOptions.Center);
        bindTitle.text = "KEY BINDINGS";
        y -= 28f;

        string[] binds = { "Move  WASD", "Aim  Mouse", "Shoot  LMB", "Dash  Left Shift" };
        foreach (string b in binds)
        {
            TMP_Text bt = BuildLabel(panel.transform, "Bind_" + b,
                new Vector2(0.1f, 1f), new Vector2(0.9f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, y), 13f, ColText, TextAlignmentOptions.Center);
            bt.text = b;
            y -= 22f;
        }

        // Close button
        GameObject closeGo = BuildImage(panel.transform, "CloseButton",
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 20f), 120f, 36f, new Color(0.65f, 0.15f, 0.15f));
        closeButton = closeGo.AddComponent<Button>();
        closeButton.targetGraphic = closeGo.GetComponent<Image>();
        closeButton.onClick.AddListener(Hide);
        TMP_Text closeTxt = BuildLabel(closeGo.transform, "CloseTxt",
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
            Vector2.zero, 15f, Color.white, TextAlignmentOptions.Center);
        closeTxt.text = "CLOSE";

        // Redirect refs to the panel children.
        gameObject.transform.SetParent(panel.transform.parent, false);
    }

    private (Slider, TMP_Text) BuildSliderRow(Transform parent, string name, float anchoredY)
    {
        // Label
        TMP_Text lbl = BuildLabel(parent, name + "Label",
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(24f, anchoredY), 15f, ColText, TextAlignmentOptions.Left);
        lbl.text = $"{name}  0%";

        // Slider
        GameObject sliderGo = new GameObject(name + "Slider");
        sliderGo.transform.SetParent(parent, false);
        Slider s = sliderGo.AddComponent<Slider>();
        s.minValue   = 0f;
        s.maxValue   = 1f;
        s.value      = 1f;
        RectTransform sRt = sliderGo.GetComponent<RectTransform>();
        sRt.anchorMin = new Vector2(0.05f, 1f); sRt.anchorMax = new Vector2(0.95f, 1f);
        sRt.pivot     = new Vector2(0.5f, 1f);
        sRt.anchoredPosition = new Vector2(0f, anchoredY - 20f);
        sRt.sizeDelta = new Vector2(0f, 18f);

        // Background
        GameObject bg = BuildImage(sliderGo.transform, "BG",
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
            Vector2.zero, 0f, 0f, new Color(0.12f, 0.12f, 0.15f));

        // Fill area
        GameObject fa = new GameObject("FillArea"); fa.transform.SetParent(sliderGo.transform, false);
        RectTransform faRt = fa.AddComponent<RectTransform>();
        faRt.anchorMin = Vector2.zero; faRt.anchorMax = Vector2.one;
        faRt.offsetMin = new Vector2(2f, 2f); faRt.offsetMax = new Vector2(-2f, -2f);

        // Fill
        GameObject fill = BuildImage(fa.transform, "Fill",
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
            Vector2.zero, 0f, 0f, ColFill);
        s.fillRect          = fill.GetComponent<RectTransform>();
        s.targetGraphic     = fill.GetComponent<Image>();
        s.interactable      = true;

        return (s, lbl);
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static Canvas FindCanvas()
    {
        GameObject go = GameObject.Find("Canvas_MainMenu") ?? GameObject.Find("Canvas_HUD");
        if (go != null) return go.GetComponent<Canvas>();
        return Object.FindFirstObjectByType<Canvas>();
    }

    private static GameObject BuildImage(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
        Vector2 anchoredPos, float w, float h, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Image img = go.AddComponent<Image>();
        img.color = color;
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin        = anchorMin;
        rt.anchorMax        = anchorMax;
        rt.pivot            = pivot;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta        = new Vector2(w, h);
        return go;
    }

    private static TMP_Text BuildLabel(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
        Vector2 anchoredPos, float fontSize, Color color, TextAlignmentOptions alignment)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        TMP_Text txt = go.AddComponent<TextMeshProUGUI>();
        txt.fontSize  = fontSize;
        txt.color     = color;
        txt.alignment = alignment;
        RectTransform rt = txt.GetComponent<RectTransform>();
        rt.anchorMin        = anchorMin;
        rt.anchorMax        = anchorMax;
        rt.pivot            = pivot;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta        = Vector2.zero;
        return txt;
    }
}
