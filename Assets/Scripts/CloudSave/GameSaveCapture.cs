using System.Linq;
using UnityEngine;

/// <summary>
/// Builds a <see cref="GameSaveDocument"/> from the current play scene state.
/// </summary>
public static class GameSaveCapture
{
    public static GameSaveDocument CaptureCurrentOrThrow()
    {
        var gm = GameManager.Instance;
        if (gm == null)
            throw new System.InvalidOperationException("No GameManager — open MainScene first.");

        var doc = new GameSaveDocument
        {
            version = GameSaveSerializer.CurrentPayloadVersion,
            account = AccountProgression.Instance != null
                ? AccountProgression.Instance.ExportSaveBlock()
                : new AccountSaveBlock(),
            highScore = HighScoreManager.Instance != null
                ? HighScoreManager.Instance.ExportSaveBlock()
                : new HighScoreSaveBlock(),
            settings = SettingsManager.ExportSaveBlock()
        };

        var run = new RunSaveBlock();
        doc.run = run;

        if (gm.PlayerController != null)
        {
            var t = gm.PlayerController.transform.position;
            run.playerX = t.x;
            run.playerY = t.y;
            run.playerZ = t.z;
        }

        if (gm.PlayerHealth != null)
        {
            run.healthCurrent = gm.PlayerHealth.CurrentHP;
            run.healthMax     = gm.PlayerHealth.maxHP;
        }

        if (gm.PlayerXP != null)
        {
            run.runLevel       = gm.PlayerXP.RunLevel;
            run.runXP          = gm.PlayerXP.CurrentXP;
            run.runXPThreshold = gm.PlayerXP.XPThreshold;
            run.pickupRadius   = gm.PlayerXP.pickupRadius;
        }

        if (gm.PlayerWeapon != null)
        {
            run.weaponDamage = gm.PlayerWeapon.damage;
            run.weaponFireRate        = gm.PlayerWeapon.fireRate;
            run.weaponPierce          = gm.PlayerWeapon.pierceCount;
            run.weaponProjectileCount = gm.PlayerWeapon.projectileCount;
        }

        if (gm.PlayerController != null)
        {
            run.moveSpeed    = gm.PlayerController.moveSpeed;
            run.dashCooldown = gm.PlayerController.dashCooldown;
        }

        run.tutorialCompleted = PlayerPrefs.GetInt("Meta_TutorialCompleted", 0) == 1;

        run.activeWorld       = ResolveActiveWorld();
        run.waveIndex         = gm.WaveManager != null ? gm.WaveManager.WaveIndex : 0;
        run.wavesHasStarted   = gm.WaveManager != null && gm.WaveManager.HasStarted;
        run.waveIsBreak       = gm.WaveManager != null && gm.WaveManager.IsBreak;
        run.wavesClearedStat  = gm.WavesCleared;
        run.totalKillsStat    = gm.TotalKills;

        if (UpgradeManager.AppliedUpgradeIdsThisRun != null && UpgradeManager.AppliedUpgradeIdsThisRun.Count > 0)
            run.appliedUpgradeIds = UpgradeManager.AppliedUpgradeIdsThisRun.ToArray();
        else
            run.appliedUpgradeIds = System.Array.Empty<string>();

        return doc;
    }

    private static int ResolveActiveWorld()
    {
        if (IsActiveRoot("=== LEVEL (Lobby) ===")) return 0;
        if (IsActiveRoot("=== LEVEL (Tutorial) ===")) return 1;
        if (IsActiveRoot("=== LEVEL (ProBuilder) ===")) return 2;
        if (IsActiveRoot("=== LEVEL (ProBuilder) Arena2 ===")) return 3;
        return 0;
    }

    private static bool IsActiveRoot(string name)
    {
        foreach (var root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            if (root.name == name && root.activeInHierarchy) return true;
        return false;
    }
}
