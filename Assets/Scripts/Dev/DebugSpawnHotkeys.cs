using UnityEngine;

/// <summary>
/// Hotkeys to spawn enemies for testing (Editor Play mode):
///   1 = Chaser, 2 = Fast, 3 = Heavy
///   F2 = force run level-up (XP card)
///   F3 = skip current wave/break
///   F4 = grant 1 account level + 1 skill point (Shift+F4 = 3 levels)
///   F5 = RESET all account progression (wipes skills, XP, levels)
///   F6 = spawn Fast enemy, F7 = spawn Heavy, F8 = BigBat, F9 = Boss, F10 = Satan
///   F11 = skip tutorial / jump straight to lobby (dev shortcut)
/// Requires spawner reference wired (run CS4483 → SETUP EVERYTHING to wire it).
/// </summary>
public class DebugSpawnHotkeys : MonoBehaviour
{
    public EnemySpawner spawner;

#if UNITY_EDITOR
    private void Update()
    {
        // F10 must work even if EnemySpawner is missing (TrySpawnSatan has fallbacks).
        if (Input.GetKeyDown(KeyCode.F10))
        {
            TrySpawnSatanDebug();
        }

        // F11 = skip tutorial and land directly in the lobby for rapid iteration.
        if (Input.GetKeyDown(KeyCode.F11))
        {
            SkipTutorial();
        }

        if (Input.GetKeyDown(KeyCode.F2))
        {
            PlayerXP xp = Object.FindFirstObjectByType<PlayerXP>();
            if (xp != null) xp.DebugForceLevelUp();
        }

        // F4 = grant 1 account level + 1 skill point instantly (test skill tree)
        // Hold Shift+F4 to grant 3 levels at once
        if (Input.GetKeyDown(KeyCode.F4))
        {
            EnsureAccountProgression();
            int count = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ? 3 : 1;
            AccountProgression.Instance.DebugGrantLevels(count);
        }

        // F5 = reset ALL account progression (wipe skills, XP, and levels back to 1)
        if (Input.GetKeyDown(KeyCode.F5))
        {
            EnsureAccountProgression();
            AccountProgression.Instance.DebugResetAll();
        }

        if (Input.GetKeyDown(KeyCode.F3))
        {
            WaveManager wm = GameManager.Instance != null ? GameManager.Instance.WaveManager : null;
            if (wm == null) wm = Object.FindFirstObjectByType<WaveManager>();
            wm?.DebugRequestSkipWaveOrBreak();
        }

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

    /// <summary>
    /// Ensures AccountProgression singleton exists at runtime even if Setup wasn't re-run.
    /// Creates it on the fly so F4 / F5 always work without having to re-run SETUP EVERYTHING.
    /// </summary>
    private static void EnsureAccountProgression()
    {
        if (AccountProgression.Instance != null) return;
        GameObject go = new GameObject("AccountProgression_Runtime");
        go.AddComponent<AccountProgression>();
        Debug.Log("[Debug] AccountProgression was missing — created it on the fly. " +
                  "Re-run CS4483 → SETUP EVERYTHING for permanent wiring.");
    }

    private static void TrySpawnSatanDebug()
    {
        EnemySpawner es = Object.FindFirstObjectByType<EnemySpawner>();
        if (es != null)
        {
            es.SpawnSatanDebug();
            return;
        }

        SatanDebugSpawner.TrySpawnSatan();
    }

    /// <summary>
    /// F11 — immediately ends the tutorial and places the player in the lobby so devs
    /// can jump straight to testing arena gameplay without sitting through the tutorial.
    /// Works whether the tutorial is currently active or not.
    /// </summary>
    private static void SkipTutorial()
    {
        // If the tutorial is currently running, end it cleanly.
        TutorialManager.Instance?.EndTutorialRoom();

        // If TutorialRoomManager is present, use its return path to get back to lobby.
        TutorialRoomManager trm = Object.FindFirstObjectByType<TutorialRoomManager>();
        if (trm != null)
        {
            trm.ExitTutorialToLobby();
            Debug.Log("[DEV F11] Tutorial skipped — returned to lobby via TutorialRoomManager.");
            return;
        }

        // Fallback: make sure the lobby is visible and the player is positioned there.
        GameObject lobbyRoot = null;
        foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (root.name == "=== LEVEL (Lobby) ===") { lobbyRoot = root; break; }
        }
        if (lobbyRoot != null) lobbyRoot.SetActive(true);

        // Hide tutorial level if it's still up
        foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (root.name == "=== LEVEL (Tutorial) ===") { root.SetActive(false); break; }
        }

        // Teleport player to lobby centre
        var cc = Object.FindFirstObjectByType<CharacterController>();
        if (cc != null)
        {
            cc.enabled = false;
            cc.transform.position = new Vector3(0f, 1.1f, 0f);
            cc.enabled = true;
        }

        // Unlock input in case the tutorial had locked it
        GameManager.Instance?.PlayerController?.SetInputLocked(false);

        Debug.Log("[DEV F11] Tutorial skipped — player teleported to lobby.");
    }
#endif
}

