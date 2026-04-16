using System;
using Newtonsoft.Json;

/// <summary>
/// Versioned snapshot stored in local JSON saves and used for hydrate/capture.
/// Bump <see cref="GameSaveDocument.version"/> when adding fields; migrate in <see cref="GameSaveSerializer"/>.
/// </summary>
[Serializable]
public class GameSaveDocument
{
    public int version = 1;

    public AccountSaveBlock account = new AccountSaveBlock();
    public RunSaveBlock run = new RunSaveBlock();
    public HighScoreSaveBlock highScore = new HighScoreSaveBlock();
    public SettingsSaveBlock settings = new SettingsSaveBlock();
}

[Serializable]
public class AccountSaveBlock
{
    public int accountLevel = 1;
    public int metaXP;
    public int totalMetaXP;
    public int skillPoints;
    public string[] unlockedSkills = Array.Empty<string>();
}

[Serializable]
public class RunSaveBlock
{
    public bool tutorialCompleted;

    /// <summary>0 = lobby, 1 = tutorial, 2 = arena1, 3 = arena2</summary>
    public int activeWorld;

    public float playerX, playerY, playerZ;

    public float healthCurrent;
    public float healthMax;

    public int runLevel = 1;
    public float runXP;
    public float runXPThreshold;
    public float pickupRadius;

    public float weaponDamage;
    public float weaponFireRate;
    public int weaponPierce;
    public int weaponProjectileCount;

    public float moveSpeed;
    public float dashCooldown;

    public int waveIndex;
    public bool wavesHasStarted;
    public bool waveIsBreak;

    public int wavesClearedStat;
    public int totalKillsStat;

    /// <summary>Upgrade IDs applied in order during this run (for debugging / future replay).</summary>
    public string[] appliedUpgradeIds = Array.Empty<string>();
}

[Serializable]
public class HighScoreSaveBlock
{
    public int bestWaves;
    public float bestTime;
    public int bestKills;
}

[Serializable]
public class SettingsSaveBlock
{
    public float masterVolume =1f;
    public float sfxVolume = 1f;
    public float musicVolume = 1f;
}
