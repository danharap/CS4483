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

    // ── Spawn ─────────────────────────────────────────────────────────────

    public void SpawnForWave(int waveIndex)
    {
        GameObject prefab = ChoosePrefab(waveIndex);
        if (prefab == null) return;

        Transform sp = PickSpawnPoint();
        if (sp == null) return;

        GameObject enemy = Instantiate(prefab, sp.position, Quaternion.identity);
        ScaleEnemyHP(enemy, waveIndex);
        GameManager.Instance?.WaveManager?.NotifyEnemySpawned();
    }

    public void SpawnBoss()
    {
        if (bossPrefab == null) return;
        Transform sp = PickSpawnPoint();
        if (sp == null) return;

        Instantiate(bossPrefab, sp.position, Quaternion.identity);
        GameManager.Instance?.WaveManager?.NotifyEnemySpawned();
    }

    /// <summary>
    /// Call when transitioning to Arena 2 so enemies spawn from Arena 2 spawn points.
    /// </summary>
    public void UseArena2Spawns()
    {
        if (spawnPointsArena2 != null && spawnPointsArena2.Length > 0)
            spawnPoints = spawnPointsArena2;
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

        // Try to find a spawn point that is:
        // 1. Not overlapping with colliders
        // 2. Far enough from the player (minPlayerDistance)
        for (int attempt = 0; attempt < 10; attempt++)
        {
            Transform sp = spawnPoints[Random.Range(0, spawnPoints.Length)];
            
            // Check if spawn point is far enough from player
            float distToPlayer = Vector3.Distance(sp.position, playerPos);
            if (distToPlayer < minPlayerDistance)
                continue;
            
            // Check if spawn point is clear of colliders
            if (!Physics.CheckSphere(sp.position, 0.8f, ~LayerMask.GetMask("Ignore Raycast")))
                return sp;
        }
        
        // Fallback: just pick a random spawn point (better than not spawning)
        return spawnPoints[Random.Range(0, spawnPoints.Length)];
    }

    private void ScaleEnemyHP(GameObject enemy, int waveIndex)
    {
        EnemyBase eb = enemy.GetComponent<EnemyBase>();
        if (eb != null)
            eb.maxHP *= (1f + hpScalePerWave * waveIndex);
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
        if (prefab == null)
        {
            Debug.LogWarning("[EnemySpawner] SpawnSpecific called with null prefab.");
            return;
        }
        Transform sp = PickSpawnPoint();
        if (sp == null) return;
        Instantiate(prefab, sp.position, Quaternion.identity);
    }

    /// <summary>Spawn Satan at a fixed arena position (F10). Does not use perimeter spawn points.</summary>
    public void SpawnSatanDebug()
    {
        if (satanPrefab == null)
        {
            Debug.LogWarning("[EnemySpawner] satanPrefab is not assigned. Run CS4483 → 3 - Create Prefabs (creates Enemy_Satan), then CS4483 → SETUP EVERYTHING.");
            return;
        }
        if (UnityEngine.Object.FindFirstObjectByType<SatanBossController>() != null)
        {
            Debug.Log("[EnemySpawner] Satan already active — ignoring F10.");
            return;
        }

        GameObject go = Instantiate(satanPrefab, satanDebugSpawnPosition, Quaternion.identity);
        go.name = "Satan_Boss";
        Debug.Log($"[EnemySpawner] Satan spawned at {satanDebugSpawnPosition}.");
    }
#endif
}
