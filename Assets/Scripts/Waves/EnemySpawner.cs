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

        // Shuffle-pick: try up to 5 random points and use first one that isn't
        // overlapping with another collider (avoids spawning inside walls).
        for (int attempt = 0; attempt < 5; attempt++)
        {
            Transform sp = spawnPoints[Random.Range(0, spawnPoints.Length)];
            if (!Physics.CheckSphere(sp.position, 0.8f, ~LayerMask.GetMask("Ignore Raycast")))
                return sp;
        }
        return spawnPoints[Random.Range(0, spawnPoints.Length)];
    }

    private void ScaleEnemyHP(GameObject enemy, int waveIndex)
    {
        EnemyBase eb = enemy.GetComponent<EnemyBase>();
        if (eb != null)
            eb.maxHP *= (1f + hpScalePerWave * waveIndex);
    }
}
