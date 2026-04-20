using UnityEngine;

/// <summary>
/// Applies a <see cref="GameSaveDocument"/> after MainScene loads.
/// </summary>
public static class GameSaveHydrator
{
    public static void ApplyFromDocument(GameSaveDocument doc)
    {
        if (doc == null) return;

        AccountProgression.Instance?.OverwriteFromSave(doc.account);
        HighScoreManager.Instance?.ApplyFromSave(doc.highScore);
        SettingsManager.ApplyFromSave(doc.settings);

        var gm = GameManager.Instance;
        if (gm == null)
        {
            Debug.LogWarning("[GameSaveHydrator] GameManager missing — skipped run hydrate.");
            return;
        }

        var run = doc.run;
        if (run == null) return;

        if (IsInvalidPlayableRunSnapshot(run))
        {
            Debug.LogWarning("[GameSaveHydrator] Invalid/dead run snapshot detected — forcing clean lobby respawn.");
            gm.RespawnToLobby();
            return;
        }

        // Level visibility
        GameObject lobby = FindRoot("=== LEVEL (Lobby) ===");
        GameObject tutorial = FindRoot("=== LEVEL (Tutorial) ===");
        GameObject arena1   = FindRoot("=== LEVEL (ProBuilder) ===");
        GameObject arena2   = FindRoot("=== LEVEL (ProBuilder) Arena2 ===");

        if (lobby != null)    lobby.SetActive(false);
        if (tutorial != null) tutorial.SetActive(false);
        if (arena1 != null)   arena1.SetActive(false);
        if (arena2 != null)   arena2.SetActive(false);

        switch (run.activeWorld)
        {
            case 0: if (lobby != null) lobby.SetActive(true); break;
            case 1: if (tutorial != null) tutorial.SetActive(true); break;
            case 2: if (arena1 != null) arena1.SetActive(true); break;
            case 3: if (arena2 != null) arena2.SetActive(true); break;
            default: if (lobby != null) lobby.SetActive(true); break;
        }

        if (run.activeWorld == 0)
            LobbyPortalManager.Instance?.ResetForRespawn();

        // Player
        if (gm.PlayerController != null)
        {
            var cc = gm.PlayerController.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            gm.PlayerController.transform.position = new Vector3(run.playerX, run.playerY, run.playerZ);
            if (cc != null) cc.enabled = true;
        }

        gm.PlayerHealth?.RestoreFromSave(run.healthCurrent, run.healthMax);
        gm.PlayerXP?.RestoreFromSave(run.runXP, run.runXPThreshold, run.runLevel, run.pickupRadius);
        gm.PlayerWeapon?.RestoreFromSave(run.weaponDamage, run.weaponFireRate, run.weaponPierce, run.weaponProjectileCount);
        gm.PlayerController?.RestoreFromSave(run.moveSpeed, run.dashCooldown);

        bool inArena = run.activeWorld == 2 || run.activeWorld == 3;
        gm.PlayerWeapon?.SetShootingEnabled(inArena && run.wavesHasStarted);

        // Waves
        if (gm.WaveManager != null)
        {
            if (inArena && run.wavesHasStarted)
                gm.WaveManager.PrepareRestoreFromSave(run.waveIndex, true);
            else
                gm.WaveManager.ResetToFirstWave();
        }

        // Reflect stats on HUD if possible
        gm.HUD?.UpdateWaveNumber(Mathf.Max(0, run.waveIndex));

        bool needAutoTutorial = !run.tutorialCompleted && run.activeWorld != 1;
        MainMenuManager.SetShouldRunTutorialForLoadedSave(needAutoTutorial);
        PlayerPrefs.SetInt("Meta_TutorialCompleted", run.tutorialCompleted ? 1 : 0);
        PlayerPrefs.Save();

        UpgradeManager.ClearRunUpgradeHistory();

        gm.ApplyMetaPassives();
    }

    private static bool IsInvalidPlayableRunSnapshot(RunSaveBlock run)
    {
        if (run == null) return true;
        if (run.healthMax <= 0f) return true;
        if (run.healthCurrent <= 0f) return true;
        if (float.IsNaN(run.healthCurrent) || float.IsInfinity(run.healthCurrent)) return true;
        if (float.IsNaN(run.healthMax) || float.IsInfinity(run.healthMax)) return true;
        return false;
    }

    private static GameObject FindRoot(string name)
    {
        foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            if (root.name == name) return root;
        return null;
    }
}
