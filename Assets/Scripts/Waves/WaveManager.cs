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
    [SerializeField] private float waveDuration    = 60f;
    [SerializeField] private float breakDuration   = 3f;
    [SerializeField] private float baseSpawnInterval = 2f;   // seconds between spawns
    [SerializeField] private float spawnIntervalMin   = 0.4f; // fastest possible
    [SerializeField] private int   enemyCountCap   = 30;     // max simultaneous enemies
    [SerializeField] private int   bossEveryNWaves = 5;

    [Header("References")]
    [SerializeField] private EnemySpawner spawner;

    // ── State ─────────────────────────────────────────────────────────────
    public int   WaveIndex     { get; private set; }  // 0-based (display as +1)
    public float WaveTimer     { get; private set; }
    public bool  IsBreak       { get; private set; }
    public bool  IsBossWave    { get; private set; }
    private bool bossKilledThisWave;
    private int  activeEnemies;

    // ── Events ────────────────────────────────────────────────────────────
    public event Action<int> OnWaveStart;    // waveIndex (0-based)
    public event Action<int> OnWaveCleared;  // waveIndex cleared
    public event Action      OnBreakStart;
    public event Action      OnBreakEnd;
    public event Action      OnBossSpawned;
    public event Action      OnBossKilled;
    public event Action      OnEnemyKilled;

    // ── Lifecycle ─────────────────────────────────────────────────────────

    void Start() => StartCoroutine(WaveLoop());

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
        OnWaveStart?.Invoke(WaveIndex);
        GameManager.Instance?.HUD?.ShowTransition($"Wave {WaveIndex + 1}!");

        float elapsed = 0f;
        float spawnTimer = 0f;
        float spawnInterval = CalculateSpawnInterval();

        while (elapsed < waveDuration)
        {
            if (GameManager.Instance?.State == GameManager.GameState.Playing)
            {
                elapsed   += Time.deltaTime;
                spawnTimer += Time.deltaTime;
                WaveTimer  = waveDuration - elapsed;

                if (spawnTimer >= spawnInterval && activeEnemies < enemyCountCap)
                {
                    spawnTimer = 0f;
                    spawner?.SpawnForWave(WaveIndex);
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

    // ── Helpers ───────────────────────────────────────────────────────────

    private float CalculateSpawnInterval()
    {
        // Each wave the interval shrinks by 10% (clamped to minimum)
        return Mathf.Max(spawnIntervalMin, baseSpawnInterval * Mathf.Pow(0.9f, WaveIndex));
    }
}
