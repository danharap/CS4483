using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Base interactive narrative trigger. When the player enters the trigger zone,
/// displays a text pop-up (once per session by default) and optionally plays
/// an audio clip. Satisfies Activity 3 Step 3: Interactive Narrative Triggers.
/// Requires a Collider with Is Trigger = true on this GameObject.
/// </summary>
public class NarrativeTrigger : MonoBehaviour
{
    // ── Tunables ──────────────────────────────────────────────────────────
    [Header("Narrative Text")]
    [SerializeField] protected string narrativeTitle   = "Echo";
    [TextArea(3, 6)]
    [SerializeField] protected string narrativeBody    = "A whisper from the past...";
    [SerializeField] private float    displayDuration  = 5f;
    [SerializeField] private bool     triggerOnce      = true;

    [Header("Audio")]
    [SerializeField] private AudioClip ambientClip;    // drag any audio clip here
    [SerializeField] private float     audioVolume     = 0.6f;

    [Header("UI Reference")]
    [SerializeField] private TMP_Text  titleUI;        // wired by NarrativeSetup
    [SerializeField] private TMP_Text  bodyUI;         // wired by NarrativeSetup
    [SerializeField] private GameObject panelUI;       // the popup panel GameObject

    // ── State ─────────────────────────────────────────────────────────────
    private bool triggered;
    private AudioSource audioSource;

    protected virtual void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && ambientClip != null)
            audioSource = gameObject.AddComponent<AudioSource>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (triggerOnce && triggered) return;
        triggered = true;
        StartCoroutine(ShowNarrative());
    }

    protected virtual IEnumerator ShowNarrative()
    {
        // Show popup
        if (panelUI)  panelUI.SetActive(true);
        if (titleUI)  titleUI.text = narrativeTitle;
        if (bodyUI)   bodyUI.text  = narrativeBody;

        // Play ambient clip
        if (audioSource != null && ambientClip != null)
        {
            audioSource.clip   = ambientClip;
            audioSource.volume = audioVolume;
            audioSource.Play();
        }

        // Fade in
        float t = 0f;
        while (t < 0.3f)
        {
            t += Time.unscaledDeltaTime;
            SetPanelAlpha(Mathf.Lerp(0f, 1f, t / 0.3f));
            yield return null;
        }
        SetPanelAlpha(1f);

        yield return new WaitForSecondsRealtime(displayDuration);

        // Fade out
        t = 0f;
        while (t < 0.5f)
        {
            t += Time.unscaledDeltaTime;
            SetPanelAlpha(Mathf.Lerp(1f, 0f, t / 0.5f));
            yield return null;
        }

        if (panelUI) panelUI.SetActive(false);
    }

    private void SetPanelAlpha(float a)
    {
        if (titleUI) titleUI.alpha = a;
        if (bodyUI)  bodyUI.alpha  = a;
    }

    // Called by NarrativeSetup to wire the shared HUD popup panel
    public void SetUIRefs(GameObject panel, TMP_Text title, TMP_Text body)
    {
        panelUI  = panel;
        titleUI  = title;
        bodyUI   = body;
    }
}
