using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// Narrative presence for the Satan boss.
///
/// Hooks:
///   • Wave 1 starts  → camera pans to Satan's throne, he delivers a taunt line.
///   • Wave 5 boss dies → camera pans again, second line.
///
/// Attach this to a persistent manager object (e.g. the GameManager or a dedicated
/// "NarrativeManager" in the scene). Wire the throne position and the dialogue lines
/// in the Inspector.
///
/// Dialogue is displayed using the existing NPCDialogue system if available, or falls
/// back to a temporary full-screen text label created at runtime.
/// </summary>
public class SatanTauntController : MonoBehaviour
{
    [Header("Camera Pan Target")]
    [Tooltip("World position to pan the camera to (e.g. Satan's throne location).")]
    [SerializeField] private Transform throneTransform;
    [SerializeField] private Vector3   throneFallbackPosition = new Vector3(0f, 0f, 60f);

    [Header("Pan Timing")]
    [SerializeField] private float travelTime = 1.0f;
    [SerializeField] private float holdTime   = 2.8f;

    [Header("Taunt Lines")]
    [TextArea(2, 4)]
    [SerializeField] private string wave1TauntLine = "You dare enter my domain? I will enjoy watching you suffer.";
    [TextArea(2, 4)]
    [SerializeField] private string afterBoss1Line = "A satisfying spectacle. But your champion still awaits his true test.";

    [Header("Dialogue UI")]
    [Tooltip("Name shown in the taunt banner. Leave blank for 'The Devil'.")]
    [SerializeField] private string speakerName = "The Devil";
    [SerializeField] private float  textFadeTime = 0.35f;

    // Runtime
    private bool wave1TauntDone;
    private bool boss1TauntDone;

    // Fallback UI
    private TMP_Text fallbackText;
    private Coroutine tauntCoroutine;

    void OnEnable()
    {
        var wm = GameManager.Instance?.WaveManager;
        if (wm == null) { StartCoroutine(WaitAndSubscribe()); return; }
        Subscribe(wm);
    }

    private IEnumerator WaitAndSubscribe()
    {
        while (GameManager.Instance?.WaveManager == null)
            yield return null;
        Subscribe(GameManager.Instance.WaveManager);
    }

    private void Subscribe(WaveManager wm)
    {
        wm.OnWaveStart  += OnWaveStart;
        wm.OnBossKilled += OnBossKilled;
    }

    void OnDisable()
    {
        var wm = GameManager.Instance?.WaveManager;
        if (wm == null) return;
        wm.OnWaveStart  -= OnWaveStart;
        wm.OnBossKilled -= OnBossKilled;
    }

    private void OnWaveStart(int waveIndex)
    {
        if (waveIndex == 0 && !wave1TauntDone)
        {
            wave1TauntDone = true;
            if (tauntCoroutine != null) StopCoroutine(tauntCoroutine);
            tauntCoroutine = StartCoroutine(TauntSequence(wave1TauntLine));
        }
    }

    private void OnBossKilled()
    {
        // Fires after the wave-5 mini-boss is killed (Arena 1).
        // Only trigger if we're in Arena 1 (Arena 2 is not active yet).
        GameObject arena2 = GameObject.Find("=== LEVEL (ProBuilder) Arena2 ===");
        if (arena2 != null && arena2.activeInHierarchy) return; // Satan is the next boss, skip

        if (!boss1TauntDone)
        {
            boss1TauntDone = true;
            if (tauntCoroutine != null) StopCoroutine(tauntCoroutine);
            tauntCoroutine = StartCoroutine(TauntSequence(afterBoss1Line));
        }
    }

    // ── Sequence ──────────────────────────────────────────────────────────

    private IEnumerator TauntSequence(string line)
    {
        // Lock player input during the pan.
        LockPlayerInput(true);

        // Pan camera.
        Vector3 target = throneTransform != null ? throneTransform.position : throneFallbackPosition;
        CameraController cam = Camera.main?.GetComponent<CameraController>();
        cam?.PanToAndReturn(target, travelTime, holdTime);

        // Wait a beat for the pan to reach the throne.
        yield return new WaitForSeconds(travelTime * 0.8f);

        // Show dialogue.
        yield return StartCoroutine(ShowTauntText(line));

        // Wait for return pan.
        float remaining = travelTime + holdTime + travelTime - travelTime * 0.8f;
        yield return new WaitForSeconds(remaining);

        LockPlayerInput(false);
        tauntCoroutine = null;
    }

    // ── Text display ──────────────────────────────────────────────────────

    private IEnumerator ShowTauntText(string line)
    {
        EnsureFallbackText();
        if (fallbackText == null) yield break;

        fallbackText.text  = $"<b>{speakerName}</b>\n<i>\"{line}\"</i>";
        fallbackText.alpha = 0f;

        // Fade in
        float t = 0f;
        while (t < textFadeTime)
        {
            t += Time.unscaledDeltaTime;
            fallbackText.alpha = Mathf.Lerp(0f, 1f, t / textFadeTime);
            yield return null;
        }
        fallbackText.alpha = 1f;

        yield return new WaitForSecondsRealtime(holdTime * 0.9f);

        // Fade out
        t = 0f;
        while (t < textFadeTime)
        {
            t += Time.unscaledDeltaTime;
            fallbackText.alpha = Mathf.Lerp(1f, 0f, t / textFadeTime);
            yield return null;
        }
        fallbackText.alpha = 0f;
    }

    private void EnsureFallbackText()
    {
        if (fallbackText != null) return;

        Canvas canvas = FindHUDCanvas();
        if (canvas == null) return;

        GameObject go = new GameObject("SatanTauntText");
        go.transform.SetParent(canvas.transform, false);
        fallbackText = go.AddComponent<TextMeshProUGUI>();
        fallbackText.fontSize      = 22f;
        fallbackText.color         = new Color(0.95f, 0.75f, 0.25f);
        fallbackText.alignment     = TextAlignmentOptions.Center;
        fallbackText.fontStyle     = FontStyles.Normal;
        fallbackText.alpha         = 0f;

        RectTransform rt = fallbackText.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.1f, 0.12f);
        rt.anchorMax        = new Vector2(0.9f, 0.28f);
        rt.offsetMin        = Vector2.zero;
        rt.offsetMax        = Vector2.zero;

        // Dark backing panel
        GameObject panel = new GameObject("TauntPanel");
        panel.transform.SetParent(canvas.transform, false);
        panel.transform.SetSiblingIndex(go.transform.GetSiblingIndex()); // behind text
        Image img = panel.AddComponent<Image>();
        img.color = new Color(0.04f, 0.04f, 0.06f, 0.78f);
        RectTransform pRt = panel.GetComponent<RectTransform>();
        pRt.anchorMin = new Vector2(0.08f, 0.10f);
        pRt.anchorMax = new Vector2(0.92f, 0.30f);
        pRt.offsetMin = Vector2.zero;
        pRt.offsetMax = Vector2.zero;

        // Move text above panel in hierarchy
        go.transform.SetAsLastSibling();
    }

    private static Canvas FindHUDCanvas()
    {
        GameObject go = GameObject.Find("Canvas_HUD");
        if (go != null) return go.GetComponent<Canvas>();
        return Object.FindFirstObjectByType<Canvas>();
    }

    // ── Player input lock ─────────────────────────────────────────────────

    private static void LockPlayerInput(bool locked)
    {
        PlayerController pc = GameManager.Instance?.PlayerController
                              ?? Object.FindFirstObjectByType<PlayerController>();
        pc?.SetInputLocked(locked);
    }
}
