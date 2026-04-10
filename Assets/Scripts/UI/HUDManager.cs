using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages all in-run HUD elements: HP bar, XP bar, wave info, timer, transitions.
///
/// This version builds a clean, modern layout at runtime if serialized references
/// are not already wired (e.g. after a SETUP EVERYTHING run). All layout values are
/// exposed as serialized fields so they can be tweaked without code changes.
/// </summary>
public class HUDManager : MonoBehaviour
{
    public static HUDManager Instance { get; private set; }

    // ── Inspector References (auto-created at runtime if null) ────────────
    [Header("HP")]
    [SerializeField] private Slider   hpSlider;
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private Image    hpFill;

    [Header("XP")]
    [SerializeField] private Slider   xpSlider;
    [SerializeField] private TMP_Text levelText;

    [Header("Wave / Timer")]
    [SerializeField] private TMP_Text waveText;
    [SerializeField] private TMP_Text timerText;

    [Header("Transition Banner")]
    [SerializeField] private TMP_Text transitionText;
    [SerializeField] private float    transitionDuration = 2.5f;

    [Header("Boss HP Bar")]
    [SerializeField] private GameObject bossHpPanel;
    [SerializeField] private Slider     bossHpSlider;
    [SerializeField] private TMP_Text   bossHpText;
    [SerializeField] private TMP_Text   bossNameText;
    [SerializeField] private Image      bossHpFill;

    // ── Colours ───────────────────────────────────────────────────────────
    private static readonly Color ColHP      = new Color(0.87f, 0.20f, 0.20f);
    private static readonly Color ColHPLow   = new Color(1.00f, 0.55f, 0.05f);
    private static readonly Color ColXP      = new Color(0.25f, 0.70f, 1.00f);
    private static readonly Color ColBossHP  = new Color(1.00f, 0.30f, 0.05f);
    private static readonly Color ColPanel   = new Color(0.06f, 0.06f, 0.08f, 0.82f);
    private static readonly Color ColBorder  = new Color(0.80f, 0.65f, 0.18f, 1.00f);
    private static readonly Color ColText    = new Color(0.95f, 0.92f, 0.85f, 1.00f);

    // ── Lifecycle ─────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        if (transitionText != null) transitionText.alpha = 0f;

        EnsureHUD();
    }

    void Start()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        if (gm.PlayerHealth != null)
        {
            gm.PlayerHealth.OnHealthChanged += UpdateHP;
            UpdateHP(gm.PlayerHealth.CurrentHP, gm.PlayerHealth.maxHP);
        }
        if (gm.PlayerXP != null)
        {
            gm.PlayerXP.OnXPChanged += UpdateXP;
            UpdateXP(0f, gm.PlayerXP.XPThreshold, 1);
        }
        if (gm.WaveManager != null)
            gm.WaveManager.OnWaveStart += UpdateWaveNumber;
    }

    void Update()
    {
        var wm = GameManager.Instance?.WaveManager;
        if (wm == null || !wm.HasStarted)
        {
            if (timerText) timerText.text = "";
            if (waveText)  waveText.text  = "";
            return;
        }

        if (timerText != null)
        {
            if (wm.IsBreak)         timerText.text = "Prepare…";
            else if (wm.IsBossWave) timerText.text = "BOSS";
            else                    timerText.text = $"{Mathf.CeilToInt(wm.WaveTimer)}s";
        }

        // Pulse HP red when low
        if (hpFill != null && GameManager.Instance?.PlayerHealth != null)
        {
            float frac = GameManager.Instance.PlayerHealth.CurrentHP / GameManager.Instance.PlayerHealth.maxHP;
            hpFill.color = frac < 0.3f
                ? Color.Lerp(ColHPLow, ColHP, Mathf.PingPong(Time.unscaledTime * 3f, 1f))
                : ColHP;
        }
    }

    // ── Update methods ────────────────────────────────────────────────────

    public void UpdateHP(float current, float max)
    {
        if (hpSlider) hpSlider.value = max > 0f ? current / max : 0f;
        if (hpText)   hpText.text    = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
    }

    public void UpdateXP(float current, float max, int level)
    {
        if (xpSlider)  xpSlider.value = max > 0f ? current / max : 0f;
        if (levelText) levelText.text  = $"Lv {level}";
    }

    public void UpdateWaveNumber(int waveIndex)
    {
        if (waveText) waveText.text = $"Wave {waveIndex + 1}";
    }

    public void ShowTransition(string message)
    {
        StopAllCoroutines();
        StartCoroutine(TransitionRoutine(message));
    }

    // ── Boss HP ───────────────────────────────────────────────────────────

    public void ShowBossHP(string bossDisplayName, float current, float max)
    {
        if (bossHpPanel != null) bossHpPanel.SetActive(true);
        if (bossNameText != null) bossNameText.text = bossDisplayName;
        UpdateBossHP(current, max);
    }

    public void UpdateBossHP(float current, float max)
    {
        if (bossHpSlider != null) bossHpSlider.value = max > 0f ? current / max : 0f;
        if (bossHpText   != null) bossHpText.text    = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
    }

    public void HideBossHP()
    {
        if (bossHpPanel != null) bossHpPanel.SetActive(false);
    }

    // ── Transition banner ─────────────────────────────────────────────────

    private IEnumerator TransitionRoutine(string msg)
    {
        if (transitionText == null) yield break;
        transitionText.text  = msg;
        transitionText.alpha = 1f;

        float hold  = transitionDuration * 0.65f;
        float fade  = transitionDuration * 0.35f;
        yield return new WaitForSeconds(hold);

        float t = 0f;
        while (t < fade)
        {
            t += Time.unscaledDeltaTime;
            transitionText.alpha = Mathf.Lerp(1f, 0f, t / fade);
            yield return null;
        }
        transitionText.alpha = 0f;
    }

    // ── Runtime HUD builder ───────────────────────────────────────────────
    // Creates a clean layout if the scene doesn't have wired references yet.

    private void EnsureHUD()
    {
        if (hpSlider != null && xpSlider != null) return; // already wired

        Canvas canvas = FindCanvasHUD();
        if (canvas == null) return;

        RectTransform root = canvas.GetComponent<RectTransform>();

        // ── Bottom-left cluster: HP + XP stacked ─────────────────────────
        float barW = 280f, barH = 18f, pad = 14f, spacing = 10f;

        // HP bar
        if (hpSlider == null)
        {
            GameObject hpGroup = BuildBarGroup(canvas.transform, "HUD_HP",
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0.5f, 0f),
                new Vector2(pad, pad + barH + spacing + barH + spacing),
                barW, barH, "HP", ColHP,
                out hpSlider, out hpText, out hpFill);
        }

        // XP bar
        if (xpSlider == null)
        {
            GameObject xpGroup = BuildBarGroup(canvas.transform, "HUD_XP",
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0.5f, 0f),
                new Vector2(pad, pad + barH + spacing),
                barW, barH, "XP", ColXP,
                out xpSlider, out TMP_Text xpLabel, out Image xpFillImg);
        }

        // Level text next to XP
        if (levelText == null)
            levelText = BuildLabel(canvas.transform, "HUD_Level",
                new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(pad + barW + 8f, pad + spacing), 18f, ColText, TextAlignmentOptions.Left);

        // ── Top-right cluster: wave + timer ───────────────────────────────
        float topPad = 14f;
        if (waveText == null)
        {
            // Background chip
            GameObject chip = BuildPanel(canvas.transform, "HUD_WaveChip",
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-(topPad + 120f), -(topPad)), 130f, 56f);

            waveText = BuildLabel(chip.transform, "WaveLabel",
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -4f), 20f, ColBorder, TextAlignmentOptions.Center);

            timerText = BuildLabel(chip.transform, "TimerLabel",
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 4f), 16f, ColText, TextAlignmentOptions.Center);
        }

        // ── Centre-top: transition banner ─────────────────────────────────
        if (transitionText == null)
        {
            transitionText = BuildLabel(canvas.transform, "HUD_Transition",
                new Vector2(0.1f, 0.65f), new Vector2(0.9f, 0.65f), new Vector2(0.5f, 0.5f),
                Vector2.zero, 32f, Color.white, TextAlignmentOptions.Center);
            transitionText.fontStyle = FontStyles.Bold;
            transitionText.alpha = 0f;
        }

        // ── Top-center: boss HP panel ─────────────────────────────────────
        if (bossHpPanel == null)
            BuildBossHP(canvas.transform);
    }

    // ── UI factory helpers ────────────────────────────────────────────────

    private static Canvas FindCanvasHUD()
    {
        GameObject go = GameObject.Find("Canvas_HUD");
        if (go != null) return go.GetComponent<Canvas>();
        Canvas c = Object.FindFirstObjectByType<Canvas>();
        return c;
    }

    private static GameObject BuildPanel(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
        Vector2 anchoredPos, float w, float h)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        Image img = go.AddComponent<Image>();
        img.color = ColPanel;
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin        = anchorMin;
        rt.anchorMax        = anchorMax;
        rt.pivot            = pivot;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta        = new Vector2(w, h);
        return go;
    }

    private static GameObject BuildBarGroup(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
        Vector2 anchoredPos, float w, float h, string label, Color fillColor,
        out Slider slider, out TMP_Text valueText, out Image fillImage)
    {
        // Backing panel
        GameObject panel = BuildPanel(parent, name, anchorMin, anchorMax, pivot, anchoredPos, w, h + 20f);

        // Bar label
        TMP_Text lbl = BuildLabel(panel.transform, "BarLabel",
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f),
            new Vector2(4f, 0f), 13f, ColBorder, TextAlignmentOptions.Left);
        lbl.text = label;

        // Slider
        GameObject sliderGo = new GameObject("Slider");
        sliderGo.transform.SetParent(panel.transform, false);
        slider = sliderGo.AddComponent<Slider>();
        RectTransform sRt = sliderGo.GetComponent<RectTransform>();
        sRt.anchorMin = Vector2.zero; sRt.anchorMax = Vector2.one;
        sRt.offsetMin = new Vector2(0f, 0f); sRt.offsetMax = new Vector2(0f, -16f);

        // Background
        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(sliderGo.transform, false);
        Image bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.12f, 0.12f, 0.14f, 1f);
        RectTransform bgRt = bg.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero; bgRt.offsetMax = Vector2.zero;

        // Fill area
        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderGo.transform, false);
        RectTransform faRt = fillArea.GetComponent<RectTransform>() ?? fillArea.AddComponent<RectTransform>();
        faRt.anchorMin = Vector2.zero; faRt.anchorMax = Vector2.one;
        faRt.offsetMin = new Vector2(2f, 2f); faRt.offsetMax = new Vector2(-2f, -2f);

        // Fill
        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        fillImage = fill.AddComponent<Image>();
        fillImage.color = fillColor;
        RectTransform fillRt = fill.GetComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero; fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = Vector2.zero; fillRt.offsetMax = Vector2.zero;

        slider.fillRect    = fillRt;
        slider.targetGraphic = fillImage;
        slider.interactable  = false;
        slider.minValue = 0f; slider.maxValue = 1f;

        // Value text (right side)
        valueText = BuildLabel(panel.transform, "ValueText",
            new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f),
            new Vector2(-4f, 2f), 11f, ColText, TextAlignmentOptions.Right);

        return panel;
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

    private void BuildBossHP(Transform parent)
    {
        float panelW = 500f, panelH = 58f;
        bossHpPanel = BuildPanel(parent, "HUD_BossHP",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -14f), panelW, panelH);

        // Boss name
        bossNameText = BuildLabel(bossHpPanel.transform, "BossName",
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -3f), 18f, ColBorder, TextAlignmentOptions.Center);
        bossNameText.fontStyle = FontStyles.Bold;

        // Boss HP slider
        GameObject sliderGo = new GameObject("BossSlider");
        sliderGo.transform.SetParent(bossHpPanel.transform, false);
        bossHpSlider = sliderGo.AddComponent<Slider>();
        RectTransform sRt = sliderGo.GetComponent<RectTransform>();
        sRt.anchorMin = new Vector2(0f, 0f); sRt.anchorMax = new Vector2(1f, 0f);
        sRt.pivot = new Vector2(0.5f, 0f);
        sRt.anchoredPosition = new Vector2(0f, 6f);
        sRt.sizeDelta = new Vector2(-20f, 14f);

        GameObject bg2 = new GameObject("BG"); bg2.transform.SetParent(sliderGo.transform, false);
        Image bg2Img = bg2.AddComponent<Image>(); bg2Img.color = new Color(0.1f, 0.1f, 0.12f);
        RectTransform bg2Rt = bg2.GetComponent<RectTransform>();
        bg2Rt.anchorMin = Vector2.zero; bg2Rt.anchorMax = Vector2.one;
        bg2Rt.offsetMin = Vector2.zero; bg2Rt.offsetMax = Vector2.zero;

        GameObject fa2 = new GameObject("FillArea"); fa2.transform.SetParent(sliderGo.transform, false);
        RectTransform fa2Rt = fa2.AddComponent<RectTransform>();
        fa2Rt.anchorMin = Vector2.zero; fa2Rt.anchorMax = Vector2.one;
        fa2Rt.offsetMin = new Vector2(2f, 2f); fa2Rt.offsetMax = new Vector2(-2f, -2f);

        GameObject f2 = new GameObject("Fill"); f2.transform.SetParent(fa2.transform, false);
        bossHpFill = f2.AddComponent<Image>(); bossHpFill.color = ColBossHP;
        RectTransform f2Rt = f2.GetComponent<RectTransform>();
        f2Rt.anchorMin = Vector2.zero; f2Rt.anchorMax = Vector2.one;
        f2Rt.offsetMin = Vector2.zero; f2Rt.offsetMax = Vector2.zero;

        bossHpSlider.fillRect     = f2Rt;
        bossHpSlider.targetGraphic = bossHpFill;
        bossHpSlider.interactable  = false;
        bossHpSlider.minValue = 0f; bossHpSlider.maxValue = 1f;

        bossHpText = BuildLabel(bossHpPanel.transform, "BossHPText",
            new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 5f), 11f, ColText, TextAlignmentOptions.Center);

        bossHpPanel.SetActive(false);
    }
}
