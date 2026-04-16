using System.Collections;
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
    [SerializeField] private RespawnUI  respawnUI;
    [SerializeField] private VictoryUI  victoryUI;
    [SerializeField] private PlaytestLogger logger;

    [Header("Level Roots (assigned by SetupAll)")]
    [SerializeField] private GameObject lobbyLevelRoot;
    [SerializeField] private GameObject tutorialLevelRoot;
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
    private bool accountLevelUpBound = false;

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

        // Default: no shooting in lobby/menu until entering tutorial or arena.
        PlayerWeapon?.SetShootingEnabled(false);

        if (PlayerHealth != null)
            PlayerHealth.OnDeath += HandlePlayerDeath;

        if (waveManager != null)
        {
            waveManager.OnWaveCleared += HandleWaveCleared;
            waveManager.OnWaveCleared += HandleWaveClearedMetaXP;
            waveManager.OnEnemyKilled += HandleEnemyKilled;
        }

        if (AccountProgression.Instance != null)
        {
            AccountProgression.Instance.OnAccountLevelUp += HandleAccountLevelUp;
            accountLevelUpBound = true;
        }

        if (LocalSaveRuntime.TryConsumePendingHydrate(out GameSaveDocument cloudDoc))
            GameSaveHydrator.ApplyFromDocument(cloudDoc);

        if (MainMenuManager.ShouldRunTutorial)
            StartCoroutine(AutoStartTutorial());
    }

    private IEnumerator AutoStartTutorial()
    {
        yield return null; // wait one frame so all singletons are ready
        // Tutorial is accessed via the "New Game" flow (not via portal/NPC interaction).
        TutorialRoomManager.Instance?.EnterTutorialFromLobby();
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

    private void HandleAccountLevelUp(int newLevel)
    {
        hudManager?.ShowTransition($"ACCOUNT LEVEL UP!  Level {newLevel}  —  +1 Skill Point");
    }

    private void HandleWaveClearedMetaXP(int waveIndex)
    {
        // Base: 50 XP per wave, scaling slightly with wave number.
        // Boss waves grant a flat +200 bonus.
        // With xpPerLevel=1500, a full 5-wave arena run earns ~450-650 XP — takes
        // several runs to level up, making each skill point feel earned.
        // Late-bind the level-up listener in case AccountProgression wasn't ready at Start()
        if (AccountProgression.Instance != null && !accountLevelUpBound)
        {
            AccountProgression.Instance.OnAccountLevelUp += HandleAccountLevelUp;
            accountLevelUpBound = true;
        }

        int baseXP  = 50 + 10 * waveIndex;
        bool isBoss = waveManager != null && waveManager.IsBossWave;
        int totalXP = isBoss ? baseXP + 200 : baseXP;
        AccountProgression.Instance?.AddMetaXP(totalXP);
        Debug.Log($"[GameManager] Meta XP awarded: {totalXP} (wave {waveIndex + 1}, boss={isBoss})");
    }

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
        LocalSaveAutoSync.TrySyncNow();

        Time.timeScale = 1f;
        State = GameState.Playing;

        // Reset stats and kill counters for the new run
        TotalKills    = 0;
        WavesCleared  = 0;
        RunStartTime  = Time.time;

        // ── Reset portal managers first (stops any in-flight coroutines/portals) ─
        ArenaPortalManager.Instance?.ResetForRespawn();

        // ── Resolve level roots ───────────────────────────────────────────────
        if (lobbyLevelRoot == null || tutorialLevelRoot == null || arena1LevelRoot == null || arena2LevelRoot == null)
        {
            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (lobbyLevelRoot  == null && root.name == "=== LEVEL (Lobby) ===")             lobbyLevelRoot  = root;
                if (tutorialLevelRoot == null && root.name == "=== LEVEL (Tutorial) ===")         tutorialLevelRoot = root;
                if (arena1LevelRoot == null && root.name == "=== LEVEL (ProBuilder) ===")         arena1LevelRoot = root;
                if (arena2LevelRoot == null && root.name == "=== LEVEL (ProBuilder) Arena2 ===")  arena2LevelRoot = root;
            }
        }

        if (lobbyLevelRoot  != null) lobbyLevelRoot.SetActive(true);
        if (tutorialLevelRoot != null) tutorialLevelRoot.SetActive(false);
        if (arena1LevelRoot != null) arena1LevelRoot.SetActive(false);
        if (arena2LevelRoot != null) arena2LevelRoot.SetActive(false);

        // Re-enable lobby colliders that EnterArenaFromLobby() disabled — critical for
        // LobbyPortal trigger to work and for lobby walls to block physics again.
        LobbyPortalManager.Instance?.ResetForRespawn();

        ClearTransientArenaEntities();

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

        UpgradeManager.ClearRunUpgradeHistory();

        // Back in lobby: lock shooting until player enters arena again.
        PlayerWeapon?.SetShootingEnabled(false);

        // ── Reset wave system ─────────────────────────────────────────────────
        if (waveManager != null)
            waveManager.ResetToFirstWave();

        // ── Refresh HUD stats ─────────────────────────────────────────────────
        hudManager?.UpdateWaveNumber(1);
    }

    /// <summary>
    /// Show the victory screen after the final boss is defeated.
    /// Clears feet, pickups, and stray VFX first so nothing flashes after returning to the lobby.
    /// Freezes time for drama; the Return button calls RespawnToLobby().
    /// </summary>
    public void ShowVictoryScreen()
    {
        ClearTransientArenaEntities();

        if (victoryUI == null)
        {
            Debug.LogWarning("[GameManager] victoryUI not assigned – falling back to instant respawn.");
            RespawnToLobby();
            return;
        }

        State = GameState.GameOver; // prevent other UI from triggering simultaneously
        Time.timeScale = 0f;

        float timeSurvived = Time.time - RunStartTime;
        victoryUI.Show(WavesCleared, TotalKills, timeSurvived);
    }

    /// <summary>
    /// Destroys arena-only entities that are not covered by the normal enemy/projectile sweep:
    /// Satan feet (separate GameObjects), orphaned foot telegraphs, pickups, boss bullets, damage numbers.
    /// Tutorial subtree is skipped for pickups so tutorial orbs/medkits are not stripped on arena respawn.
    /// </summary>
    public void ClearTransientArenaEntities()
    {
        const string tutorialRoot = "=== LEVEL (Tutorial) ===";

        static bool UnderTutorial(Transform t)
        {
            while (t != null)
            {
                if (t.name == tutorialRoot) return true;
                t = t.parent;
            }
            return false;
        }

        foreach (SatanFootController foot in FindObjectsByType<SatanFootController>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (foot == null) continue;
            foreach (Renderer r in foot.GetComponentsInChildren<Renderer>(true))
                r.enabled = false;
            Destroy(foot.gameObject);
        }

        foreach (LineRenderer lr in FindObjectsByType<LineRenderer>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (lr != null && lr.gameObject.name == "FootWarning")
            {
                lr.enabled = false;
                Destroy(lr.gameObject);
            }
        }

        foreach (HealthPack hp in FindObjectsByType<HealthPack>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (hp == null || UnderTutorial(hp.transform)) continue;
            foreach (Renderer r in hp.GetComponentsInChildren<Renderer>(true))
                r.enabled = false;
            Destroy(hp.gameObject);
        }

        foreach (XPOrb orb in FindObjectsByType<XPOrb>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (orb == null || UnderTutorial(orb.transform)) continue;
            foreach (Renderer r in orb.GetComponentsInChildren<Renderer>(true))
                r.enabled = false;
            Destroy(orb.gameObject);
        }

        foreach (DamageNumber dn in FindObjectsByType<DamageNumber>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (dn == null) continue;
            foreach (Renderer r in dn.GetComponentsInChildren<Renderer>(true))
                r.enabled = false;
            Destroy(dn.gameObject);
        }

        foreach (SatanBullet sb in FindObjectsByType<SatanBullet>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (sb == null) continue;
            foreach (Renderer r in sb.GetComponentsInChildren<Renderer>(true))
                r.enabled = false;
            Destroy(sb.gameObject);
        }

        foreach (EnemyBase enemy in FindObjectsByType<EnemyBase>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (enemy != null) Destroy(enemy.gameObject);
        }

        foreach (Projectile proj in FindObjectsByType<Projectile>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (proj != null) Destroy(proj.gameObject);
        }

        EnemyRegistry.Clear();
    }

    public void GoToMainMenu()
    {
        LocalSaveAutoSync.TrySyncNow();
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    /// <summary>
    /// Applies all unlocked meta passives to the player at the start of a run.
    /// Called by <see cref="LobbyPortalManager"/> right before the player enters the arena.
    /// </summary>
    public void ApplyMetaPassives()
    {
        MetaPassiveApplicator.ApplyAll(PlayerHealth, PlayerWeapon, PlayerController);
    }

    // ── Accessors ─────────────────────────────────────────────────────────
    public HUDManager HUD => hudManager;
    public WaveManager WaveManager => waveManager;
    public PlaytestLogger Logger => logger;
}
