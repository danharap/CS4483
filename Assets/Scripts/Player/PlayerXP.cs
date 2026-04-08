using System;
using UnityEngine;

/// <summary>
/// Manages run-level XP, level-up logic, and pickup radius for XP orbs.
/// </summary>
public class PlayerXP : MonoBehaviour
{
    // ── Tunables ──────────────────────────────────────────────────────────
    [Header("XP / Level")]
    [SerializeField] private float baseXPThreshold = 50f;  // XP needed for first level-up
    [SerializeField] private float xpScalePerLevel  = 1.15f; // multiplier per level
    [SerializeField] public  float pickupRadius     = 2.5f;

    // ── State ─────────────────────────────────────────────────────────────
    public float CurrentXP  { get; private set; }
    public float XPThreshold { get; private set; }
    public int   RunLevel   { get; private set; } = 1;

    private float basePickupRadius;

    public event Action<float, float, int> OnXPChanged; // current, max, level
    public event Action<int>               OnLevelUp;   // new level

    void Awake()
    {
        XPThreshold     = baseXPThreshold;
        basePickupRadius = pickupRadius;
    }

    /// <summary>Full stat reset for respawn: clear XP/level and undo upgrade-applied pickup radius.</summary>
    public void ResetToBase()
    {
        CurrentXP    = 0f;
        RunLevel     = 1;
        XPThreshold  = baseXPThreshold;
        pickupRadius = basePickupRadius;
        OnXPChanged?.Invoke(CurrentXP, XPThreshold, RunLevel);
    }

    public void AddXP(float amount)
    {
        CurrentXP += amount;
        OnXPChanged?.Invoke(CurrentXP, XPThreshold, RunLevel);

        if (CurrentXP >= XPThreshold)
            LevelUp();
    }

    private void LevelUp()
    {
        CurrentXP -= XPThreshold;
        RunLevel++;
        XPThreshold *= xpScalePerLevel;

        OnLevelUp?.Invoke(RunLevel);

        GameManager.Instance?.Logger?.RecordFirstLevelUp();
        GameManager.Instance?.PauseForUpgrade(false);

        // Notify HUD
        OnXPChanged?.Invoke(CurrentXP, XPThreshold, RunLevel);
    }

    /// <summary>Grant XP to trigger the next level-up (upgrade panel). F2 in <see cref="DebugSpawnHotkeys"/>.</summary>
    public void DebugForceLevelUp()
    {
        float need = Mathf.Max(0f, XPThreshold - CurrentXP);
        AddXP(need + 0.01f);
    }
}
