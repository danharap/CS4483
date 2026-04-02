using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Simple NPC dialogue interaction.
/// - Player enters trigger -> can press E to interact
/// - Guide mode supports questions 1-4
/// - Lore mode shows random one-line flavor text
/// Requires Collider IsTrigger = true and player tagged "Player".
/// </summary>
public class NPCDialogue : MonoBehaviour
{
    public enum DialogueMode { Guide, Lore }

    [Header("Mode")]
    [SerializeField] private DialogueMode mode = DialogueMode.Guide;

    [Header("Input")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;

    [Header("UI (optional - auto-created)")]
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private GameObject dialogueRoot;

    [Header("Prompt")]
    [SerializeField] private bool showPromptWhenInRange = true;
    [SerializeField] private string promptText = "Press E to interact";

    private bool playerInRange;
    private bool guideMenuOpen;

    private const string GuideDefault =
        "You\'re new.\n\n" +
        "You won\'t last long without understanding this place.\n\n" +
        "[1] What is this place?\n" +
        "[2] How do upgrades work?\n" +
        "[3] How do I survive?\n" +
        "[4] Who is the King?";

    private static readonly string[] LoreLines =
    {
        "I\'ve seen dozens fall. You won\'t be different.",
        "They enjoy watching us struggle.",
        "Don\'t trust the power they give you.",
        "The King always wins."
    };

    void Awake()
    {
        EnsureDialogueUI();
        SetVisible(false);
    }

    void Update()
    {
        if (!playerInRange) return;

        if (Input.GetKeyDown(interactKey))
        {
            if (mode == DialogueMode.Guide)
            {
                guideMenuOpen = true;
                SetVisible(true);
                SetText(GuideDefault);
            }
            else
            {
                SetVisible(true);
                SetText(LoreLines[Random.Range(0, LoreLines.Length)]);
            }
        }

        if (mode == DialogueMode.Guide && guideMenuOpen)
            HandleGuideQuestions();
    }

    private void HandleGuideQuestions()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
            SetText("A prison disguised as entertainment.\n\nThey call it a game.\n\nIt is not.");
        else if (Input.GetKeyDown(KeyCode.Alpha2))
            SetText("Kill enough enemies and pick up their orbs to level up and choose an upgrade. They shape your abilities. No two runs are the same.");
        else if (Input.GetKeyDown(KeyCode.Alpha3))
            SetText("Keep moving.\n\nHesitation gets you killed.\n\nLearn the patterns.");
        else if (Input.GetKeyDown(KeyCode.Alpha4))
            SetText("The one who never fights... until the end.\n\nIf you reach him... you\'ll understand.");
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = true;
        if (showPromptWhenInRange)
        {
            SetVisible(true);
            SetText(promptText);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = false;
        guideMenuOpen = false;
        SetVisible(false);
    }

    private void EnsureDialogueUI()
    {
        if (dialogueText != null) return;

        GameObject canvasGo = GameObject.Find("Canvas_HUD");
        if (canvasGo == null)
        {
            Canvas c = FindFirstObjectByType<Canvas>();
            if (c != null) canvasGo = c.gameObject;
        }
        if (canvasGo == null) return;

        dialogueRoot = new GameObject($"{name}_DialoguePanel");
        dialogueRoot.transform.SetParent(canvasGo.transform, false);

        Image bg = dialogueRoot.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.6f);

        RectTransform panelRt = dialogueRoot.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.5f, 0f);
        panelRt.anchorMax = new Vector2(0.5f, 0f);
        panelRt.pivot = new Vector2(0.5f, 0f);
        panelRt.anchoredPosition = new Vector2(0f, 24f);
        panelRt.sizeDelta = new Vector2(1200f, 300f);

        GameObject textGo = new GameObject("DialogueText");
        textGo.transform.SetParent(dialogueRoot.transform, false);

        dialogueText = textGo.AddComponent<TextMeshProUGUI>();
        dialogueText.fontSize = 40f;
        dialogueText.alignment = TextAlignmentOptions.Center;
        dialogueText.color = Color.white;

        RectTransform textRt = dialogueText.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(12f, 8f);
        textRt.offsetMax = new Vector2(-12f, -8f);
    }

    private void SetText(string value)
    {
        if (dialogueText != null) dialogueText.text = value;
    }

    private void SetVisible(bool visible)
    {
        if (dialogueRoot != null) dialogueRoot.SetActive(visible);
    }
}
