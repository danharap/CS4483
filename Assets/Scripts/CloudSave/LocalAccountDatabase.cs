using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;

/// <summary>
/// All accounts and save slots live in one JSON file under Application.persistentDataPath
/// (writable in standalone builds). No external services.
///
/// On first run, if the file is missing, we copy StreamingAssets/DefaultLocalAccounts/accounts.json
/// when present so you can ship starter data with the game.
/// </summary>
public static class LocalAccountDatabase
{
    public const int MaxSavesPerUser = 3;
    const int FileVersion = 1;
    const string SubDir = "CS4483";
    const string FileName = "local_accounts.json";
    const string StreamingSeed = "DefaultLocalAccounts/accounts.json";

    static readonly object FileLock = new object();

    [Serializable]
    public class RootFile
    {
        public int version = FileVersion;
        public List<UserRecord> users = new List<UserRecord>();
    }

    [Serializable]
    public class UserRecord
    {
        public string id;
        public string username;
        // legacy field name kept for backward compatibility with older local DB files
        public string email;
        public string passwordSalt;
        public string passwordHash;
        public AccountSaveBlock accountProfile = new AccountSaveBlock();
        public HighScoreSaveBlock highScoreProfile = new HighScoreSaveBlock();
        public SettingsSaveBlock settingsProfile = new SettingsSaveBlock();
        public List<SaveSlotRecord> saves = new List<SaveSlotRecord>();
    }

    [Serializable]
    public class SaveSlotRecord
    {
        public string id;
        public string slotLabel;
        public string updatedAtIso;
        public GameSaveDocument payload;
    }

    static string DbPath =>
        Path.Combine(Application.persistentDataPath, SubDir, FileName);

    public static RootFile LoadOrCreate()
    {
        lock (FileLock)
        {
            EnsureSeedFromStreamingAssets();
            if (!File.Exists(DbPath))
            {
                var fresh = new RootFile();
                SaveInternal(fresh);
                return fresh;
            }

            try
            {
                string json = File.ReadAllText(DbPath, Encoding.UTF8);
                var db = JsonConvert.DeserializeObject<RootFile>(json);
                if (db == null) db = new RootFile();
                if (db.users == null) db.users = new List<UserRecord>();
                db.version = FileVersion;
                return db;
            }
            catch (Exception e)
            {
                Debug.LogError("[LocalAccountDatabase] Corrupt DB, backing up and resetting. " + e.Message);
                try
                {
                    File.Copy(DbPath, DbPath + ".bak_" + DateTime.UtcNow.Ticks, true);
                }
                catch { /* ignore */ }
                var fresh = new RootFile();
                SaveInternal(fresh);
                return fresh;
            }
        }
    }

    static void EnsureSeedFromStreamingAssets()
    {
        if (File.Exists(DbPath)) return;
        string seed = Path.Combine(Application.streamingAssetsPath, StreamingSeed);
        if (!File.Exists(seed)) return;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DbPath) ?? Application.persistentDataPath);
            File.Copy(seed, DbPath, false);
            Debug.Log("[LocalAccountDatabase] Seeded DB from StreamingAssets.");
        }
        catch (Exception e)
        {
            Debug.LogWarning("[LocalAccountDatabase] Could not copy seed: " + e.Message);
        }
    }

    public static void Save(RootFile db)
    {
        lock (FileLock)
        {
            SaveInternal(db);
        }
    }

    static void SaveInternal(RootFile db)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(DbPath) ?? Application.persistentDataPath);
        string json = JsonConvert.SerializeObject(db, Formatting.Indented, new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore
        });
        File.WriteAllText(DbPath, json, Encoding.UTF8);
    }

    public static string Register(string username, string password, out string error)
    {
        error = null;
        username = NormalizeUsername(username);
        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            error = "Username and password required.";
            return null;
        }
        if (username.Length < 3)
        {
            error = "Username must be at least 3 characters.";
            return null;
        }
        if (password.Length < 4)
        {
            error = "Password must be at least 4 characters.";
            return null;
        }

        var db = LoadOrCreate();
        if (db.users.Any(u => string.Equals(GetUsername(u), username, StringComparison.OrdinalIgnoreCase)))
        {
            error = "That username already exists.";
            return null;
        }

        string salt = NewSalt();
        var user = new UserRecord
        {
            id = Guid.NewGuid().ToString("N"),
            username = username,
            email = username,
            passwordSalt = salt,
            passwordHash = HashPassword(password, salt),
            accountProfile = new AccountSaveBlock(),
            highScoreProfile = new HighScoreSaveBlock(),
            settingsProfile = SettingsManager.ExportSaveBlock(),
            saves = new List<SaveSlotRecord>()
        };
        db.users.Add(user);
        Save(db);
        return user.id;
    }

    public static string SignIn(string username, string password, out string error)
    {
        error = null;
        username = NormalizeUsername(username);
        var db = LoadOrCreate();
        var user = db.users.FirstOrDefault(u =>
            string.Equals(GetUsername(u), username, StringComparison.OrdinalIgnoreCase));
        if (user == null)
        {
            error = "Invalid username or password.";
            return null;
        }
        if (user.passwordHash != HashPassword(password, user.passwordSalt))
        {
            error = "Invalid username or password.";
            return null;
        }
        return user.id;
    }

    public static UserRecord FindUser(RootFile db, string userId) =>
        db.users.FirstOrDefault(u => u.id == userId);

    public static bool TryCreateSave(string userId, string slotLabel, GameSaveDocument doc, out SaveSlotRecord slot, out string error)
    {
        slot = null;
        error = null;
        var db = LoadOrCreate();
        var user = FindUser(db, userId);
        if (user == null)
        {
            error = "Not signed in.";
            return false;
        }
        if (user.saves.Count >= MaxSavesPerUser)
        {
            error = "Maximum 3 saves per account. Delete one first.";
            return false;
        }

        slot = new SaveSlotRecord
        {
            id = Guid.NewGuid().ToString("N"),
            slotLabel = string.IsNullOrWhiteSpace(slotLabel) ? "Save" : slotLabel.Trim(),
            updatedAtIso = DateTime.UtcNow.ToString("o"),
            payload = doc
        };
        user.saves.Add(slot);
        Save(db);
        return true;
    }

    public static bool TryUpdateSave(string userId, string saveId, string slotLabel, GameSaveDocument doc, out string error)
    {
        error = null;
        var db = LoadOrCreate();
        var user = FindUser(db, userId);
        if (user == null)
        {
            error = "Not signed in.";
            return false;
        }
        var slot = user.saves.FirstOrDefault(s => s.id == saveId);
        if (slot == null)
        {
            error = "Save not found.";
            return false;
        }
        slot.payload = doc;
        slot.slotLabel = string.IsNullOrWhiteSpace(slotLabel) ? slot.slotLabel : slotLabel.Trim();
        slot.updatedAtIso = DateTime.UtcNow.ToString("o");
        Save(db);
        return true;
    }

    public static bool TryDeleteSave(string userId, string saveId, out string error)
    {
        error = null;
        var db = LoadOrCreate();
        var user = FindUser(db, userId);
        if (user == null)
        {
            error = "Not signed in.";
            return false;
        }
        int n = user.saves.RemoveAll(s => s.id == saveId);
        if (n == 0)
        {
            error = "Save not found.";
            return false;
        }
        Save(db);
        return true;
    }

    public static List<SaveSlotRecord> ListSaves(string userId)
    {
        var db = LoadOrCreate();
        var user = FindUser(db, userId);
        return user?.saves?.OrderByDescending(s => s.updatedAtIso).ToList() ?? new List<SaveSlotRecord>();
    }

    public static SaveSlotRecord FindSave(string userId, string saveId)
    {
        var db = LoadOrCreate();
        var user = FindUser(db, userId);
        return user?.saves?.FirstOrDefault(s => s.id == saveId);
    }

    public static string GetUserEmail(string userId)
    {
        if (string.IsNullOrEmpty(userId)) return "";
        return GetUsername(FindUser(LoadOrCreate(), userId));
    }

    public static bool TryLoadAccountProfile(string userId, out AccountSaveBlock account, out HighScoreSaveBlock highScore, out SettingsSaveBlock settings)
    {
        account = null;
        highScore = null;
        settings = null;
        if (string.IsNullOrEmpty(userId)) return false;
        var user = FindUser(LoadOrCreate(), userId);
        if (user == null) return false;
        account = user.accountProfile ?? new AccountSaveBlock();
        highScore = user.highScoreProfile ?? new HighScoreSaveBlock();
        settings = user.settingsProfile ?? SettingsManager.ExportSaveBlock();
        return true;
    }

    public static void SaveAccountProfileFromRuntime(string userId)
    {
        if (string.IsNullOrEmpty(userId)) return;
        var db = LoadOrCreate();
        var user = FindUser(db, userId);
        if (user == null) return;
        user.accountProfile = AccountProgression.Instance != null
            ? AccountProgression.Instance.ExportSaveBlock()
            : (user.accountProfile ?? new AccountSaveBlock());
        user.highScoreProfile = HighScoreManager.Instance != null
            ? HighScoreManager.Instance.ExportSaveBlock()
            : (user.highScoreProfile ?? new HighScoreSaveBlock());
        user.settingsProfile = SettingsManager.ExportSaveBlock();
        Save(db);
    }

    static string NormalizeUsername(string e) => (e ?? "").Trim().ToLowerInvariant();
    static string GetUsername(UserRecord u) => (u?.username ?? u?.email ?? "").Trim();

    static string NewSalt()
    {
        var bytes = new byte[16];
        using (var rng = RandomNumberGenerator.Create())
            rng.GetBytes(bytes);
        return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
    }

    static string HashPassword(string password, string salt)
    {
        using (var sha = SHA256.Create())
        {
            byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(salt + ":" + password));
            return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
        }
    }
}
