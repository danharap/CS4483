using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Full-screen Skill Tree overlay. Three anchor-positioned columns, one per track.
/// Nodes are built at runtime as simple Button cards with TMP children.
/// </summary>
public class SkillTreeUI : MonoBehaviour
{
    [Header("References (wired by SetupAll)")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text   levelText;
    [SerializeField] private Slider     xpBar;
    [SerializeField] private TMP_Text   xpLabel;
    [SerializeField] private TMP_Text   skillPointsText;
    [SerializeField] private Button     closeButton;
    [SerializeField] private Button     resetButton;

    [Header("Node Columns (content transforms inside ScrollRects)")]
    [SerializeField] private Transform strikerColumn;
    [SerializeField] private Transform survivorColumn;
    [SerializeField] private Transform skirmisherColumn;

    private readonly Dictionary<string, Button> nodeButtons = new Dictionary<string, Button>();
    private readonly Dictionary<string, Image>  nodeBgs     = new Dictionary<string, Image>();
    private bool builtNodes = false;

    private static readonly Color ColLocked     = new Color(0.13f, 0.13f, 0.16f);
    private static readonly Color ColUnlockable = new Color(0.10f, 0.28f, 0.52f);
    private static readonly Color ColUnlocked   = new Color(0.08f, 0.36f, 0.10f);
    private static readonly Color ColCapstone   = new Color(0.40f, 0.24f, 0.04f);

    void Awake()
    {
        if (panel) panel.SetActive(false);
        if (closeButton) closeButton.onClick.AddListener(Hide);
        if (resetButton) resetButton.onClick.AddListener(ResetProgression);
    }

    void OnEnable()
    {
        if (AccountProgression.Instance != null)
        {
            AccountProgression.Instance.OnProgressionChanged += RefreshHeader;
            AccountProgression.Instance.OnSkillUnlocked      += OnSkillUnlockedRefresh;
        }
    }

    void OnDisable()
    {
        if (AccountProgression.Instance != null)
        {
            AccountProgression.Instance.OnProgressionChanged -= RefreshHeader;
            AccountProgression.Instance.OnSkillUnlocked      -= OnSkillUnlockedRefresh;
        }
    }

    private void OnSkillUnlockedRefresh(string _) => RefreshNodes();

    /// <summary>True while the skill tree overlay is open (hides lobby NPC proximity prompts).</summary>
    public bool IsOpen => panel != null && panel.activeSelf;

    public void Show()
    {
        // Activate the panel BEFORE building nodes so coroutines can start
        if (panel) { panel.SetActive(true); panel.transform.SetAsLastSibling(); }
        // Ensure this MonoBehaviour's GameObject is active for StartCoroutine
        gameObject.SetActive(true);
        if (!builtNodes) BuildAllNodes();
        RefreshAll();
    }

    public void Hide()
    {
        if (panel) panel.SetActive(false);
    }

    // ── Build nodes ───────────────────────────────────────────────────────

    private void BuildAllNodes()
    {
        builtNodes = true;
        BuildTrack("striker",    strikerColumn);
        BuildTrack("survivor",   survivorColumn);
        BuildTrack("skirmisher", skirmisherColumn);
    }

    private void BuildTrack(string trackId, Transform column)
    {
        if (column == null) return;
        var nodes = SkillTreeCatalogue.GetTrack(trackId);

        // Manually position cards using percentage-based anchors within the body.
        // No LayoutGroup — just raw anchors.
        // Each track has 4 nodes. We divide the body height into 4 equal slots
        // with small gaps between them (the arrows).
        int count    = nodes.Count;
        float gap    = 0.01f; // 1% of body height per gap
        float totalGap = gap * (count - 1);
        float slotH  = (1f - totalGap) / count; // fraction of body per card

        for (int i = 0; i < count; i++)
        {
            var  node  = nodes[i];
            bool isCap = node.kind == SkillNodeKind.Capstone;

            // Card anchors: top = 1 - (i * (slotH + gap)), bottom = top - slotH
            float top = 1f - i * (slotH + gap);
            float bot = top - slotH;

            // ── Arrow between cards ───────────────────────────────────────
            if (i > 0)
            {
                float arrowTop = top + gap;
                float arrowBot = top;
                TMP_Text arTxt = AnchoredTMP(column, "Arrow",
                    0f, arrowBot, 1f, arrowTop, 0f, 0f, 0f, 0f);
                arTxt.text      = "▼";
                arTxt.fontSize  = 18f;
                arTxt.alignment = TextAlignmentOptions.Center;
                arTxt.color     = new Color(0.5f, 0.5f, 0.6f);
            }

            // ── Card ──────────────────────────────────────────────────────
            GameObject card = new GameObject(node.id, typeof(RectTransform));
            card.transform.SetParent(column, false);
            RectTransform cardRT = card.GetComponent<RectTransform>();
            cardRT.anchorMin = new Vector2(0f, bot);
            cardRT.anchorMax = new Vector2(1f, top);
            cardRT.offsetMin = Vector2.zero;
            cardRT.offsetMax = Vector2.zero;

            Image bg = card.AddComponent<Image>();
            bg.color = ColLocked;
            nodeBgs[node.id] = bg;

            Button btn = card.AddComponent<Button>();
            string cid = node.id;
            btn.onClick.AddListener(() => OnNodeClicked(cid));
            ColorBlock cb = btn.colors;
            cb.normalColor      = Color.white;
            cb.highlightedColor = new Color(1f, 1f, 1f, 0.85f);
            cb.pressedColor     = new Color(0.7f, 0.7f, 0.7f);
            cb.disabledColor    = new Color(1f, 1f, 1f, 0.30f);
            cb.colorMultiplier  = 1f;
            btn.colors = cb;
            nodeButtons[node.id] = btn;

            // ── Card internals (percentage of card height) ────────────────
            // All use stretch anchors within the card

            // Capstone gold accent (top 5% of card)
            if (isCap)
            {
                AnchoredRect(card.transform, "Accent",
                    0f, 0.95f, 1f, 1f, 0f, 0f, 0f, 0f,
                    new Color(1f, 0.78f, 0.12f));
            }

            // Name (top 30% of card)
            float nameBot2 = isCap ? 0.65f : 0.68f;
            TMP_Text nameTxt = AnchoredTMP(card.transform, "Name",
                0f, nameBot2, 1f, isCap ? 0.93f : 0.97f, 8f, 0f, -8f, -2f);
            nameTxt.text               = (isCap ? "[MAX] " : "") + node.displayName;
            nameTxt.enableAutoSizing   = true;
            nameTxt.fontSizeMin        = 12f;
            nameTxt.fontSizeMax        = 22f;
            nameTxt.fontStyle          = FontStyles.Bold;
            nameTxt.color              = Color.white;
            nameTxt.alignment          = TextAlignmentOptions.Left;
            nameTxt.enableWordWrapping = true;
            nameTxt.overflowMode       = TextOverflowModes.Ellipsis;

            // Divider (thin line)
            AnchoredRect(card.transform, "Div",
                0f, nameBot2 - 0.01f, 1f, nameBot2, 8f, 0f, -8f, 0f,
                new Color(1f, 1f, 1f, 0.15f));

            // Description (middle ~40% of card)
            float descTop2 = 0.22f;
            float descBot2 = nameBot2 - 0.03f;
            TMP_Text descTxt = AnchoredTMP(card.transform, "Desc",
                0f, descTop2, 1f, descBot2, 8f, 0f, -8f, 0f);
            descTxt.text               = node.description;
            descTxt.enableAutoSizing   = true;
            descTxt.fontSizeMin        = 9f;
            descTxt.fontSizeMax        = 15f;
            descTxt.color              = new Color(0.80f, 0.83f, 0.95f);
            descTxt.alignment          = TextAlignmentOptions.TopLeft;
            descTxt.enableWordWrapping = true;
            descTxt.overflowMode       = TextOverflowModes.Ellipsis;

            // Cost bar (bottom 20% of card)
            AnchoredRect(card.transform, "CostBg",
                0f, 0f, 1f, 0.20f, 0f, 0f, 0f, 0f,
                new Color(0f, 0f, 0f, 0.35f));
            TMP_Text costTxt = AnchoredTMP(card.transform, "CostTxt",
                0f, 0f, 1f, 0.20f, 4f, 0f, -4f, 0f);
            costTxt.text      = $"Cost: {node.cost} SP";
            costTxt.enableAutoSizing = true;
            costTxt.fontSizeMin = 10f;
            costTxt.fontSizeMax = 15f;
            costTxt.fontStyle = FontStyles.Bold;
            costTxt.color     = new Color(1f, 0.85f, 0.25f);
            costTxt.alignment = TextAlignmentOptions.Center;
        }
    }

    // ── Refresh ───────────────────────────────────────────────────────────

    private void RefreshAll()
    {
        int metaXP = AccountProgression.Instance?.MetaXP          ?? 0;
        int xpMax  = AccountProgression.Instance?.XPToNextLevel()  ?? 500;
        int level  = AccountProgression.Instance?.AccountLevel     ?? 1;
        int sp     = AccountProgression.Instance?.SkillPoints      ?? 0;
        RefreshHeader(metaXP, xpMax, level, sp);
        RefreshNodes();
    }

    private void RefreshHeader(int metaXP, int xpMax, int level, int sp)
    {
        if (levelText)       levelText.text       = $"Account Level  {level}";
        if (skillPointsText) skillPointsText.text = $"{sp} Skill Point{(sp != 1 ? "s" : "")} available";
        if (xpBar)           { xpBar.maxValue = xpMax; xpBar.value = metaXP; }
        if (xpLabel)         xpLabel.text         = $"{metaXP} / {xpMax} XP to next level";
    }

    private void RefreshNodes()
    {
        if (AccountProgression.Instance == null) return;
        var ap = AccountProgression.Instance;

        foreach (var node in SkillTreeCatalogue.All)
        {
            if (!nodeBgs.TryGetValue(node.id, out Image bg)) continue;
            if (!nodeButtons.TryGetValue(node.id, out Button btn)) continue;

            bool isUnlocked = ap.IsUnlocked(node.id);
            bool canUnlock  = ap.CanUnlock(node.id);
            bool isCap      = node.kind == SkillNodeKind.Capstone;

            if (isUnlocked)      bg.color = isCap ? ColCapstone : ColUnlocked;
            else if (canUnlock)  bg.color = ColUnlockable;
            else                 bg.color = ColLocked;

            btn.interactable = canUnlock && !isUnlocked;
        }
    }

    private void OnNodeClicked(string id)
    {
        if (AccountProgression.Instance == null) return;
        AccountProgression.Instance.TryUnlockSkill(id);
        RefreshAll();
    }

    private void ResetProgression()
    {
#if UNITY_EDITOR
        AccountProgression.Instance?.DebugResetAll();
        RefreshAll();
#endif
    }

    // ── UI helpers ────────────────────────────────────────────────────────
    // Use percentage-based anchors so elements scale with their parent.
    // Pixel offsets are added on top for fine-tuning (padding).

    private static void AnchoredRect(Transform parent, string name,
        float aMinX, float aMinY, float aMaxX, float aMaxY,
        float oMinX, float oMinY, float oMaxX, float oMaxY, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(aMinX, aMinY);
        rt.anchorMax = new Vector2(aMaxX, aMaxY);
        rt.offsetMin = new Vector2(oMinX, oMinY);
        rt.offsetMax = new Vector2(oMaxX, oMaxY);
        Image img = go.AddComponent<Image>();
        img.color = color;
    }

    private static TMP_Text AnchoredTMP(Transform parent, string name,
        float aMinX, float aMinY, float aMaxX, float aMaxY,
        float oMinX, float oMinY, float oMaxX, float oMaxY)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(aMinX, aMinY);
        rt.anchorMax = new Vector2(aMaxX, aMaxY);
        rt.offsetMin = new Vector2(oMinX, oMinY);
        rt.offsetMax = new Vector2(oMaxX, oMaxY);
        return go.AddComponent<TextMeshProUGUI>();
    }
}
