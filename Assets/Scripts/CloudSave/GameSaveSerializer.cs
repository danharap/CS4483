using System;
using Newtonsoft.Json;
using UnityEngine;

public static class GameSaveSerializer
{
    public const int CurrentPayloadVersion = 1;

    public static string ToJson(GameSaveDocument doc)
    {
        doc.version = CurrentPayloadVersion;
        return JsonConvert.SerializeObject(doc, Formatting.None, new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore
        });
    }

    public static bool TryParse(string json, out GameSaveDocument doc, out string error)
    {
        doc = null;
        error = null;
        if (string.IsNullOrWhiteSpace(json))
        {
            error = "Empty save data.";
            return false;
        }

        try
        {
            doc = JsonConvert.DeserializeObject<GameSaveDocument>(json);
        }
        catch (Exception e)
        {
            error = e.Message;
            return false;
        }

        if (doc == null)
        {
            error = "Could not parse save.";
            return false;
        }

        if (doc.version < 1 || doc.version > CurrentPayloadVersion)
        {
            error = $"Unsupported save version {doc.version}.";
            return false;
        }

        if (doc.account == null) doc.account = new AccountSaveBlock();
        if (doc.run == null) doc.run = new RunSaveBlock();
        if (doc.highScore == null) doc.highScore = new HighScoreSaveBlock();
        if (doc.settings == null) doc.settings = new SettingsSaveBlock();

        if (doc.account.unlockedSkills == null) doc.account.unlockedSkills = Array.Empty<string>();
        if (doc.run.appliedUpgradeIds == null) doc.run.appliedUpgradeIds = Array.Empty<string>();

        return true;
    }

    public static GameSaveDocument CreateNewRun(string slotLabelIgnored = null)
    {
        return new GameSaveDocument
        {
            version = CurrentPayloadVersion,
            account = new AccountSaveBlock
            {
                accountLevel = 1,
                unlockedSkills = Array.Empty<string>()
            },
            run = new RunSaveBlock
            {
                tutorialCompleted = false,
                activeWorld = 0,
                playerX = 0f, playerY = 1.1f, playerZ = 0f,
                healthCurrent = 100f,
                healthMax = 100f,
                runLevel = 1,
                runXP = 0f,
                runXPThreshold = 50f,
                pickupRadius = 2.5f,
                weaponDamage = 20f,
                weaponFireRate = 2f,
                weaponPierce = 0,
                weaponProjectileCount = 1,
                moveSpeed = 7f,
                dashCooldown = 1.5f,
                waveIndex = 0,
                wavesHasStarted = false,
                waveIsBreak = false
            },
            highScore = new HighScoreSaveBlock(),
            settings = new SettingsSaveBlock
            {
                masterVolume = PlayerPrefs.GetFloat(SettingsManager.KeyMasterVol, 0.8f),
                sfxVolume    = PlayerPrefs.GetFloat(SettingsManager.KeySFXVol,    1f),
                musicVolume  = PlayerPrefs.GetFloat(SettingsManager.KeyMusicVol,  0.6f)
            }
        };
    }
}
