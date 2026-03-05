using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages all in-run HUD elements: HP bar, XP bar, wave info, timer, transitions.
/// Wire references in inspector.
/// </summary>
public class HUDManager : MonoBehaviour
{
    public static HUDManager Instance { get; private set; }

    // ── Inspector References ──────────────────────────────────────────────
    [Header("HP")]
    [SerializeField] private Slider   hpSlider;
    [SerializeField] private TMP_Text hpText;

    [Header("XP")]
    [SerializeField] private Slider   xpSlider;
    [SerializeField] private TMP_Text levelText;

    [Header("Wave / Timer")]
    [SerializeField] private TMP_Text waveText;
    [SerializeField] private TMP_Text timerText;

    [Header("Transition Banner")]
    [SerializeField] private TMP_Text transitionText;
    [SerializeField] private float    transitionDuration = 2.5f;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (transitionText) transitionText.alpha = 0f;
    }

    void Start()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        if (gm.PlayerHealth != null)
        {
            gm.PlayerHealth.OnHealthChanged += UpdateHP;
            UpdateHP(gm.PlayerHealth.CurrentHP, gm.PlayerHealth.maxHP);
        }
        if (gm.PlayerXP != null)
        {
            gm.PlayerXP.OnXPChanged += UpdateXP;
            UpdateXP(0f, gm.PlayerXP.XPThreshold, 1);
        }
        if (gm.WaveManager != null)
        {
            gm.WaveManager.OnWaveStart += UpdateWaveNumber;
            UpdateWaveNumber(0);
        }
    }

    void Update()
    {
        var wm = GameManager.Instance?.WaveManager;
        if (wm == null) return;

        if (wm.IsBreak)
            timerText.text = "Break...";
        else if (wm.IsBossWave)
            timerText.text = "BOSS";
        else
            timerText.text = $"{Mathf.CeilToInt(wm.WaveTimer)}s";
    }

    // ── Update Methods ────────────────────────────────────────────────────

    public void UpdateHP(float current, float max)
    {
        if (hpSlider) hpSlider.value = max > 0f ? current / max : 0f;
        if (hpText)   hpText.text    = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
    }

    public void UpdateXP(float current, float max, int level)
    {
        if (xpSlider)  xpSlider.value = max > 0f ? current / max : 0f;
        if (levelText) levelText.text = $"Lv {level}";
    }

    public void UpdateWaveNumber(int waveIndex)
    {
        if (waveText) waveText.text = $"Wave {waveIndex + 1}";
    }

    public void ShowTransition(string message)
    {
        StopAllCoroutines();
        StartCoroutine(TransitionRoutine(message));
    }

    private IEnumerator TransitionRoutine(string msg)
    {
        if (transitionText == null) yield break;

        transitionText.text = msg;
        transitionText.alpha = 1f;
        yield return new WaitForSeconds(transitionDuration * 0.7f);

        float t = 0f;
        while (t < transitionDuration * 0.3f)
        {
            t += Time.unscaledDeltaTime;
            transitionText.alpha = Mathf.Lerp(1f, 0f, t / (transitionDuration * 0.3f));
            yield return null;
        }
        transitionText.alpha = 0f;
    }
}
