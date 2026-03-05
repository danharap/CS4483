using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Game over / run summary panel. Shows time, waves, kills, new-best indicators,
/// and Restart + Main Menu buttons.
/// </summary>
public class GameOverUI : MonoBehaviour
{
    // ── Inspector References ──────────────────────────────────────────────
    [Header("Panel")]
    [SerializeField] private GameObject gameOverPanel;

    [Header("Stats Text")]
    [SerializeField] private TMP_Text timeSurvivedText;
    [SerializeField] private TMP_Text wavesClearedText;
    [SerializeField] private TMP_Text totalKillsText;
    [SerializeField] private TMP_Text newBestText;       // "NEW BEST!" banner
    [SerializeField] private TMP_Text bestRunText;       // shows current all-time best

    [Header("Buttons")]
    [SerializeField] private Button restartButton;
    [SerializeField] private Button menuButton;

    void Awake()
    {
        if (gameOverPanel) gameOverPanel.SetActive(false);
        restartButton?.onClick.AddListener(() => GameManager.Instance?.RestartGame());
        menuButton?.onClick.AddListener(() => GameManager.Instance?.GoToMainMenu());
    }

    public void Show(float timeSurvived, int wavesCleared, int totalKills)
    {
        if (timeSurvivedText) timeSurvivedText.text = $"Time:   {FormatTime(timeSurvived)}";
        if (wavesClearedText) wavesClearedText.text = $"Waves:  {wavesCleared}";
        if (totalKillsText)   totalKillsText.text   = $"Kills:  {totalKills}";

        // High score feedback
        var hs = HighScoreManager.Instance;
        bool anyNew = hs != null && (hs.NewBestWaves || hs.NewBestTime || hs.NewBestKills);
        if (newBestText)  newBestText.gameObject.SetActive(anyNew);

        if (bestRunText && hs != null && hs.HasAnyRecord())
            bestRunText.text = $"BEST: Wave {hs.BestWaves}  {FormatTime(hs.BestTime)}  {hs.BestKills} kills";
        else if (bestRunText)
            bestRunText.text = "";

        if (gameOverPanel) gameOverPanel.SetActive(true);
    }

    private string FormatTime(float seconds)
    {
        int m = Mathf.FloorToInt(seconds / 60f);
        int s = Mathf.FloorToInt(seconds % 60f);
        return $"{m:00}:{s:00}";
    }
}
