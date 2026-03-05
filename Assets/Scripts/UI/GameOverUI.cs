using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Game over / run summary panel. Shows time, waves, kills and a Restart button.
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

    [Header("Button")]
    [SerializeField] private Button restartButton;

    void Awake()
    {
        if (gameOverPanel) gameOverPanel.SetActive(false);
        restartButton?.onClick.AddListener(() => GameManager.Instance?.RestartGame());
    }

    public void Show(float timeSurvived, int wavesCleared, int totalKills)
    {
        if (timeSurvivedText) timeSurvivedText.text = $"Time Survived: {FormatTime(timeSurvived)}";
        if (wavesClearedText) wavesClearedText.text = $"Waves Cleared: {wavesCleared}";
        if (totalKillsText)   totalKillsText.text   = $"Total Kills:   {totalKills}";
        if (gameOverPanel)    gameOverPanel.SetActive(true);
    }

    private string FormatTime(float seconds)
    {
        int m = Mathf.FloorToInt(seconds / 60f);
        int s = Mathf.FloorToInt(seconds % 60f);
        return $"{m:00}:{s:00}";
    }
}
