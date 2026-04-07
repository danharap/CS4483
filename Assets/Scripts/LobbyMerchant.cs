using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Lobby Merchant (devil) NPC. Opens the skill tree on interact.
/// When the player is in range, shows the same bottom-screen dialogue panel style as <see cref="Dialogue.NPCDialogue"/>
/// (portrait + title + prompt), using the merchant/devil sprite as the portrait.
/// </summary>
public class LobbyMerchant : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private float interactRadius = 2.5f;
    [SerializeField] private KeyCode interactKey  = KeyCode.E;

    [Header("Identity (proximity panel)")]
    [SerializeField] private string speakerDisplayName = "The Devil";

    [Header("References")]
    [SerializeField] private SkillTreeUI skillTreeUI;

    [Header("Sprite")]
    [SerializeField] private Sprite idleSprite;
    [SerializeField] private Sprite blinkSprite;
    [SerializeField] private float blinkDuration = 0.12f;
    [SerializeField] private float blinkIntervalMin = 2f;
    [SerializeField] private float blinkIntervalMax = 5f;

    private Transform playerTransform;
    private SpriteRenderer spriteRenderer;
    private float nextBlinkTime;
    private float blinkEndTime;
    private bool isBlinking;

    // Proximity panel (matches NPCDialogue layout)
    private GameObject     dialogueRoot;
    private RectTransform  panelRt;
    private float          hiddenY;
    private float          shownY;
    private Coroutine      slideCoroutine;
    private bool           lastShouldShowPanel;

    void Start()
    {
        // Build after inspector / SetupAll wiring — Awake runs too early for AddComponent+Serialize flow.
        if (dialogueRoot == null)
        {
            BuildProximityPanel();
            SetPanelVisible(false, instant: true);
        }

        GameObject player = GameObject.FindWithTag("Player");
        if (player != null) playerTransform = player.transform;

        if (skillTreeUI == null)
            skillTreeUI = Object.FindFirstObjectByType<SkillTreeUI>(FindObjectsInactive.Include);

        if (skillTreeUI == null)
            Debug.LogWarning("[LobbyMerchant] SkillTreeUI not found — re-run CS4483 → SETUP EVERYTHING.");

        SetupSprite();
        ScheduleNextBlink();
    }

    void Update()
    {
        UpdateBlink();
        UpdateProximityPanel();
        UpdateInteraction();
    }

    private void SetupSprite()
    {
        MeshRenderer mr = GetComponent<MeshRenderer>();
        if (mr != null) mr.enabled = false;

        Transform existing = transform.Find("Merchant_Sprite");
        GameObject spriteObj;
        if (existing != null)
        {
            spriteObj = existing.gameObject;
            spriteRenderer = spriteObj.GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
                spriteRenderer = spriteObj.AddComponent<SpriteRenderer>();
        }
        else
        {
            spriteObj = new GameObject("Merchant_Sprite");
            spriteObj.transform.SetParent(transform, false);
            spriteObj.transform.localPosition = Vector3.zero;
            spriteRenderer = spriteObj.AddComponent<SpriteRenderer>();
        }

        spriteRenderer.sortingOrder = 10;

        if (idleSprite != null)
            spriteRenderer.sprite = idleSprite;
        else
            Debug.LogWarning("[LobbyMerchant] idleSprite is null — run CS4483 → Slice Sprite Sheets, then SETUP EVERYTHING.");

        if (spriteObj.GetComponent<Billboard>() == null)
            spriteObj.AddComponent<Billboard>();
    }

    private void ScheduleNextBlink()
    {
        nextBlinkTime = Time.time + Random.Range(blinkIntervalMin, blinkIntervalMax);
    }

    private void UpdateBlink()
    {
        if (spriteRenderer == null || idleSprite == null || blinkSprite == null) return;

        if (isBlinking)
        {
            if (Time.time >= blinkEndTime)
            {
                spriteRenderer.sprite = idleSprite;
                isBlinking = false;
                ScheduleNextBlink();
            }
        }
        else if (Time.time >= nextBlinkTime)
        {
            spriteRenderer.sprite = blinkSprite;
            blinkEndTime = Time.time + blinkDuration;
            isBlinking = true;
        }
    }

    private void UpdateProximityPanel()
    {
        if (dialogueRoot == null || playerTransform == null) return;

        float dist = Vector3.Distance(transform.position, playerTransform.position);
        bool inRange = dist <= interactRadius;
        bool skillOpen = skillTreeUI != null && skillTreeUI.IsOpen;
        bool shouldShow = inRange && !skillOpen;

        if (shouldShow == lastShouldShowPanel) return;
        lastShouldShowPanel = shouldShow;
        SetPanelVisible(shouldShow);
    }

    private void UpdateInteraction()
    {
        if (playerTransform == null || skillTreeUI == null) return;

        float dist = Vector3.Distance(transform.position, playerTransform.position);
        bool inRange = dist <= interactRadius;
        bool skillOpen = skillTreeUI.IsOpen;

        if (inRange && !skillOpen && Input.GetKeyDown(interactKey))
            skillTreeUI.Show();
    }

    void OnDisable()
    {
        lastShouldShowPanel = false;
        SetPanelVisible(false, instant: true);
    }

    // ── Panel visibility (same animation idea as NPCDialogue) ─────────────

    private void SetPanelVisible(bool visible, bool instant = false)
    {
        if (dialogueRoot == null) return;

        if (slideCoroutine != null)
        {
            StopCoroutine(slideCoroutine);
            slideCoroutine = null;
        }

        if (instant)
        {
            dialogueRoot.SetActive(false);
            if (panelRt != null) panelRt.anchoredPosition = new Vector2(0f, hiddenY);
            return;
        }

        if (visible)
        {
            dialogueRoot.SetActive(true);
            slideCoroutine = StartCoroutine(SlidePanel(hiddenY, shownY, 0.18f));
        }
        else
            slideCoroutine = StartCoroutine(SlideOutAndHide());
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
        yield return SlidePanel(shownY, hiddenY, 0.12f);
        if (dialogueRoot != null) dialogueRoot.SetActive(false);
    }

    /// <summary>Same bottom bar layout as <see cref="Dialogue.NPCDialogue"/> — portrait + speaker + body + corner prompt.</summary>
    private void BuildProximityPanel()
    {
        GameObject canvasGo = GameObject.Find("Canvas_HUD");
        if (canvasGo == null)
        {
            Canvas c = FindFirstObjectByType<Canvas>();
            if (c != null) canvasGo = c.gameObject;
        }
        if (canvasGo == null) return;

        const float panelH     = 220f;
        const float panelW     = 1280f;
        const float portraitSz = 180f;
        const float edgePad    = 20f;

        shownY  = edgePad;
        hiddenY = -(panelH + 10f);

        dialogueRoot = new GameObject($"{name}_MerchantDialogue");
        dialogueRoot.transform.SetParent(canvasGo.transform, false);

        panelRt = dialogueRoot.AddComponent<RectTransform>();
        panelRt.anchorMin        = new Vector2(0.5f, 0f);
        panelRt.anchorMax        = new Vector2(0.5f, 0f);
        panelRt.pivot            = new Vector2(0.5f, 0f);
        panelRt.sizeDelta        = new Vector2(panelW, panelH);
        panelRt.anchoredPosition = new Vector2(0f, hiddenY);

        Image panelBg = dialogueRoot.AddComponent<Image>();
        panelBg.color = new Color(0.08f, 0.06f, 0.10f, 0.92f);

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

        const float frameSz = 172f;

        GameObject portraitFrame = new GameObject("PortraitFrame");
        portraitFrame.transform.SetParent(dialogueRoot.transform, false);
        Image frameBg = portraitFrame.AddComponent<Image>();
        frameBg.color = new Color(0.82f, 0.65f, 0.18f, 1f);
        RectTransform frameRt = portraitFrame.GetComponent<RectTransform>();
        frameRt.anchorMin        = new Vector2(0f, 0.5f);
        frameRt.anchorMax        = new Vector2(0f, 0.5f);
        frameRt.pivot            = new Vector2(0f, 0.5f);
        frameRt.anchoredPosition = new Vector2(edgePad, 0f);
        frameRt.sizeDelta        = new Vector2(frameSz, frameSz);

        GameObject frameInner = new GameObject("PortraitInner");
        frameInner.transform.SetParent(portraitFrame.transform, false);
        Image innerBg = frameInner.AddComponent<Image>();
        innerBg.color = new Color(0.18f, 0.14f, 0.22f, 1f);
        RectTransform innerRt = frameInner.GetComponent<RectTransform>();
        innerRt.anchorMin = Vector2.zero;
        innerRt.anchorMax = Vector2.one;
        innerRt.offsetMin = new Vector2(3f, 3f);
        innerRt.offsetMax = new Vector2(-3f, -3f);

        GameObject portraitGo = new GameObject("Portrait");
        portraitGo.transform.SetParent(frameInner.transform, false);
        RawImage portraitRaw = portraitGo.AddComponent<RawImage>();
        // Devil sprite: shorter/wider — match TutorialManager crop on merchant portrait
        portraitRaw.uvRect = new Rect(0f, 0.22f, 1f, 0.78f);

        RectTransform portRt = portraitRaw.GetComponent<RectTransform>();
        portRt.anchorMin = Vector2.zero;
        portRt.anchorMax = Vector2.one;
        portRt.offsetMin = Vector2.zero;
        portRt.offsetMax = Vector2.zero;

#if UNITY_EDITOR
        Texture2D portraitTex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Sprites/NPC/Merchant_Idle.png");
        if (portraitTex != null)
            portraitRaw.texture = portraitTex;
        else
#endif
        {
            if (idleSprite != null)
                portraitRaw.texture = idleSprite.texture;
            else
            {
                Texture2D fallback = Resources.Load<Texture2D>("Portraits/Merchant_Idle");
                if (fallback != null) portraitRaw.texture = fallback;
                else portraitRaw.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
            }
        }

        float textAreaX     = edgePad + frameSz + edgePad;
        float textAreaWidth = panelW - textAreaX - edgePad;

        GameObject nameGo = new GameObject("SpeakerName");
        nameGo.transform.SetParent(dialogueRoot.transform, false);
        TMP_Text speakerNameText = nameGo.AddComponent<TextMeshProUGUI>();
        speakerNameText.text      = speakerDisplayName;
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

        GameObject bodyGo = new GameObject("DialogueBody");
        bodyGo.transform.SetParent(dialogueRoot.transform, false);
        TMP_Text dialogueBodyText = bodyGo.AddComponent<TextMeshProUGUI>();
        dialogueBodyText.text               = $"Press [{interactKey}] to open upgrades";
        dialogueBodyText.fontSize         = 26f;
        dialogueBodyText.color              = Color.white;
        dialogueBodyText.alignment          = TextAlignmentOptions.TopLeft;
        dialogueBodyText.enableWordWrapping = true;

        RectTransform bodyRt = dialogueBodyText.GetComponent<RectTransform>();
        bodyRt.anchorMin        = new Vector2(0f, 0f);
        bodyRt.anchorMax        = new Vector2(0f, 1f);
        bodyRt.pivot            = new Vector2(0f, 1f);
        bodyRt.anchoredPosition = new Vector2(textAreaX, -(edgePad + 36f + 8f));
        bodyRt.sizeDelta        = new Vector2(textAreaWidth, -(edgePad * 2f + 36f + 8f + 32f));

        GameObject promptGo = new GameObject("ContinuePrompt");
        promptGo.transform.SetParent(dialogueRoot.transform, false);
        TMP_Text continuePrompt = promptGo.AddComponent<TextMeshProUGUI>();
        continuePrompt.text      = $"[{interactKey}] Open ▶";
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

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
}
