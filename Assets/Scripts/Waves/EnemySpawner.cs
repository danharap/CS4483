using UnityEngine;

/// <summary>
/// Picks a spawn point and instantiates the appropriate enemy prefab.
/// Spawn points should be placed around the arena perimeter (8–12 empties).
/// </summary>
public class EnemySpawner : MonoBehaviour
{
    // ── Tunables ──────────────────────────────────────────────────────────
    [Header("Spawn Points")]
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private Transform[] spawnPointsArena2;
    [SerializeField] private float minPlayerDistance = 5f; // Don't spawn within this distance of player

    [Header("Enemy Prefabs")]
    [SerializeField] private GameObject chaserPrefab;
    [SerializeField] private GameObject fastPrefab;
    [SerializeField] private GameObject bigBatPrefab;
    [SerializeField] private GameObject heavyPrefab;
    [SerializeField] private GameObject bossPrefab;
    [Header("Satan Boss (F10 debug spawn)")]
    [SerializeField] private GameObject satanPrefab;
    [Tooltip("Fixed spawn for Satan debug (top of arena).")]
    [SerializeField] private Vector3 satanDebugSpawnPosition = new Vector3(0f, 0f, 14f);

    [Header("Enemy HP Scaling per Wave")]
    [SerializeField] private float hpScalePerWave = 0.12f;   // +12% HP per wave

    [Header("Fast Enemy Unlock Wave")]
    [SerializeField] private int fastEnemyUnlockWave = 2;    // 0-based index

    [Header("Heavy Enemy Unlock Wave")]
    [SerializeField] private int heavyEnemyUnlockWave = 2;   // 0-based index (Wave 3)

    [Header("Big Bat Unlock Wave")]
    [SerializeField] private int bigBatUnlockWave = 5;       // 0-based index (Wave 6 = after wave 5)

    [Header("Spawn Jitter")]
    [Tooltip("Random XZ offset added to every spawn position to prevent enemies from stacking at the same point.")]
    [SerializeField] private float spawnJitter = 1.2f;

    [Header("Portal Exclusion Zone")]
    [Tooltip("World-space center of the no-spawn zone around the arrival portal in Arena 2.")]
    [SerializeField] private Vector3 portalExclusionCenter = new Vector3(0f, 0.5f, 14f);
    [Tooltip("Horizontal radius around the portal where no enemy may spawn.")]
    [SerializeField] private float portalExclusionRadius = 5f;
    [Tooltip("When true, enable the portal exclusion check during spawn selection.")]
    [SerializeField] private bool usePortalExclusion = true;

    [Header("Debug Gizmos")]
    [Tooltip("Draw spawn point spheres and portal exclusion zone in Scene view.")]
    [SerializeField] private bool showSpawnGizmos = true;

    // ── Internal ──────────────────────────────────────────────────────────

    private Transform[] arena1SpawnPoints; // preserved so ResetToArena1Spawns() works after UseArena2Spawns()

    void Awake()
    {
        arena1SpawnPoints = spawnPoints;
    }

    // ── Spawn ─────────────────────────────────────────────────────────────

    public void SpawnForWave(int waveIndex)
    {
        if (!CanSpawnInCurrentLevelState())
            return;

        GameObject prefab = ChoosePrefab(waveIndex);
        if (prefab == null) return;

        Transform sp = PickSpawnPoint();
        if (sp == null) return;

        Vector3 pos = ApplyJitter(sp.position);
        GameObject enemy = Instantiate(prefab, pos, Quaternion.identity);
        ScaleEnemyHP(enemy, waveIndex);
        GameManager.Instance?.WaveManager?.NotifyEnemySpawned();
    }

    public void SpawnBoss()
    {
        if (!CanSpawnInCurrentLevelState())
            return;

        // If Satan is already in the scene (placed on the throne by SatanArenaIntroController),
        // do NOT spawn a duplicate — the intro controller handles his entrance.
        if (FindFirstObjectByType<SatanBossController>() != null)
        {
            Debug.Log("[EnemySpawner] Satan already exists in scene (throne-watch mode) — skipping SpawnBoss().");
            return;
        }

        if (bossPrefab == null) return;
        Transform sp = PickSpawnPoint();
        if (sp == null) return;

        Instantiate(bossPrefab, ApplyJitter(sp.position), Quaternion.identity);
        GameManager.Instance?.WaveManager?.NotifyEnemySpawned();
    }

    /// <summary>
    /// Spawns the same first enemy used in Arena 1 (Wave 1 baseline) at a fixed position.
    /// Used by the tutorial hallway.
    /// </summary>
    public GameObject SpawnFirstArenaEnemyAt(Vector3 position)
    {
        if (chaserPrefab == null) return null;
        GameObject enemy = Instantiate(chaserPrefab, position, Quaternion.identity);
        return enemy;
    }

    /// <summary>
    /// Call when transitioning to Arena 2 so enemies spawn from Arena 2 spawn points.
    /// </summary>
    public void UseArena2Spawns()
    {
        if (spawnPointsArena2 != null && spawnPointsArena2.Length > 0)
            spawnPoints = spawnPointsArena2;
    }

    /// <summary>
    /// Restore Arena 1 spawn points after a respawn.
    /// </summary>
    public void ResetToArena1Spawns()
    {
        if (arena1SpawnPoints != null && arena1SpawnPoints.Length > 0)
            spawnPoints = arena1SpawnPoints;
    }

    private Vector3 ApplyJitter(Vector3 pos)
    {
        return new Vector3(
            pos.x + Random.Range(-spawnJitter, spawnJitter),
            pos.y,
            pos.z + Random.Range(-spawnJitter, spawnJitter));
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private GameObject ChoosePrefab(int waveIndex)
    {
        bool canSpawnFast  = waveIndex >= fastEnemyUnlockWave  && fastPrefab  != null;
        bool canSpawnHeavy = waveIndex >= heavyEnemyUnlockWave && heavyPrefab != null;
        bool canSpawnBigBat = waveIndex >= bigBatUnlockWave && bigBatPrefab != null;

        // Waves before unlock: only basic chasers
        if (!canSpawnFast && !canSpawnHeavy && !canSpawnBigBat)
            return chaserPrefab;

        float r = Random.value;

        // Mix:
        // After wave 5: add BigBat (fast+HP). Keep Heavy and Fast in the pool.
        if (canSpawnBigBat && r < 0.20f)
            return bigBatPrefab;
        if (canSpawnHeavy && r < 0.20f + 0.25f)
            return heavyPrefab;
        if (canSpawnFast && r < 0.20f + 0.25f + 0.30f)
            return fastPrefab;
        return chaserPrefab;
    }

    private Transform PickSpawnPoint()
    {
        if (spawnPoints == null || spawnPoints.Length == 0) return null;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        Vector3 playerPos = player != null ? player.transform.position : Vector3.zero;

        for (int attempt = 0; attempt < 15; attempt++)
        {
            Transform sp = spawnPoints[Random.Range(0, spawnPoints.Length)];

            // 1. Must be far enough from player
            if (Vector3.Distance(sp.position, playerPos) < minPlayerDistance)
                continue;

            // 2. Must not be inside the portal exclusion zone (XZ only)
            if (usePortalExclusion)
            {
                float flatDist = Vector2.Distance(
                    new Vector2(sp.position.x, sp.position.z),
                    new Vector2(portalExclusionCenter.x, portalExclusionCenter.z));
                if (flatDist < portalExclusionRadius)
                {
                    Debug.Log($"[EnemySpawner] '{sp.name}' rejected — inside portal exclusion zone " +
                              $"(dist={flatDist:F1} < radius={portalExclusionRadius}).");
                    continue;
                }
            }

            // 3. Must not be overlapping existing colliders
            if (!Physics.CheckSphere(sp.position, 0.8f, ~LayerMask.GetMask("Ignore Raycast")))
                return sp;
        }

        // Fallback: pick a random spawn point that at least clears the portal zone
        foreach (Transform sp in spawnPoints)
        {
            if (!usePortalExclusion) return sp;
            float flatDist = Vector2.Distance(
                new Vector2(sp.position.x, sp.position.z),
                new Vector2(portalExclusionCenter.x, portalExclusionCenter.z));
            if (flatDist >= portalExclusionRadius) return sp;
        }

        // Last resort: any point at all
        return spawnPoints[Random.Range(0, spawnPoints.Length)];
    }

    private void ScaleEnemyHP(GameObject enemy, int waveIndex)
    {
        EnemyBase eb = enemy.GetComponent<EnemyBase>();
        if (eb != null)
            eb.maxHP *= (1f + hpScalePerWave * waveIndex);
    }

    private bool CanSpawnInCurrentLevelState()
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

#if UNITY_EDITOR
    // ── Debug helpers for manual spawning while testing ────────────────────

    public void SpawnChaserDebug()
    {
        SpawnSpecific(chaserPrefab);
    }

    public void SpawnFastDebug()
    {
        SpawnSpecific(fastPrefab);
    }

    public void SpawnHeavyDebug()
    {
        SpawnSpecific(heavyPrefab);
    }

    public void SpawnBigBatDebug()
    {
        if (bigBatPrefab == null)
        {
            Debug.LogWarning("[EnemySpawner] BigBat prefab is not wired. Run CS4483 → 3 - Create Prefabs, then CS4483 → SETUP EVERYTHING.");
            return;
        }
        SpawnSpecific(bigBatPrefab);
    }

    private void SpawnSpecific(GameObject prefab)
    {
        if (!CanSpawnInCurrentLevelState())
            return;

        if (prefab == null)
        {
            Debug.LogWarning("[EnemySpawner] SpawnSpecific called with null prefab.");
            return;
        }
        Transform sp = PickSpawnPoint();
        if (sp == null) return;
        Instantiate(prefab, sp.position, Quaternion.identity);
    }

    /// <summary>
    /// Spawn Satan via the throne intro (F10).
    /// - If Satan is already on the throne (ThroneWatch state), immediately triggers the jump.
    /// - If Satan is not in the scene yet, spawns him at the throne position and queues the jump.
    /// - If Satan is already in combat, logs a warning and does nothing.
    /// </summary>
    public void SpawnSatanDebug()
    {
        // Case A: Satan already exists in scene
        SatanBossController existing = UnityEngine.Object.FindFirstObjectByType<SatanBossController>();
        if (existing != null)
        {
            if (existing.CurrentState == SatanBossController.BossState.ThroneWatch)
            {
                Debug.Log("[EnemySpawner] F10: Satan is on the throne — triggering awakening jump now.");
                existing.BeginAwakenFromThrone();
            }
            else
            {
                Debug.Log("[EnemySpawner] F10: Satan is already active — ignoring.");
            }
            return;
        }

        // Case B: Satan not yet in scene — spawn at throne position with immediate awakening jump
        if (satanPrefab == null)
        {
            Debug.LogWarning("[EnemySpawner] satanPrefab not assigned. Run CS4483 → SETUP EVERYTHING.");
            return;
        }

        // Read throne/landing positions from SatanArenaIntroController (may be on an inactive object)
        Vector3 spawnPos   = new Vector3(0f, 11f, 42f);   // fallback throne position
        Vector3 landingPos = new Vector3(0f, 1.1f, 12f);  // fallback landing position

        SatanArenaIntroController introCtrl =
            UnityEngine.Object.FindAnyObjectByType<SatanArenaIntroController>(FindObjectsInactive.Include);
        if (introCtrl != null)
        {
            spawnPos   = introCtrl.ThronePosition;
            landingPos = introCtrl.LandingPosition;
        }

        GameObject go = Instantiate(satanPrefab, spawnPos, Quaternion.identity);
        go.name = "Satan_Boss";

        SatanBossController boss = go.GetComponent<SatanBossController>();
        if (boss != null)
            boss.EnableThroneModeAndAwaken(spawnPos, landingPos);

        Debug.Log($"[EnemySpawner] F10: Satan spawned at throne {spawnPos}, jumping to arena {landingPos}.");
    }
#endif

    // ── Editor Gizmos ─────────────────────────────────────────────────────

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!showSpawnGizmos) return;

        // Arena 1 spawn points — green
        if (spawnPoints != null)
        {
            UnityEditor.Handles.color = new Color(0.2f, 1f, 0.2f, 0.8f);
            foreach (Transform sp in spawnPoints)
            {
                if (sp == null) continue;
                Gizmos.color = new Color(0.2f, 1f, 0.2f, 0.4f);
                Gizmos.DrawSphere(sp.position, 0.6f);
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(sp.position, 0.6f);
                UnityEditor.Handles.Label(sp.position + Vector3.up * 1.2f, sp.name);
            }
        }

        // Arena 2 spawn points — cyan
        if (spawnPointsArena2 != null)
        {
            foreach (Transform sp in spawnPointsArena2)
            {
                if (sp == null) continue;
                Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.4f);
                Gizmos.DrawSphere(sp.position, 0.6f);
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(sp.position, 0.6f);
                UnityEditor.Handles.Label(sp.position + Vector3.up * 1.2f, sp.name);
            }
        }

        // Portal exclusion zone — red/magenta disc
        if (usePortalExclusion)
        {
            Gizmos.color = new Color(1f, 0.1f, 0.8f, 0.15f);
            Gizmos.DrawSphere(portalExclusionCenter, portalExclusionRadius);
            Gizmos.color = new Color(1f, 0.1f, 0.8f, 0.9f);
            Gizmos.DrawWireSphere(portalExclusionCenter, portalExclusionRadius);
            UnityEditor.Handles.color = new Color(1f, 0.1f, 0.8f, 0.9f);
            UnityEditor.Handles.Label(portalExclusionCenter + Vector3.up * (portalExclusionRadius + 0.5f),
                $"Portal Exclusion r={portalExclusionRadius}");
        }
    }
#endif
}
