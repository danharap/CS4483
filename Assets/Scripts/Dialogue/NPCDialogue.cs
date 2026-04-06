using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Stardew Valley-style NPC dialogue.
///
/// Layout (bottom of screen):
///   ┌──────────────────────────────────────────────────────────────┐
///   │ [Portrait] │ SpeakerName                                     │
///   │            │ Dialogue text here...                           │
///   │            │                                          [E] ▶  │
///   └──────────────────────────────────────────────────────────────┘
///
/// Usage:
///   - Assign speakerPortrait and speakerName in the Inspector.
///   - Guide mode answers questions 1-4 via keyboard.
///   - Lore mode shows random one-line flavor text.
///   - The panel animates in/out with a small vertical slide.
/// Requires Collider IsTrigger = true and player tagged "Player".
/// </summary>
public class NPCDialogue : MonoBehaviour
{
    public enum DialogueMode { Guide, Lore }

    // ── Inspector ─────────────────────────────────────────────────────────

    [Header("Mode")]
    [SerializeField] private DialogueMode mode = DialogueMode.Guide;

    [Header("Speaker Identity")]
    [SerializeField] private string     speakerName         = "Guide";
    [SerializeField] private Sprite     speakerPortrait;
    [Tooltip("Resources path to load portrait at runtime if speakerPortrait is not set (e.g. 'Portraits/TutorialNPC_Portrait').")]
    [SerializeField] private string     portraitResourcePath = "Portraits/TutorialNPC_Portrait";

    [Header("Input")]
    [SerializeField] private KeyCode    interactKey    = KeyCode.E;

    [Header("UI (auto-created if null)")]
    [SerializeField] private GameObject dialogueRoot;
    [SerializeField] private TMP_Text   speakerNameText;
    [SerializeField] private TMP_Text   dialogueBodyText;
    [SerializeField] private Image      portraitImage;
    [SerializeField] private TMP_Text   continuePrompt;

    [Header("Panel Slide Animation")]
    [SerializeField] private float slideInTime  = 0.18f;
    [SerializeField] private float slideOutTime = 0.12f;

    // ── Internal ──────────────────────────────────────────────────────────

    private bool playerInRange;
    private bool guideMenuOpen;
    private RectTransform panelRt;
    private float hiddenY;
    private float shownY;
    private Coroutine slideCoroutine;

    private const string GuideDefault =
        "You're new.\n\n" +
        "You won't last long without understanding this place.\n\n" +
        "[1] What is this place?     [2] How do upgrades work?\n" +
        "[3] How do I survive?       [4] Who is the King?";

    private static readonly string[] LoreLines =
    {
        "I've seen dozens fall. You won't be different.",
        "They enjoy watching us struggle.",
        "Don't trust the power they give you.",
        "The King always wins."
    };

    // ── Unity Lifecycle ───────────────────────────────────────────────────

    void Awake()
    {
        // Auto-load portrait from Resources if not assigned in Inspector
        if (speakerPortrait == null && !string.IsNullOrEmpty(portraitResourcePath))
            speakerPortrait = Resources.Load<Sprite>(portraitResourcePath);

        BuildDialogueUI();
        SetVisible(false, instant: true);
    }

    void Update()
    {
        if (!playerInRange) return;

        if (Input.GetKeyDown(interactKey))
        {
            if (!guideMenuOpen)
            {
                if (mode == DialogueMode.Guide)
                {
                    guideMenuOpen = true;
                    ShowPanel(GuideDefault);
                }
                else
                {
                    ShowPanel(LoreLines[Random.Range(0, LoreLines.Length)]);
                }
            }
            else
            {
                // Second press closes
                guideMenuOpen = false;
                SetVisible(false);
            }
        }

        if (mode == DialogueMode.Guide && guideMenuOpen)
            HandleGuideQuestions();
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = true;
        ShowPanel($"Press [{interactKey}] to speak with {speakerName}");
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange  = false;
        guideMenuOpen  = false;
        SetVisible(false);
    }

    // ── Guide Questions ───────────────────────────────────────────────────

    private void HandleGuideQuestions()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
            ShowPanel("A prison disguised as entertainment.\n\nThey call it a game.\n\nIt is not.");
        else if (Input.GetKeyDown(KeyCode.Alpha2))
            ShowPanel("Kill enough enemies and pick up their orbs to level up and choose an upgrade. They shape your abilities. No two runs are the same.");
        else if (Input.GetKeyDown(KeyCode.Alpha3))
            ShowPanel("Keep moving.\n\nHesitation gets you killed.\n\nLearn the patterns.");
        else if (Input.GetKeyDown(KeyCode.Alpha4))
            ShowPanel("The one who never fights... until the end.\n\nIf you reach him... you'll understand.");
    }

    // ── Panel Helpers ─────────────────────────────────────────────────────

    private void ShowPanel(string text)
    {
        if (dialogueBodyText != null) dialogueBodyText.text = text;
        SetVisible(true);
    }

    private void SetVisible(bool visible, bool instant = false)
    {
        if (dialogueRoot == null) return;

        if (slideCoroutine != null) StopCoroutine(slideCoroutine);

        if (instant)
        {
            dialogueRoot.SetActive(false);
            if (panelRt != null) panelRt.anchoredPosition = new Vector2(0f, hiddenY);
            return;
        }

        if (visible)
        {
            dialogueRoot.SetActive(true);
            slideCoroutine = StartCoroutine(SlidePanel(hiddenY, shownY, slideInTime));
        }
        else
        {
            slideCoroutine = StartCoroutine(SlideOutAndHide());
        }
    }

    private IEnumerator SlidePanel(float fromY, float toY, float duration)
    {
        if (panelRt == null) yield break;
        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / duration));
            panelRt.anchoredPosition = new Vector2(0f, Mathf.Lerp(fromY, toY, u));
            yield return null;
        }
        panelRt.anchoredPosition = new Vector2(0f, toY);
    }

    private IEnumerator SlideOutAndHide()
    {
        yield return SlidePanel(shownY, hiddenY, slideOutTime);
        if (dialogueRoot != null) dialogueRoot.SetActive(false);
    }

    // ── UI Builder ────────────────────────────────────────────────────────

    private void BuildDialogueUI()
    {
        if (dialogueBodyText != null && dialogueRoot != null) return;

        GameObject canvasGo = GameObject.Find("Canvas_HUD");
        if (canvasGo == null)
        {
            Canvas c = FindFirstObjectByType<Canvas>();
            if (c != null) canvasGo = c.gameObject;
        }
        if (canvasGo == null) return;

        // ── Root panel ────────────────────────────────────────────────────
        const float panelH     = 220f;
        const float panelW     = 1280f;
        const float portraitSz = 180f;
        const float edgePad    = 20f;

        shownY  = edgePad;          // slightly off the bottom edge
        hiddenY = -(panelH + 10f);  // fully below screen

        dialogueRoot = new GameObject($"{name}_DialogueBox");
        dialogueRoot.transform.SetParent(canvasGo.transform, false);

        panelRt = dialogueRoot.AddComponent<RectTransform>();
        panelRt.anchorMin        = new Vector2(0.5f, 0f);
        panelRt.anchorMax        = new Vector2(0.5f, 0f);
        panelRt.pivot            = new Vector2(0.5f, 0f);
        panelRt.sizeDelta        = new Vector2(panelW, panelH);
        panelRt.anchoredPosition = new Vector2(0f, hiddenY);

        // Dark semi-transparent backing
        Image panelBg = dialogueRoot.AddComponent<Image>();
        panelBg.color = new Color(0.08f, 0.06f, 0.10f, 0.92f);

        // Coloured top border strip (warm gold accent like Stardew)
        GameObject border = new GameObject("TopBorder");
        border.transform.SetParent(dialogueRoot.transform, false);
        Image borderImg = border.AddComponent<Image>();
        borderImg.color = new Color(0.82f, 0.65f, 0.18f, 1f);
        RectTransform borderRt = border.GetComponent<RectTransform>();
        borderRt.anchorMin        = new Vector2(0f, 1f);
        borderRt.anchorMax        = new Vector2(1f, 1f);
        borderRt.pivot            = new Vector2(0.5f, 1f);
        borderRt.anchoredPosition = Vector2.zero;
        borderRt.sizeDelta        = new Vector2(0f, 4f);

        // ── Portrait frame ────────────────────────────────────────────────
        GameObject portraitFrame = new GameObject("PortraitFrame");
        portraitFrame.transform.SetParent(dialogueRoot.transform, false);
        Image frameBg = portraitFrame.AddComponent<Image>();
        frameBg.color = new Color(0.18f, 0.14f, 0.22f, 1f);
        RectTransform frameRt = portraitFrame.GetComponent<RectTransform>();
        frameRt.anchorMin        = new Vector2(0f, 0.5f);
        frameRt.anchorMax        = new Vector2(0f, 0.5f);
        frameRt.pivot            = new Vector2(0f, 0.5f);
        frameRt.anchoredPosition = new Vector2(edgePad, 0f);
        frameRt.sizeDelta        = new Vector2(portraitSz, portraitSz);

        // Portrait image inside frame
        GameObject portraitGo = new GameObject("Portrait");
        portraitGo.transform.SetParent(portraitFrame.transform, false);
        portraitImage = portraitGo.AddComponent<Image>();
        portraitImage.preserveAspect = true;
        if (speakerPortrait != null)
            portraitImage.sprite = speakerPortrait;
        else
            portraitImage.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);

        RectTransform portRt = portraitImage.GetComponent<RectTransform>();
        portRt.anchorMin = new Vector2(0.05f, 0.05f);
        portRt.anchorMax = new Vector2(0.95f, 0.95f);
        portRt.offsetMin = Vector2.zero;
        portRt.offsetMax = Vector2.zero;

        // ── Text area ─────────────────────────────────────────────────────
        float textAreaX     = edgePad + portraitSz + edgePad;
        float textAreaWidth = panelW - textAreaX - edgePad;

        // Speaker name
        GameObject nameGo = new GameObject("SpeakerName");
        nameGo.transform.SetParent(dialogueRoot.transform, false);
        speakerNameText = nameGo.AddComponent<TextMeshProUGUI>();
        speakerNameText.text      = speakerName;
        speakerNameText.fontSize  = 28f;
        speakerNameText.fontStyle = FontStyles.Bold;
        speakerNameText.color     = new Color(0.95f, 0.85f, 0.45f, 1f);
        speakerNameText.alignment = TextAlignmentOptions.TopLeft;

        RectTransform nameRt = speakerNameText.GetComponent<RectTransform>();
        nameRt.anchorMin        = new Vector2(0f, 1f);
        nameRt.anchorMax        = new Vector2(0f, 1f);
        nameRt.pivot            = new Vector2(0f, 1f);
        nameRt.anchoredPosition = new Vector2(textAreaX, -edgePad);
        nameRt.sizeDelta        = new Vector2(textAreaWidth, 36f);

        // Dialogue body
        GameObject bodyGo = new GameObject("DialogueBody");
        bodyGo.transform.SetParent(dialogueRoot.transform, false);
        dialogueBodyText = bodyGo.AddComponent<TextMeshProUGUI>();
        dialogueBodyText.fontSize             = 26f;
        dialogueBodyText.color                = Color.white;
        dialogueBodyText.alignment            = TextAlignmentOptions.TopLeft;
        dialogueBodyText.enableWordWrapping   = true;
        dialogueBodyText.overflowMode         = TextOverflowModes.Ellipsis;

        RectTransform bodyRt = dialogueBodyText.GetComponent<RectTransform>();
        bodyRt.anchorMin        = new Vector2(0f, 0f);
        bodyRt.anchorMax        = new Vector2(0f, 1f);
        bodyRt.pivot            = new Vector2(0f, 1f);
        bodyRt.anchoredPosition = new Vector2(textAreaX, -(edgePad + 36f + 8f));
        bodyRt.sizeDelta        = new Vector2(textAreaWidth, -(edgePad * 2f + 36f + 8f + 32f));

        // Continue prompt (bottom-right)
        GameObject promptGo = new GameObject("ContinuePrompt");
        promptGo.transform.SetParent(dialogueRoot.transform, false);
        continuePrompt = promptGo.AddComponent<TextMeshProUGUI>();
        continuePrompt.text      = $"[{interactKey}] Close  ▶";
        continuePrompt.fontSize  = 22f;
        continuePrompt.color     = new Color(0.7f, 0.7f, 0.7f, 0.85f);
        continuePrompt.alignment = TextAlignmentOptions.BottomRight;

        RectTransform promptRt = continuePrompt.GetComponent<RectTransform>();
        promptRt.anchorMin        = new Vector2(1f, 0f);
        promptRt.anchorMax        = new Vector2(1f, 0f);
        promptRt.pivot            = new Vector2(1f, 0f);
        promptRt.anchoredPosition = new Vector2(-edgePad, edgePad);
        promptRt.sizeDelta        = new Vector2(300f, 30f);
    }
}
