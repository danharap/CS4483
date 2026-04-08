using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton that tracks account-level meta-progression across all runs.
/// Meta XP is awarded when waves are cleared. Account levels unlock skill-point slots.
/// Data is persisted via PlayerPrefs so it survives session restarts.
/// </summary>
public class AccountProgression : MonoBehaviour
{
    public static AccountProgression Instance { get; private set; }

    // ── Tunables ──────────────────────────────────────────────────────────
    [Header("XP Curve")]
    [Tooltip("Meta XP required per account level (linear curve).")]
    [SerializeField] private int xpPerLevel = 1500;

    // ── Events ────────────────────────────────────────────────────────────
    /// <summary>Fired whenever meta XP changes. Args: (currentXP, xpToNextLevel, accountLevel, skillPoints)</summary>
    public event Action<int, int, int, int> OnProgressionChanged;
    /// <summary>Fired when a new account level is reached.</summary>
    public event Action<int> OnAccountLevelUp;
    /// <summary>Fired when a skill node is successfully unlocked.</summary>
    public event Action<string> OnSkillUnlocked;

    // ── State ─────────────────────────────────────────────────────────────
    public int AccountLevel   { get; private set; } = 1;
    public int MetaXP         { get; private set; } = 0;    // XP within current level
    public int TotalMetaXP    { get; private set; } = 0;    // cumulative (for debugging)
    public int SkillPoints    { get; private set; } = 0;

    /// <summary>IDs of all permanently unlocked skill nodes.</summary>
    public HashSet<string> UnlockedSkills { get; private set; } = new HashSet<string>();

    // ── PlayerPrefs keys ─────────────────────────────────────────────────
    private const string KEY_LEVEL        = "Meta_AccountLevel";
    private const string KEY_META_XP      = "Meta_MetaXP";
    private const string KEY_TOTAL_XP     = "Meta_TotalMetaXP";
    private const string KEY_SKILL_POINTS = "Meta_SkillPoints";
    private const string KEY_UNLOCKED     = "Meta_UnlockedSkills"; // CSV of IDs

    // ── Lifecycle ─────────────────────────────────────────────────────────

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Load();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // ── Persistence ───────────────────────────────────────────────────────

    private void Load()
    {
        AccountLevel  = PlayerPrefs.GetInt(KEY_LEVEL, 1);
        MetaXP        = PlayerPrefs.GetInt(KEY_META_XP, 0);
        TotalMetaXP   = PlayerPrefs.GetInt(KEY_TOTAL_XP, 0);
        SkillPoints   = PlayerPrefs.GetInt(KEY_SKILL_POINTS, 0);

        string csv = PlayerPrefs.GetString(KEY_UNLOCKED, "");
        UnlockedSkills = new HashSet<string>();
        if (!string.IsNullOrEmpty(csv))
            foreach (var id in csv.Split(','))
                if (!string.IsNullOrEmpty(id)) UnlockedSkills.Add(id);

        Debug.Log($"[AccountProgression] Loaded: Level {AccountLevel}, MetaXP {MetaXP}, SP {SkillPoints}, Unlocked: {csv}");
    }

    private void Save()
    {
        PlayerPrefs.SetInt(KEY_LEVEL, AccountLevel);
        PlayerPrefs.SetInt(KEY_META_XP, MetaXP);
        PlayerPrefs.SetInt(KEY_TOTAL_XP, TotalMetaXP);
        PlayerPrefs.SetInt(KEY_SKILL_POINTS, SkillPoints);
        PlayerPrefs.SetString(KEY_UNLOCKED, string.Join(",", UnlockedSkills));
        PlayerPrefs.Save();
    }

    // ── XP ────────────────────────────────────────────────────────────────

    /// <summary>Award meta XP and trigger level-ups. Called by GameManager on wave clear.</summary>
    public void AddMetaXP(int amount)
    {
        if (amount <= 0) return;

        TotalMetaXP += amount;
        MetaXP      += amount;

        int levelsGained = 0;
        while (MetaXP >= XPToNextLevel())
        {
            MetaXP -= XPToNextLevel();
            AccountLevel++;
            SkillPoints++;
            levelsGained++;
            OnAccountLevelUp?.Invoke(AccountLevel);
            Debug.Log($"[AccountProgression] Account level up! Now level {AccountLevel}. Skill points: {SkillPoints}");
        }

        Save();
        FireProgressionChanged();
    }

    /// <summary>XP threshold to reach the next account level.</summary>
    public int XPToNextLevel() => xpPerLevel;

    // ── Skill Unlocking ───────────────────────────────────────────────────

    /// <summary>
    /// Returns true and unlocks the node if all prerequisites are met.
    /// Failure reasons are logged for debugging.
    /// </summary>
    public bool TryUnlockSkill(string id)
    {
        if (UnlockedSkills.Contains(id))
        {
            Debug.Log($"[AccountProgression] '{id}' already unlocked.");
            return false;
        }

        SkillNode node = SkillTreeCatalogue.Get(id);
        if (node == null)
        {
            Debug.LogWarning($"[AccountProgression] Unknown skill id '{id}'.");
            return false;
        }

        if (SkillPoints < node.cost)
        {
            Debug.Log($"[AccountProgression] Not enough skill points to unlock '{id}' (have {SkillPoints}, need {node.cost}).");
            return false;
        }

        if (AccountLevel < node.minAccountLevel)
        {
            Debug.Log($"[AccountProgression] Account level {AccountLevel} too low for '{id}' (need {node.minAccountLevel}).");
            return false;
        }

        if (!string.IsNullOrEmpty(node.previousNodeId) && !UnlockedSkills.Contains(node.previousNodeId))
        {
            Debug.Log($"[AccountProgression] Must unlock '{node.previousNodeId}' before '{id}'.");
            return false;
        }

        SkillPoints -= node.cost;
        UnlockedSkills.Add(id);
        Save();
        FireProgressionChanged();
        OnSkillUnlocked?.Invoke(id);
        Debug.Log($"[AccountProgression] Unlocked '{node.displayName}'! Remaining skill points: {SkillPoints}");
        return true;
    }

    public bool IsUnlocked(string id) => UnlockedSkills.Contains(id);

    /// <summary>Returns whether this node is currently purchasable (prereqs met, have points, level ok, not bought).</summary>
    public bool CanUnlock(string id)
    {
        if (UnlockedSkills.Contains(id)) return false;
        SkillNode node = SkillTreeCatalogue.Get(id);
        if (node == null) return false;
        if (SkillPoints < node.cost) return false;
        if (AccountLevel < node.minAccountLevel) return false;
        if (!string.IsNullOrEmpty(node.previousNodeId) && !UnlockedSkills.Contains(node.previousNodeId)) return false;
        return true;
    }

    // ── Debug helpers (also compiled into standalone — used by DebugSpawnHotkeys F4/F5) ──

    /// <summary>Grant a large chunk of meta XP to test level-ups quickly.</summary>
    public void DebugGrantXP(int amount = 300) => AddMetaXP(amount);

    /// <summary>
    /// Instantly grant N account levels (and N skill points) without accumulating XP.
    /// </summary>
    public void DebugGrantLevels(int count = 1)
    {
        for (int i = 0; i < count; i++)
        {
            AccountLevel++;
            SkillPoints++;
            OnAccountLevelUp?.Invoke(AccountLevel);
        }
        Save();
        FireProgressionChanged();
        Debug.Log($"[AccountProgression] DEBUG: Granted {count} level(s). Now level {AccountLevel}, SP={SkillPoints}.");
    }

    /// <summary>Reset all account progression (dev convenience).</summary>
    public void DebugResetAll()
    {
        AccountLevel  = 1;
        MetaXP        = 0;
        TotalMetaXP   = 0;
        SkillPoints   = 0;
        UnlockedSkills.Clear();
        Save();
        FireProgressionChanged();
        Debug.Log("[AccountProgression] DEBUG RESET complete.");
    }

    private void FireProgressionChanged() =>
        OnProgressionChanged?.Invoke(MetaXP, XPToNextLevel(), AccountLevel, SkillPoints);
}
