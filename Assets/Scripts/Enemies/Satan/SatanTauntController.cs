using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Delivers Satan's commentary throughout the run.
///
/// Triggers:
///   Wave 1  start  — dramatic camera pan to throne + opening taunt (input locked briefly)
///   Wave 3  start  — text-only taunt (no lock, player is fighting)
///   Wave 5  start  — text-only taunt before the mini-boss wave
///   After mini-boss killed — short reaction line
///   Wave 7  start  — text-only taunt, player is now in Arena 2
///   Wave 9  start  — final warning before Satan himself arrives
///
/// Wave 10 is the Satan fight itself; the SatanArenaIntroController handles that cinematic.
///
/// Attach to any persistent GameObject (e.g. GameManager child).
/// Optionally wire throneTransform in the Inspector; falls back to throneFallbackPosition.
/// </summary>
public class SatanTauntController : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────

    [Header("Camera Pan (Wave 1 only)")]
    [Tooltip("World position to pan to — e.g. Satan's throne. Falls back to throneFallbackPosition.")]
    [SerializeField] private Transform throneTransform;
    [SerializeField] private Vector3   throneFallbackPosition = new Vector3(0f, 0f, 60f);
    [SerializeField] private float     panTravelTime = 1.0f;
    [SerializeField] private float     panHoldTime   = 2.4f;

    [Header("Text Display")]
    [SerializeField] private string speakerName  = "Satan";
    [SerializeField] private float  fadeTime     = 0.30f;
    [Tooltip("How long the taunt text stays fully visible.")]
    [SerializeField] private float  displayTime  = 3.2f;

    // ── Taunt lines (editable in Inspector) ──────────────────────────────

    [Header("Wave 1 — Opening")]
    [TextArea(2, 4)]
    [SerializeField] private string wave1Line =
        "So. Another wretch crawls into my arena.\nI have watched a thousand like you die screaming. Begin.";

    [Header("Wave 3 — Mild Interest")]
    [TextArea(2, 4)]
    [SerializeField] private string wave3Line =
        "Still breathing? Curious.\nMost collapse long before the third wave. You have my attention — briefly.";

    [Header("Wave 5 — The Champion")]
    [TextArea(2, 4)]
    [SerializeField] private string wave5Line =
        "You have earned the right to face my Champion.\nHe has broken every warrior who stood where you stand.\nToday will be no different.";

    [Header("After Wave 5 Boss Killed")]
    [TextArea(2, 4)]
    [SerializeField] private string afterBossLine =
        "...Interesting.\nYou killed him.\nI had not expected that.\nDo not mistake my surprise for mercy.";

    [Header("Wave 7 — Deep Arena")]
    [TextArea(2, 4)]
    [SerializeField] private string wave7Line =
        "You are in MY arena now. No more warm-up.\nThe things I send you from here on — they are pieces of what I am.\nFeel honoured. Feel afraid.";

    [Header("Wave 9 — Final Warning")]
    [TextArea(2, 4)]
    [SerializeField] private string wave9Line =
        "One more wave stands between you and me.\nI want you tired. I want you bleeding.\nWhen you fall at my feet, I want it to mean something.";

    // ── State ─────────────────────────────────────────────────────────────

    private readonly HashSet<int> shownWaves = new HashSet<int>();
    private bool   afterBossDone;
    private Coroutine tauntCoroutine;

    // UI
    private GameObject panelGo;
    private TMP_Text   tauntText;
    private Image      panelImage;

    // ── Lifecycle ─────────────────────────────────────────────────────────

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

    // ── Wave hooks ────────────────────────────────────────────────────────

    private void OnWaveStart(int waveIndex)
    {
        if (shownWaves.Contains(waveIndex)) return;

        string line = null;
        bool doCameraPan = false;

        switch (waveIndex)
        {
            case 0: line = wave1Line;  doCameraPan = true;  break;
            case 2: line = wave3Line;  doCameraPan = false; break;
            case 4: line = wave5Line;  doCameraPan = false; break;
            case 6: line = wave7Line;  doCameraPan = false; break;
            case 8: line = wave9Line;  doCameraPan = false; break;
        }

        if (line == null) return;

        shownWaves.Add(waveIndex);
        InterruptAndPlay(line, doCameraPan);
    }

    private void OnBossKilled()
    {
        // Only react to the Arena-1 mini-boss, not to Satan himself.
        GameObject arena2 = GameObject.Find("=== LEVEL (ProBuilder) Arena2 ===");
        if (arena2 != null && arena2.activeInHierarchy) return;

        if (afterBossDone) return;
        afterBossDone = true;
        InterruptAndPlay(afterBossLine, doCameraPan: false);
    }

    // ── Coroutine dispatcher ──────────────────────────────────────────────

    private void InterruptAndPlay(string line, bool doCameraPan)
    {
        if (tauntCoroutine != null) StopCoroutine(tauntCoroutine);

        tauntCoroutine = doCameraPan
            ? StartCoroutine(PanAndTaunt(line))
            : StartCoroutine(TextOnlyTaunt(line));
    }

    // ── Wave-1 cinematic: camera pan + input lock ─────────────────────────

    private IEnumerator PanAndTaunt(string line)
    {
        LockPlayerInput(true);

        Vector3 target = throneTransform != null
            ? throneTransform.position
            : throneFallbackPosition;

        CameraController cam = Camera.main?.GetComponent<CameraController>();
        cam?.PanToAndReturn(target, panTravelTime, panHoldTime);

        // Wait until the camera is roughly at the throne before showing text.
        yield return new WaitForSeconds(panTravelTime * 0.75f);

        yield return StartCoroutine(ShowLine(line));

        // Wait for the camera to finish returning before unlocking.
        float remaining = panTravelTime + panHoldTime + panTravelTime
                          - panTravelTime * 0.75f
                          - (fadeTime * 2f + displayTime);
        if (remaining > 0f) yield return new WaitForSeconds(remaining);

        LockPlayerInput(false);
        tauntCoroutine = null;
    }

    // ── Text-only taunt: no camera pan, no input lock ─────────────────────

    private IEnumerator TextOnlyTaunt(string line)
    {
        // Brief delay so the text doesn't overlap the "Wave N!" transition banner.
        yield return new WaitForSeconds(0.6f);
        yield return StartCoroutine(ShowLine(line));
        tauntCoroutine = null;
    }

    // ── Text display ──────────────────────────────────────────────────────

    private IEnumerator ShowLine(string line)
    {
        EnsureUI();
        if (tauntText == null) yield break;

        tauntText.text = $"<b>{speakerName}</b>\n{line}";

        // Fade in panel + text together.
        float t = 0f;
        while (t < fadeTime)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Clamp01(t / fadeTime);
            SetUIAlpha(a);
            yield return null;
        }
        SetUIAlpha(1f);

        yield return new WaitForSecondsRealtime(displayTime);

        // Fade out.
        t = 0f;
        while (t < fadeTime)
        {
            t += Time.unscaledDeltaTime;
            float a = 1f - Mathf.Clamp01(t / fadeTime);
            SetUIAlpha(a);
            yield return null;
        }
        SetUIAlpha(0f);
    }

    private void SetUIAlpha(float a)
    {
        if (panelImage != null)
        {
            Color c = panelImage.color; c.a = 0.82f * a;
            panelImage.color = c;
        }
        if (tauntText != null) tauntText.alpha = a;
    }

    // ── UI builder ────────────────────────────────────────────────────────

    private void EnsureUI()
    {
        if (tauntText != null) return;

        Canvas canvas = FindHUDCanvas();
        if (canvas == null) return;

        // Dark panel — positioned just below the top-centre wave banner
        panelGo = new GameObject("SatanTauntPanel");
        panelGo.transform.SetParent(canvas.transform, false);

        panelImage = panelGo.AddComponent<Image>();
        panelImage.color = new Color(0.04f, 0.02f, 0.06f, 0f);
        panelImage.raycastTarget = false;

        RectTransform pRt = panelGo.GetComponent<RectTransform>();
        pRt.anchorMin        = new Vector2(0.12f, 0.68f);
        pRt.anchorMax        = new Vector2(0.88f, 0.85f);
        pRt.offsetMin        = Vector2.zero;
        pRt.offsetMax        = Vector2.zero;

        // Red accent line at bottom of panel
        GameObject accent = new GameObject("Accent");
        accent.transform.SetParent(panelGo.transform, false);
        Image accentImg = accent.AddComponent<Image>();
        accentImg.color = new Color(0.75f, 0.10f, 0.08f, 1f);
        RectTransform aRt = accent.GetComponent<RectTransform>();
        aRt.anchorMin = new Vector2(0.02f, 0f);
        aRt.anchorMax = new Vector2(0.98f, 0f);
        aRt.pivot     = new Vector2(0.5f, 0f);
        aRt.anchoredPosition = new Vector2(0f, 0f);
        aRt.sizeDelta = new Vector2(0f, 3f);

        // Text
        GameObject textGo = new GameObject("SatanTauntText");
        textGo.transform.SetParent(panelGo.transform, false);

        tauntText = textGo.AddComponent<TextMeshProUGUI>();
        tauntText.fontSize        = 20f;
        tauntText.color           = new Color(0.95f, 0.80f, 0.30f);
        tauntText.alignment       = TextAlignmentOptions.Center;
        tauntText.enableWordWrapping = true;
        tauntText.raycastTarget   = false;
        tauntText.alpha           = 0f;

        RectTransform tRt = tauntText.GetComponent<RectTransform>();
        tRt.anchorMin = Vector2.zero;
        tRt.anchorMax = Vector2.one;
        tRt.offsetMin = new Vector2(12f, 6f);
        tRt.offsetMax = new Vector2(-12f, -6f);

        // Start hidden.
        SetUIAlpha(0f);
    }

    private static Canvas FindHUDCanvas()
    {
        GameObject go = GameObject.Find("Canvas_HUD");
        if (go != null) return go.GetComponent<Canvas>();
        return Object.FindFirstObjectByType<Canvas>();
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static void LockPlayerInput(bool locked)
    {
        PlayerController pc = GameManager.Instance?.PlayerController
                              ?? Object.FindFirstObjectByType<PlayerController>();
        pc?.SetInputLocked(locked);
    }
}
