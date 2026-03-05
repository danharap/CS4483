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
    [SerializeField] private float minPlayerDistance = 5f; // Don't spawn within this distance of player

    [Header("Enemy Prefabs")]
    [SerializeField] private GameObject chaserPrefab;
    [SerializeField] private GameObject fastPrefab;
    [SerializeField] private GameObject bossPrefab;

    [Header("Enemy HP Scaling per Wave")]
    [SerializeField] private float hpScalePerWave = 0.12f;   // +12% HP per wave

    [Header("Fast Enemy Unlock Wave")]
    [SerializeField] private int fastEnemyUnlockWave = 2;    // 0-based index

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

    // ── Helpers ───────────────────────────────────────────────────────────

    private GameObject ChoosePrefab(int waveIndex)
    {
        bool canSpawnFast = waveIndex >= fastEnemyUnlockWave && fastPrefab != null;
        if (canSpawnFast && Random.value < 0.35f)
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
}
