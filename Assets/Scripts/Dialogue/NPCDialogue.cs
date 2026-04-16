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
/// Guide mode flow:
///   E → intro text  →  E → 4-option menu  →  [1-4] answer  →  E → menu  →  E → close
/// Lore mode flow:
///   E → random line  →  E → close
///
/// Requires Collider IsTrigger = true and player tagged "Player".
/// </summary>
public class NPCDialogue : MonoBehaviour
{
    public enum DialogueMode { Guide, Lore }

    private enum GuidePhase { Closed, Intro, Menu, Answer }

    // ── Inspector ─────────────────────────────────────────────────────────

    [Header("Mode")]
    [SerializeField] private DialogueMode mode = DialogueMode.Guide;

    [Header("Speaker Identity")]
    [SerializeField] private string  speakerName          = "Guide";
    [SerializeField] private Sprite  speakerPortrait;
    [Tooltip("Resources path to load portrait at runtime if speakerPortrait is not set.")]
    [SerializeField] private string  portraitResourcePath = "Portraits/TutorialNPC_Portrait";

    [Header("Input")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;

    [Header("UI (auto-created if null)")]
    [SerializeField] private GameObject dialogueRoot;
    [SerializeField] private TMP_Text   speakerNameText;
    [SerializeField] private TMP_Text   dialogueBodyText;
    [SerializeField] private TMP_Text   continuePrompt;

    [Header("Panel Slide Animation")]
    [SerializeField] private float slideInTime  = 0.18f;
    [SerializeField] private float slideOutTime = 0.12f;

    // ── Internal ──────────────────────────────────────────────────────────

    private bool             playerInRange;
    private GuidePhase       guidePhase    = GuidePhase.Closed;
    private bool             guideMenuOpen; // used only by Lore mode
    private RectTransform    panelRt;
    private float            hiddenY;
    private float            shownY;
    private Coroutine        slideCoroutine;
    private TrapSpriteAnimator spriteAnim; // optional — drives Guide NPC idle animation

    private const string GuideIntroText =
        "You're new.\n\nYou won't last long without understanding this place...";

    private const string GuideMenuText =
        "[1] What is this place?       [2] How do upgrades work?\n" +
        "[3] How do I survive?         [4] Who is the King?";

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
        if (speakerPortrait == null && !string.IsNullOrEmpty(portraitResourcePath))
            speakerPortrait = Resources.Load<Sprite>(portraitResourcePath);

        // Cache the sprite animator so we can play it only while dialogue is open
        Transform spriteChild = transform.Find("GuideSprite");
        if (spriteChild != null)
            spriteAnim = spriteChild.GetComponent<TrapSpriteAnimator>();

        BuildDialogueUI();
        SetVisible(false, instant: true);
    }

    void Update()
    {
        if (!playerInRange) return;

        if (Input.GetKeyDown(interactKey))
        {
            if (mode == DialogueMode.Guide)
                HandleGuideInteract();
            else
                HandleLoreInteract();
        }

        if (mode == DialogueMode.Guide && guidePhase == GuidePhase.Menu)
            HandleGuideQuestions();
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = true;
        ShowPanel($"Press [{interactKey}] to speak with {speakerName}");
        UpdatePrompt($"[{interactKey}] Speak ▶");
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = false;
        guidePhase    = GuidePhase.Closed;
        guideMenuOpen = false;
        StopNPCAnimation();
        SetVisible(false);
    }

    // ── Guide dialogue phases ─────────────────────────────────────────────

    private void HandleGuideInteract()
    {
        switch (guidePhase)
        {
            case GuidePhase.Closed:
                guidePhase = GuidePhase.Intro;
                StartNPCAnimation();
                ShowPanel(GuideIntroText);
                UpdatePrompt($"[{interactKey}] Continue ▶");
                break;

            case GuidePhase.Intro:
            case GuidePhase.Answer:
                guidePhase = GuidePhase.Menu;
                ShowPanel(GuideMenuText);
                UpdatePrompt($"[{interactKey}] Close  •  [1–4] Select");
                break;

            case GuidePhase.Menu:
                guidePhase = GuidePhase.Closed;
                StopNPCAnimation();
                SetVisible(false);
                break;
        }
    }

    private void HandleGuideQuestions()
    {
        string answer = null;
        if      (Input.GetKeyDown(KeyCode.Alpha1))
            answer = "A prison disguised as entertainment.\n\nThey call it a game.\n\nIt is not.";
        else if (Input.GetKeyDown(KeyCode.Alpha2))
            answer = "Kill enough enemies and pick up their orbs to level up and choose an upgrade. They shape your abilities. No two runs are the same.";
        else if (Input.GetKeyDown(KeyCode.Alpha3))
            answer = "Keep moving.\n\nHesitation gets you killed.\n\nLearn the patterns.";
        else if (Input.GetKeyDown(KeyCode.Alpha4))
            answer = "The one who never fights... until the end.\n\nIf you reach him... you'll understand.";

        if (answer != null)
        {
            guidePhase = GuidePhase.Answer;
            ShowPanel(answer);
            UpdatePrompt($"[{interactKey}] Back ▶");
        }
    }

    // ── Lore dialogue (Merchant etc.) ─────────────────────────────────────

    private void HandleLoreInteract()
    {
        if (!guideMenuOpen)
        {
            guideMenuOpen = true;
            ShowPanel(LoreLines[Random.Range(0, LoreLines.Length)]);
            UpdatePrompt($"[{interactKey}] Close ▶");
        }
        else
        {
            guideMenuOpen = false;
            SetVisible(false);
        }
    }

    // ── Sprite animation helpers ──────────────────────────────────────────

    private void StartNPCAnimation()
    {
        if (spriteAnim != null) spriteAnim.enabled = true;
    }

    private void StopNPCAnimation()
    {
        if (spriteAnim != null) spriteAnim.enabled = false;
    }

    // ── Panel Helpers ─────────────────────────────────────────────────────

    private void ShowPanel(string text)
    {
        if (dialogueBodyText != null) dialogueBodyText.text = text;
        SetVisible(true);
    }

    private void UpdatePrompt(string text)
    {
        if (continuePrompt != null) continuePrompt.text = text;
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
        const float edgePad    = 20f;

        shownY  = edgePad;
        hiddenY = -(panelH + 10f);

        dialogueRoot = new GameObject($"{name}_DialogueBox");
        dialogueRoot.transform.SetParent(canvasGo.transform, false);

        panelRt = dialogueRoot.AddComponent<RectTransform>();
        panelRt.anchorMin        = new Vector2(0.5f, 0f);
        panelRt.anchorMax        = new Vector2(0.5f, 0f);
        panelRt.pivot            = new Vector2(0.5f, 0f);
        panelRt.sizeDelta        = new Vector2(panelW, panelH);
        panelRt.anchoredPosition = new Vector2(0f, hiddenY);

        Image panelBg = dialogueRoot.AddComponent<Image>();
        panelBg.color = new Color(0.08f, 0.06f, 0.10f, 0.92f);

        // Gold top border
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
        const float frameSz = 172f;

        GameObject portraitFrame = new GameObject("PortraitFrame");
        portraitFrame.transform.SetParent(dialogueRoot.transform, false);
        Image frameBg = portraitFrame.AddComponent<Image>();
        frameBg.color = new Color(0.82f, 0.65f, 0.18f, 1f); // gold border
        RectTransform frameRt = portraitFrame.GetComponent<RectTransform>();
        frameRt.anchorMin        = new Vector2(0f, 0.5f);
        frameRt.anchorMax        = new Vector2(0f, 0.5f);
        frameRt.pivot            = new Vector2(0f, 0.5f);
        frameRt.anchoredPosition = new Vector2(edgePad, 0f);
        frameRt.sizeDelta        = new Vector2(frameSz, frameSz);

        // Inner dark backing (inset 3px from gold border)
        GameObject frameInner = new GameObject("PortraitInner");
        frameInner.transform.SetParent(portraitFrame.transform, false);
        Image innerBg = frameInner.AddComponent<Image>();
        innerBg.color = new Color(0.18f, 0.14f, 0.22f, 1f);
        RectTransform innerRt = frameInner.GetComponent<RectTransform>();
        innerRt.anchorMin = Vector2.zero;
        innerRt.anchorMax = Vector2.one;
        innerRt.offsetMin = new Vector2(3f, 3f);
        innerRt.offsetMax = new Vector2(-3f, -3f);

        // Portrait image (RawImage for UV-crop to show head + chest only)
        GameObject portraitGo = new GameObject("Portrait");
        portraitGo.transform.SetParent(frameInner.transform, false);
        RawImage portraitRaw = portraitGo.AddComponent<RawImage>();
        // Show top 62 % of the sprite — hides legs, focuses on chest + head
        portraitRaw.uvRect = new Rect(0f, 0.38f, 1f, 0.62f);

        RectTransform portRt = portraitRaw.GetComponent<RectTransform>();
        portRt.anchorMin = Vector2.zero;
        portRt.anchorMax = Vector2.one;
        portRt.offsetMin = Vector2.zero;
        portRt.offsetMax = Vector2.zero;

        // Load portrait texture.
        // Editor: use AssetDatabase for the fastest iteration experience.
        // Build: load from Resources (textures must exist under Assets/Resources/Portraits/).
        Texture2D resolvedPortraitTex = null;

#if UNITY_EDITOR
        {
            string texPath = mode == DialogueMode.Guide
                ? "Assets/Sprites/Guide NPC/2.png"
                : "Assets/Sprites/NPC/Merchant_Idle.png";
            resolvedPortraitTex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
        }
#endif

        if (resolvedPortraitTex == null)
        {
            // Resources.Load works in both editor and builds; path is relative to any Resources/ folder.
            string resPath = mode == DialogueMode.Guide
                ? "Portraits/GuideNPC_Portrait"
                : "Portraits/Merchant_Idle";
            resolvedPortraitTex = Resources.Load<Texture2D>(resPath);
        }

        if (resolvedPortraitTex != null)
            portraitRaw.texture = resolvedPortraitTex;
        else if (speakerPortrait != null)
            portraitRaw.texture = speakerPortrait.texture;
        else
            portraitRaw.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);

        // ── Text area ─────────────────────────────────────────────────────
        float textAreaX     = edgePad + frameSz + edgePad;
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
        dialogueBodyText.fontSize           = 26f;
        dialogueBodyText.color              = Color.white;
        dialogueBodyText.alignment          = TextAlignmentOptions.TopLeft;
        dialogueBodyText.enableWordWrapping = true;
        dialogueBodyText.overflowMode       = TextOverflowModes.Ellipsis;

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
        continuePrompt.text      = $"[{interactKey}] Speak ▶";
        continuePrompt.fontSize  = 22f;
        continuePrompt.color     = new Color(0.7f, 0.7f, 0.7f, 0.85f);
        continuePrompt.alignment = TextAlignmentOptions.BottomRight;

        RectTransform promptRt = continuePrompt.GetComponent<RectTransform>();
        promptRt.anchorMin        = new Vector2(1f, 0f);
        promptRt.anchorMax        = new Vector2(1f, 0f);
        promptRt.pivot            = new Vector2(1f, 0f);
        promptRt.anchoredPosition = new Vector2(-edgePad, edgePad);
        promptRt.sizeDelta        = new Vector2(380f, 30f);
    }
}
