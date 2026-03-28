using UnityEngine;

/// <summary>
/// Debug-only utility to spawn Satan during editor play-mode testing.
/// Called from DebugSpawnHotkeys via F10.
///
/// Wire the satanPrefab in the Inspector of any scene GameObject,
/// or let the static TrySpawnSatan() find it at runtime.
/// </summary>
public class SatanDebugSpawner : MonoBehaviour
{
    [Header("Satan Prefab")]
    [SerializeField] private GameObject satanPrefab;

    [Header("Spawn Position")]
    [Tooltip("Where to place Satan. Top of the arena is around (0, 0, 14).")]
    [SerializeField] private Vector3 spawnPosition = new Vector3(0f, 0f, 14f);

#if UNITY_EDITOR
    /// <summary>
    /// Fallback when <see cref="EnemySpawner.SpawnSatanDebug"/> is unavailable.
    /// Finds any SatanDebugSpawner in the scene (does not rely on Awake order).
    /// </summary>
    public static void TrySpawnSatan()
    {
        SatanBossController existing = Object.FindFirstObjectByType<SatanBossController>();
        if (existing != null)
        {
            Debug.Log("[SatanDebugSpawner] Satan already active — ignoring F10.");
            return;
        }

        SatanDebugSpawner spawner = Object.FindFirstObjectByType<SatanDebugSpawner>();
        if (spawner == null || spawner.satanPrefab == null)
        {
            Debug.LogWarning("[SatanDebugSpawner] No fallback: add EnemySpawner with satanPrefab (run CS4483 → 3 - Create Prefabs, then SETUP EVERYTHING), " +
                             "or add SatanDebugSpawner + assign satanPrefab.");
            return;
        }

        GameObject satan = Instantiate(spawner.satanPrefab, spawner.spawnPosition, Quaternion.identity);
        satan.name = "Satan_Boss";
        Debug.Log($"[SatanDebugSpawner] Satan spawned at {spawner.spawnPosition}.");
    }
#endif
}
