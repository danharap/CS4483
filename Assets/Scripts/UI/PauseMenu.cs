using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Escape-to-pause overlay during gameplay when <see cref="GameManager.State"/> is Playing.
/// Escape closes the settings panel first (if open), then resumes the game.
/// </summary>
public class PauseMenu : MonoBehaviour
{
    static readonly Color ColDim = new Color(0f, 0f, 0f, 0.62f);
    static readonly Color ColPanel = new Color(0.06f, 0.06f, 0.08f, 0.96f);
    static readonly Color ColBorder = new Color(0.80f, 0.65f, 0.18f);
    static readonly Color ColText = new Color(0.92f, 0.90f, 0.84f);

    GameObject overlayRoot;

    void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape)) return;
        EnsureUI();
        if (overlayRoot == null) return;
        if (GameManager.Instance == null) return;

        if (SettingsManager.Instance != null && SettingsManager.Instance.gameObject.activeInHierarchy)
        {
            SettingsManager.Instance.Hide();
            return;
        }

        if (GameManager.Instance.State == GameManager.GameState.Paused)
        {
            GameManager.Instance.ResumeGameMenu();
            return;
        }

        if (GameManager.Instance.State != GameManager.GameState.Playing) return;

        GameManager.Instance.PauseGameMenu();
    }

    public void Show()
    {
        EnsureUI();
        if (overlayRoot != null)
            overlayRoot.SetActive(true);
    }

    public void Hide()
    {
        if (overlayRoot != null)
            overlayRoot.SetActive(false);
    }

    void EnsureUI()
    {
        if (overlayRoot != null) return;

        Canvas canvas = GameObject.Find("Canvas_HUD")?.GetComponent<Canvas>()
                        ?? FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        overlayRoot = new GameObject("PauseMenu_Overlay");
        overlayRoot.transform.SetParent(canvas.transform, false);
        RectTransform rootRt = overlayRoot.AddComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = Vector2.zero;
        rootRt.offsetMax = Vector2.zero;
        rootRt.SetAsLastSibling();

        Image dim = overlayRoot.AddComponent<Image>();
        dim.color = ColDim;
        dim.raycastTarget = true;

        GameObject panel = new GameObject("PausePanel");
        panel.transform.SetParent(overlayRoot.transform, false);
        RectTransform prt = panel.AddComponent<RectTransform>();
        prt.anchorMin = new Vector2(0.5f, 0.5f);
        prt.anchorMax = new Vector2(0.5f, 0.5f);
        prt.pivot = new Vector2(0.5f, 0.5f);
        prt.anchoredPosition = Vector2.zero;
        prt.sizeDelta = new Vector2(360f, 280f);
        Image pimg = panel.AddComponent<Image>();
        pimg.color = ColPanel;

        TMP_Text title = BuildTmp(panel.transform, "Title", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0f, -28f), 28f, ColBorder, FontStyles.Bold, "PAUSED");

        TMP_Text hint = BuildTmp(panel.transform, "Hint", new Vector2(0.1f, 0f), new Vector2(0.9f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0f, 22f), 13f, ColText, FontStyles.Normal, "Press Escape to resume");

        BuildButton(panel.transform, "ResumeButton",
            new Vector2(0.5f, 0.55f), new Vector2(0.5f, 0.55f),
            new Vector2(0.5f, 0.55f), new Vector2(0f, 10f),
            200f, 40f, "RESUME", () =>
            {
                GameAudio.PlayButtonClick();
                GameManager.Instance?.ResumeGameMenu();
            });

        BuildButton(panel.transform, "SettingsButton",
            new Vector2(0.5f, 0.38f), new Vector2(0.5f, 0.38f),
            new Vector2(0.5f, 0.38f), Vector2.zero,
            200f, 40f, "AUDIO & SETTINGS", () =>
            {
                GameAudio.PlayButtonClick();
                SettingsManager sm = Object.FindFirstObjectByType<SettingsManager>(FindObjectsInactive.Include);
                if (sm == null)
                {
                    GameObject go = new GameObject("SettingsManager");
                    sm = go.AddComponent<SettingsManager>();
                }
                sm.Show();
            });

        overlayRoot.SetActive(false);
    }

    static TMP_Text BuildTmp(Transform parent, string name, Vector2 amin, Vector2 amax, Vector2 pivot, Vector2 pos,
        float size, Color col, FontStyles style, string text)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        TMP_Text t = go.AddComponent<TextMeshProUGUI>();
        t.fontSize = size;
        t.color = col;
        t.fontStyle = style;
        t.text = text;
        t.alignment = TextAlignmentOptions.Center;
        t.enableWordWrapping = false;
        t.overflowMode = TextOverflowModes.Overflow;
        RectTransform rt = t.rectTransform;
        rt.anchorMin = amin;
        rt.anchorMax = amax;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = Vector2.zero;
        if (Mathf.Approximately(amin.x, amax.x) && Mathf.Approximately(amin.y, amax.y) && rt.sizeDelta.sqrMagnitude < 0.01f)
            rt.sizeDelta = new Vector2(280f, size + 10f);
        else if (Mathf.Approximately(amin.y, amax.y) && rt.sizeDelta.y < 1f)
            rt.sizeDelta = new Vector2(rt.sizeDelta.x, size + 8f);
        return t;
    }

    static Button BuildButton(Transform parent, string name, Vector2 amin, Vector2 amax, Vector2 pivot, Vector2 pos,
        float w, float h, string label, UnityEngine.Events.UnityAction onClick)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = amin;
        rt.anchorMax = amax;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(w, h);
        Image img = go.AddComponent<Image>();
        img.color = new Color(0.18f, 0.18f, 0.22f, 1f);
        Button b = go.AddComponent<Button>();
        b.targetGraphic = img;
        b.onClick.AddListener(onClick);

        GameObject txtGo = new GameObject("Label");
        txtGo.transform.SetParent(go.transform, false);
        TMP_Text t = txtGo.AddComponent<TextMeshProUGUI>();
        t.text = label;
        t.fontSize = 16f;
        t.color = ColText;
        t.alignment = TextAlignmentOptions.Center;
        RectTransform trt = t.rectTransform;
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;

        return b;
    }
}
