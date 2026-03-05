using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Upgrade selection panel. Pauses game, shows 3 cards, resumes on pick.
/// Wire upgradePanel, 3 button roots (each with TMP_Text name + description), and RarityImages.
/// </summary>
public class UpgradeUI : MonoBehaviour
{
    // ── Inspector References ──────────────────────────────────────────────
    [Header("Panel")]
    [SerializeField] private GameObject upgradePanel;
    [SerializeField] private TMP_Text   titleText;

    [Header("Upgrade Cards (3 required)")]
    [SerializeField] private Button   card0Button;
    [SerializeField] private TMP_Text card0Name;
    [SerializeField] private TMP_Text card0Desc;
    [SerializeField] private Image    card0Bg;

    [SerializeField] private Button   card1Button;
    [SerializeField] private TMP_Text card1Name;
    [SerializeField] private TMP_Text card1Desc;
    [SerializeField] private Image    card1Bg;

    [SerializeField] private Button   card2Button;
    [SerializeField] private TMP_Text card2Name;
    [SerializeField] private TMP_Text card2Desc;
    [SerializeField] private Image    card2Bg;

    // ── Rarity Colors ─────────────────────────────────────────────────────
    private static readonly Color CommonColor   = new Color(0.7f, 0.7f, 0.7f);
    private static readonly Color UncommonColor = new Color(0.4f, 0.9f, 0.4f);
    private static readonly Color RareColor     = new Color(0.4f, 0.6f, 1.0f);

    // ── State ─────────────────────────────────────────────────────────────
    private List<UpgradeData> currentOptions;

    void Awake()
    {
        Debug.Log("[UpgradeUI] Awake called!");
        Debug.Log($"[UpgradeUI] upgradePanel: {(upgradePanel != null ? "OK" : "NULL")}");
        Debug.Log($"[UpgradeUI] card0Button: {(card0Button != null ? "OK" : "NULL")}");
        
        if (upgradePanel) upgradePanel.SetActive(false);

        card0Button?.onClick.AddListener(() => SelectUpgrade(0));
        card1Button?.onClick.AddListener(() => SelectUpgrade(1));
        card2Button?.onClick.AddListener(() => SelectUpgrade(2));
    }

    public void Show(bool forceRare)
    {
        Debug.Log("[UpgradeUI] SHOWING UPGRADE PANEL - Game is paused, click a card to continue!");
        
        int wave = GameManager.Instance?.WaveManager?.WaveIndex ?? 0;
        currentOptions = UpgradeManager.Instance?.GetOptions(wave, forceRare)
                         ?? new List<UpgradeData>();

        PopulateCard(0, card0Name, card0Desc, card0Bg);
        PopulateCard(1, card1Name, card1Desc, card1Bg);
        PopulateCard(2, card2Name, card2Desc, card2Bg);

        if (titleText)
        {
            titleText.text = forceRare ? "BOSS REWARD – Choose an Upgrade!" : "LEVEL UP! CHOOSE AN UPGRADE";
            Debug.Log($"[UpgradeUI] Title text set: {titleText.text}, Color: {titleText.color}");
        }
        
        if (upgradePanel)
        {
            Debug.Log($"[UpgradeUI] BEFORE SetActive - Panel state: {upgradePanel.activeSelf}");
            upgradePanel.SetActive(true);
            Debug.Log($"[UpgradeUI] IMMEDIATELY AFTER SetActive(true) - Panel state: {upgradePanel.activeSelf}");
            
            if (!upgradePanel.activeSelf)
            {
                Debug.LogError("[UpgradeUI] CRITICAL: Panel did not activate! Checking parent...");
                if (upgradePanel.transform.parent != null)
                {
                    Debug.LogError($"[UpgradeUI] Parent is: {upgradePanel.transform.parent.name}, Parent active: {upgradePanel.transform.parent.gameObject.activeSelf}");
                }
            }
            
            upgradePanel.transform.SetAsLastSibling();
            
            // DEBUG: Make panel bright magenta so we can see if it renders at all
            var img = upgradePanel.GetComponent<UnityEngine.UI.Image>();
            if (img)
            {
                img.color = new Color(1f, 0f, 1f, 1f);
                Debug.Log($"[UpgradeUI] Panel Image color set to BRIGHT MAGENTA for visibility test, Canvas: {img.canvas}");
            }
            
            // Check RectTransform
            var rt = upgradePanel.GetComponent<RectTransform>();
            if (rt)
            {
                Debug.Log($"[UpgradeUI] RectTransform - AnchorMin: {rt.anchorMin}, AnchorMax: {rt.anchorMax}, SizeDelta: {rt.sizeDelta}");
                Debug.Log($"[UpgradeUI] RectTransform - AnchoredPosition: {rt.anchoredPosition}, LocalPosition: {rt.localPosition}");
            }
            
            Debug.Log($"[UpgradeUI] FINAL CHECK - Panel active: {upgradePanel.activeSelf}, WorldPosition: {upgradePanel.transform.position}, Options: {currentOptions.Count}");
            Debug.Log($"[UpgradeUI] Panel parent: {upgradePanel.transform.parent?.name}, Sibling index: {upgradePanel.transform.GetSiblingIndex()}");
        }
        else
        {
            Debug.LogError("[UpgradeUI] upgradePanel is NULL!");
        }
    }

    private void PopulateCard(int idx, TMP_Text nameText, TMP_Text descText, Image bg)
    {
        if (idx >= currentOptions.Count) return;
        UpgradeData u = currentOptions[idx];
        if (nameText) nameText.text = u.DisplayName;
        if (descText) descText.text = u.Description;
        if (bg)       bg.color = RarityColor(u.Rarity);
    }

    private void SelectUpgrade(int idx)
    {
        if (idx >= currentOptions.Count) return;
        UpgradeManager.Instance?.Apply(currentOptions[idx]);
        Hide();
        GameManager.Instance?.ResumeAfterUpgrade();
    }

    private void Hide()
    {
        if (upgradePanel) upgradePanel.SetActive(false);
    }

    private Color RarityColor(UpgradeRarity r) => r switch
    {
        UpgradeRarity.Common   => CommonColor,
        UpgradeRarity.Uncommon => UncommonColor,
        UpgradeRarity.Rare     => RareColor,
        _                      => CommonColor
    };
}
