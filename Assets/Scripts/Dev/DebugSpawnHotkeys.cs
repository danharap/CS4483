using UnityEngine;

/// <summary>
/// Hotkeys to spawn enemies for testing (Editor Play mode):
/// 1 = Chaser, 2 = Fast, 3 = Heavy
/// F6 = Fast, F7 = Heavy, F8 = BigBat, F9 = Boss
/// Requires spawner reference wired (run CS4483 → SETUP EVERYTHING to wire it).
/// </summary>
public class DebugSpawnHotkeys : MonoBehaviour
{
    public EnemySpawner spawner;

#if UNITY_EDITOR
    private void Update()
    {
        if (spawner == null)
        {
            // Self-heal in case SetupAll wiring hasn't been run yet / got lost.
            spawner = Object.FindFirstObjectByType<EnemySpawner>();
            if (spawner == null) return;
        }
        if (Input.GetKeyDown(KeyCode.Alpha1)) spawner.SpawnChaserDebug();
        if (Input.GetKeyDown(KeyCode.Alpha2)) spawner.SpawnFastDebug();
        if (Input.GetKeyDown(KeyCode.Alpha3)) spawner.SpawnHeavyDebug();
        if (Input.GetKeyDown(KeyCode.F6)) spawner.SpawnFastDebug();
        if (Input.GetKeyDown(KeyCode.F7)) spawner.SpawnHeavyDebug();
        if (Input.GetKeyDown(KeyCode.F8))
        {
            Debug.Log("[DebugSpawnHotkeys] F8 pressed: spawn BigBat");
            spawner.SpawnBigBatDebug();
        }
        if (Input.GetKeyDown(KeyCode.F9)) spawner.SpawnBoss();
    }
#endif
}

