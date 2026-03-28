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
    [SerializeField] private RespawnUI respawnUI;
    [SerializeField] private PlaytestLogger logger;

    [Header("Level Roots (assigned by SetupAll)")]
    [SerializeField] private GameObject lobbyLevelRoot;
    [SerializeField] private GameObject arena1LevelRoot;
    [SerializeField] private GameObject arena2LevelRoot;

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
        Debug.Log($"[GameManager] PauseForUpgrade called! State: {State}");
        if (State != GameState.Playing) return;
        
        if (upgradeUI == null)
        {
            Debug.LogError("[GameManager] upgradeUI is NULL! Cannot show upgrade panel!");
            return;
        }
        
        State = GameState.PausedForUpgrade;
        Time.timeScale = 0f;
        Debug.Log("[GameManager] Game paused, calling upgradeUI.Show()...");
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

        float timeSurvived = Time.time - RunStartTime;

        HighScoreManager.Instance?.SubmitRun(WavesCleared, timeSurvived, TotalKills);
        logger?.LogSummary(timeSurvived, WavesCleared, TotalKills);

        // Pause time and show the respawn overlay
        Time.timeScale = 0f;
        if (respawnUI != null)
            respawnUI.Show();
        else
            // Fallback: if UI was never wired, just go directly to lobby
            RespawnToLobby();
    }

    private void HandleWaveCleared(int waveIndex) => WavesCleared = waveIndex;
    private void HandleEnemyKilled() => TotalKills++;

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    /// <summary>
    /// Respawn the player back in the Lobby level without reloading the whole scene.
    /// GameObject.Find cannot locate inactive objects, so we search scene roots directly.
    /// </summary>
    public void RespawnToLobby()
    {
        Time.timeScale = 1f;
        State = GameState.Playing;

        // Reset stats and kill counters for the new run
        TotalKills    = 0;
        WavesCleared  = 0;
        RunStartTime  = Time.time;

        // ── Reset portal managers first (stops any in-flight coroutines/portals) ─
        ArenaPortalManager.Instance?.ResetForRespawn();

        // ── Resolve level roots ───────────────────────────────────────────────
        if (lobbyLevelRoot == null || arena1LevelRoot == null || arena2LevelRoot == null)
        {
            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (lobbyLevelRoot  == null && root.name == "=== LEVEL (Lobby) ===")             lobbyLevelRoot  = root;
                if (arena1LevelRoot == null && root.name == "=== LEVEL (ProBuilder) ===")         arena1LevelRoot = root;
                if (arena2LevelRoot == null && root.name == "=== LEVEL (ProBuilder) Arena2 ===")  arena2LevelRoot = root;
            }
        }

        if (lobbyLevelRoot  != null) lobbyLevelRoot.SetActive(true);
        if (arena1LevelRoot != null) arena1LevelRoot.SetActive(false);
        if (arena2LevelRoot != null) arena2LevelRoot.SetActive(false);

        // Re-enable lobby colliders that EnterArenaFromLobby() disabled — critical for
        // LobbyPortal trigger to work and for lobby walls to block physics again.
        LobbyPortalManager.Instance?.ResetForRespawn();

        // ── Clean up all live enemies / projectiles ───────────────────────────
        foreach (EnemyBase enemy in FindObjectsOfType<EnemyBase>())
            Destroy(enemy.gameObject);
        foreach (Projectile proj in FindObjectsOfType<Projectile>())
            Destroy(proj.gameObject);
        EnemyRegistry.Clear();

        // ── Reset enemy spawner to Arena 1 spawn points ───────────────────────
        EnemySpawner spawner = FindFirstObjectByType<EnemySpawner>();
        spawner?.ResetToArena1Spawns();

        // ── Move player back to Lobby center ──────────────────────────────────
        if (PlayerController != null)
        {
            var cc = PlayerController.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            PlayerController.transform.position = new Vector3(0f, 1.1f, 0f);
            if (cc != null) cc.enabled = true;
            PlayerController.enabled = true;       // re-enable if it was locked during transition
        }

        // ── Reset all player stats (undoes every upgrade) ─────────────────────
        PlayerHealth?.ResetToBase();
        PlayerWeapon?.ResetToBase();
        PlayerController?.ResetToBase();
        PlayerXP?.ResetToBase();

        // ── Reset wave system ─────────────────────────────────────────────────
        if (waveManager != null)
            waveManager.ResetToFirstWave();

        // ── Refresh HUD stats ─────────────────────────────────────────────────
        hudManager?.UpdateWaveNumber(1);
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
