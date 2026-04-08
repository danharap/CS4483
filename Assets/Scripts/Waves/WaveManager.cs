using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Drives the wave loop: Wave -> Break -> Wave -> ... Boss every 5 waves.
/// Exposes events consumed by HUDManager, GateDoor, and GameManager.
/// </summary>
    public class WaveManager : MonoBehaviour
{
    // ── Tunables ──────────────────────────────────────────────────────────
    [Header("Wave Settings")]
    [SerializeField] private float waveDuration      = 75f;   // slightly longer window so more enemies spawn
    [SerializeField] private float breakDuration     = 3f;
    [SerializeField] private float baseSpawnInterval = 2f;    // seconds between spawns at wave 1
    [SerializeField] private float spawnIntervalMin  = 0.3f;  // absolute fastest (was 0.4)
    [SerializeField] private int   baseEnemyCountCap = 30;    // wave-1 cap; grows each wave
    [SerializeField] private int   enemyCapIncrement = 3;     // extra simultaneous enemies per wave
    [SerializeField] private int   bossEveryNWaves   = 5;

    // Computed each wave from the fields above.
    private int enemyCountCap = 30;

    [Header("References")]
    [SerializeField] private EnemySpawner spawner;

    // ── State ─────────────────────────────────────────────────────────────
    public int   WaveIndex     { get; private set; }  // 0-based (display as +1)
        public float WaveTimer     { get; private set; }
        public bool  IsBreak       { get; private set; }
        public bool  IsBossWave    { get; private set; }
        public bool  HasStarted    { get; private set; }
        private bool bossKilledThisWave;
        private int  activeEnemies;
        private bool wavesRunning;

#if UNITY_EDITOR
        /// <summary>When true, current wave/break is ended early (F3 debug).</summary>
        private bool debugSkipWaveRequested;
#endif

    // ── Events ────────────────────────────────────────────────────────────
    public event Action<int> OnWaveStart;    // waveIndex (0-based)
    public event Action<int> OnWaveCleared;  // waveIndex cleared
    public event Action      OnBreakStart;
    public event Action      OnBreakEnd;
    public event Action      OnBossSpawned;
    public event Action      OnBossKilled;
    public event Action      OnEnemyKilled;

    // ── Lifecycle ─────────────────────────────────────────────────────────

    void Start()
    {
        // Waves will be started explicitly (e.g. from Lobby portal) via BeginWaves().
    }

    void Update()
    {
        // Hard safety: waves should never keep running outside combat arenas.
        if (wavesRunning && !CanRunCombatWaves())
        {
            StopAllCoroutines();
            wavesRunning = false;
            HasStarted = false;
            IsBreak = false;
            IsBossWave = false;
            activeEnemies = 0;
            WaveTimer = 0f;
        }
    }

    public void BeginWaves()
    {
        if (!CanRunCombatWaves())
        {
            Debug.LogWarning("[WaveManager] BeginWaves ignored: not in an active arena combat root.");
            return;
        }

        if (wavesRunning) return;
        wavesRunning = true;
        HasStarted = true;
        StartCoroutine(WaveLoop());
    }

    /// <summary>
    /// Reset state so the next BeginWaves() starts at Wave 1 fresh.
    /// </summary>
    public void ResetToFirstWave()
    {
        StopAllCoroutines();
        WaveIndex = 0;
        WaveTimer = 0f;
        IsBreak = false;
        IsBossWave = false;
        HasStarted = false;
        wavesRunning = false;
        activeEnemies = 0;
    }

    // ── Main Loop ─────────────────────────────────────────────────────────

    private IEnumerator WaveLoop()
    {
        while (true)
        {
            bool isBoss = ((WaveIndex + 1) % bossEveryNWaves == 0);
            yield return StartCoroutine(isBoss ? BossWave() : NormalWave());

            // Break between waves
            IsBreak = true;
            OnBreakStart?.Invoke();
            GameManager.Instance?.HUD?.ShowTransition($"Wave {WaveIndex + 1} cleared! Prepare...");
            float breakLeft = breakDuration;
            while (breakLeft > 0f)
            {
#if UNITY_EDITOR
                if (debugSkipWaveRequested)
                {
                    debugSkipWaveRequested = false;
                    breakLeft = 0f;
                }
#endif
                breakLeft -= Time.deltaTime;
                WaveTimer = breakLeft;
                yield return null;
            }
            IsBreak = false;
            OnBreakEnd?.Invoke();

            WaveIndex++;
        }
    }

    // ── Normal Wave ───────────────────────────────────────────────────────

    private IEnumerator NormalWave()
    {
        IsBossWave = false;
        // Recalculate the cap for this wave: grows by enemyCapIncrement each wave.
        enemyCountCap = baseEnemyCountCap + WaveIndex * enemyCapIncrement;

        OnWaveStart?.Invoke(WaveIndex);
        GameManager.Instance?.HUD?.ShowTransition($"Wave {WaveIndex + 1}!");

        float elapsed = 0f;
        float spawnTimer = 0f;
        float spawnInterval = CalculateSpawnInterval();
        // From wave 6 onward (Arena 2) spawn 2 enemies per tick; from wave 9 spawn 3.
        int spawnsPerTick = WaveIndex >= 8 ? 3 : (WaveIndex >= 5 ? 2 : 1);

        while (elapsed < waveDuration)
        {
#if UNITY_EDITOR
            if (debugSkipWaveRequested)
            {
                debugSkipWaveRequested = false;
                elapsed = waveDuration;
            }
#endif
            if (GameManager.Instance?.State == GameManager.GameState.Playing)
            {
                elapsed    += Time.deltaTime;
                spawnTimer += Time.deltaTime;
                WaveTimer   = waveDuration - elapsed;

                if (spawnTimer >= spawnInterval && activeEnemies < enemyCountCap)
                {
                    spawnTimer = 0f;
                    for (int i = 0; i < spawnsPerTick; i++)
                    {
                        if (activeEnemies >= enemyCountCap) break;
                        spawner?.SpawnForWave(WaveIndex);
                    }
                }
            }
            yield return null;
        }

        OnWaveCleared?.Invoke(WaveIndex);
    }

    // ── Boss Wave ─────────────────────────────────────────────────────────

    private IEnumerator BossWave()
    {
        IsBossWave = true;
        bossKilledThisWave = false;
        OnWaveStart?.Invoke(WaveIndex);
        GameManager.Instance?.HUD?.ShowTransition($"BOSS WAVE {WaveIndex + 1}! Kill the boss!");
        OnBossSpawned?.Invoke();

        spawner?.SpawnBoss();

        // Wait until boss is killed (NotifyBossKilled sets flag)
        while (!bossKilledThisWave)
        {
#if UNITY_EDITOR
            if (debugSkipWaveRequested)
            {
                debugSkipWaveRequested = false;
                foreach (BossEnemy b in FindObjectsByType<BossEnemy>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                {
                    if (b != null) b.TakeDamage(1e9f);
                }
                if (!bossKilledThisWave)
                    bossKilledThisWave = true;
            }
#endif
            WaveTimer = 0f;
            yield return null;
        }

        OnBossKilled?.Invoke();
        OnWaveCleared?.Invoke(WaveIndex);

        // Boss reward: force-rare upgrade
        GameManager.Instance?.PauseForUpgrade(true);

        // Wait for player to pick upgrade before continuing
        while (GameManager.Instance?.State == GameManager.GameState.PausedForUpgrade)
            yield return null;
    }

    // ── Enemy Lifecycle Callbacks ─────────────────────────────────────────

    public void NotifyEnemySpawned() => activeEnemies++;

    public void NotifyEnemyDied(GameObject enemy)
    {
        activeEnemies = Mathf.Max(0, activeEnemies - 1);
        OnEnemyKilled?.Invoke();
    }

    public void NotifyBossKilled() => bossKilledThisWave = true;

    /// <summary>
    /// Used when transitioning to Arena 2 so the next wave is wave 6 (index 5).
    /// </summary>
    public void SetWaveIndex(int index)
    {
        WaveIndex = index;
    }

#if UNITY_EDITOR
    /// <summary>Editor play mode: skip rest of break, normal wave timer, or boss wait. Bound to F3 in <see cref="DebugSpawnHotkeys"/>.</summary>
    public void DebugRequestSkipWaveOrBreak()
    {
        debugSkipWaveRequested = true;
    }
#endif

    // ── Helpers ───────────────────────────────────────────────────────────

    private float CalculateSpawnInterval()
    {
        // Each wave the interval shrinks by 15% (clamped to minimum)
        return Mathf.Max(spawnIntervalMin, baseSpawnInterval * Mathf.Pow(0.85f, WaveIndex));
    }

    private bool CanRunCombatWaves()
    {
        GameObject lobby = GameObject.Find("=== LEVEL (Lobby) ===");
        if (lobby != null && lobby.activeInHierarchy) return false;

        GameObject tutorial = GameObject.Find("=== LEVEL (Tutorial) ===");
        if (tutorial != null && tutorial.activeInHierarchy) return false;

        GameObject arena1 = GameObject.Find("=== LEVEL (ProBuilder) ===");
        GameObject arena2 = GameObject.Find("=== LEVEL (ProBuilder) Arena2 ===");
        return (arena1 != null && arena1.activeInHierarchy) ||
               (arena2 != null && arena2.activeInHierarchy);
    }
}
