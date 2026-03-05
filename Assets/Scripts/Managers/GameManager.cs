using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Central game state machine. Singleton. Connects all major systems.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // ── Inspector References ──────────────────────────────────────────────
    [Header("Scene References")]
    [SerializeField] private WaveManager waveManager;
    [SerializeField] private HUDManager hudManager;
    [SerializeField] private UpgradeUI upgradeUI;
    [SerializeField] private GameOverUI gameOverUI;
    [SerializeField] private PlaytestLogger logger;

    [Header("Player Reference")]
    [SerializeField] private GameObject playerObject;

    // ── Cached Components ─────────────────────────────────────────────────
    public PlayerController PlayerController { get; private set; }
    public PlayerHealth PlayerHealth { get; private set; }
    public PlayerWeapon PlayerWeapon { get; private set; }
    public PlayerXP PlayerXP { get; private set; }

    // ── State ─────────────────────────────────────────────────────────────
    public enum GameState { Playing, PausedForUpgrade, GameOver }
    public GameState State { get; private set; } = GameState.Playing;

    // ── Stats tracked for game-over summary ──────────────────────────────
    public int TotalKills { get; private set; }
    public int WavesCleared { get; private set; }
    public float RunStartTime { get; private set; }

    void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); return; }

        if (playerObject != null)
        {
            PlayerController = playerObject.GetComponent<PlayerController>();
            PlayerHealth = playerObject.GetComponent<PlayerHealth>();
            PlayerWeapon = playerObject.GetComponent<PlayerWeapon>();
            PlayerXP = playerObject.GetComponent<PlayerXP>();
        }
    }

    void Start()
    {
        RunStartTime = Time.time;
        State = GameState.Playing;
        Time.timeScale = 1f;

        if (PlayerHealth != null)
            PlayerHealth.OnDeath += HandlePlayerDeath;

        if (waveManager != null)
        {
            waveManager.OnWaveCleared += HandleWaveCleared;
            waveManager.OnEnemyKilled += HandleEnemyKilled;
        }
    }

    // ── Upgrade Flow ──────────────────────────────────────────────────────

    public void PauseForUpgrade(bool forceBossRare = false)
    {
        if (State != GameState.Playing) return;
        State = GameState.PausedForUpgrade;
        Time.timeScale = 0f;
        upgradeUI.Show(forceBossRare);
    }

    public void ResumeAfterUpgrade()
    {
        if (State != GameState.PausedForUpgrade) return;
        State = GameState.Playing;
        Time.timeScale = 1f;
    }

    // ── Game Over Flow ────────────────────────────────────────────────────

    private void HandlePlayerDeath()
    {
        if (State == GameState.GameOver) return;
        State = GameState.GameOver;
        Time.timeScale = 0f;

        float timeSurvived = Time.time - RunStartTime;

        // Submit to high score before showing UI
        HighScoreManager.Instance?.SubmitRun(WavesCleared, timeSurvived, TotalKills);

        logger?.LogSummary(timeSurvived, WavesCleared, TotalKills);
        gameOverUI.Show(timeSurvived, WavesCleared, TotalKills);
    }

    private void HandleWaveCleared(int waveIndex) => WavesCleared = waveIndex;
    private void HandleEnemyKilled() => TotalKills++;

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    // ── Accessors ─────────────────────────────────────────────────────────
    public HUDManager HUD => hudManager;
    public WaveManager WaveManager => waveManager;
    public PlaytestLogger Logger => logger;
}
