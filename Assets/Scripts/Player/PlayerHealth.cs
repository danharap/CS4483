using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages player HP, damage feedback (screen flash), and death.
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    // ── Tunables ──────────────────────────────────────────────────────────
    [Header("HP")]
    [SerializeField] public float maxHP = 100f;
    [SerializeField] private float iFramesDuration = 0.5f;

    [Header("Damage Feedback")]
    [SerializeField] private Image damageOverlay;   // full-screen red Image (alpha ~0.4)
    [SerializeField] private float flashFadeSpeed = 4f;

    // ── State ─────────────────────────────────────────────────────────────
    public float CurrentHP { get; private set; }
    private float iFramesTimer;
    private float overlayAlpha;

    private float baseMaxHP;

    public event Action OnDeath;
    public event Action<float, float> OnHealthChanged; // current, max

    /// <summary>
    /// Subscribed by <see cref="SecondWindPassive"/>. Return true to absorb the killing blow
    /// (HP will be clamped to 1 HP instead of triggering death).
    /// </summary>
    public event Func<bool> OnAboutToTakeFatalDamage;

    void Awake()
    {
        baseMaxHP = maxHP;
        CurrentHP = maxHP;
    }

    void Update()
    {
        iFramesTimer -= Time.deltaTime;

        // Fade out damage overlay
        if (overlayAlpha > 0f)
        {
            overlayAlpha = Mathf.MoveTowards(overlayAlpha, 0f, flashFadeSpeed * Time.deltaTime);
            if (damageOverlay != null)
                damageOverlay.color = new Color(1f, 0f, 0f, overlayAlpha);
        }
    }

    public void TakeDamage(float amount)
    {
        if (iFramesTimer > 0f) return;
        if (CurrentHP <= 0f) return;

        CurrentHP = Mathf.Max(0f, CurrentHP - amount);
        iFramesTimer = iFramesDuration;

        // Screen flash
        overlayAlpha = 0.45f;
        if (damageOverlay != null)
            damageOverlay.color = new Color(1f, 0f, 0f, overlayAlpha);

        OnHealthChanged?.Invoke(CurrentHP, maxHP);

        if (CurrentHP <= 0f)
        {
            // Give SecondWindPassive a chance to absorb the kill
            bool absorbed = false;
            if (OnAboutToTakeFatalDamage != null)
            {
                foreach (Func<bool> listener in OnAboutToTakeFatalDamage.GetInvocationList())
                {
                    if (listener())
                    {
                        absorbed = true;
                        break;
                    }
                }
            }

            if (absorbed)
            {
                CurrentHP = 1f;
                OnHealthChanged?.Invoke(CurrentHP, maxHP);
            }
            else
            {
                OnDeath?.Invoke();
            }
        }
    }

    public void HealHP(float amount)
    {
        CurrentHP = Mathf.Min(maxHP, CurrentHP + amount);
        OnHealthChanged?.Invoke(CurrentHP, maxHP);
    }

    public void AddMaxHP(float amount)
    {
        maxHP += amount;
        CurrentHP += amount; // also heal the added amount
        OnHealthChanged?.Invoke(CurrentHP, maxHP);
    }

    /// <summary>
    /// Restore HP back to max and clear any death state / screen tint.
    /// </summary>
    /// <summary>Instantly grant iFrames for the given duration (used by Ghost Step).</summary>
    public void ForceIFrames(float duration)
    {
        iFramesTimer = Mathf.Max(iFramesTimer, duration);
    }

    public void ResetHealthToMax()
    {
        CurrentHP = maxHP;
        OnHealthChanged?.Invoke(CurrentHP, maxHP);
    }

    /// <summary>
    /// Full stat reset for respawn: restores maxHP and currentHP to their serialized defaults,
    /// undoing any upgrade increases to max HP.
    /// </summary>
    public void ResetToBase()
    {
        maxHP     = baseMaxHP;
        CurrentHP = maxHP;
        iFramesTimer = 0f;
        overlayAlpha = 0f;
        if (damageOverlay != null)
            damageOverlay.color = new Color(1f, 0f, 0f, 0f);
        OnHealthChanged?.Invoke(CurrentHP, maxHP);
    }

    /// <summary>Restore HP/max from a cloud save snapshot (may differ from base after upgrades).</summary>
    public void RestoreFromSave(float current, float max)
    {
        maxHP     = Mathf.Max(1f, max);
        float safeCurrent = current;
        if (float.IsNaN(safeCurrent) || float.IsInfinity(safeCurrent) || safeCurrent <= 0f)
            safeCurrent = maxHP;

        CurrentHP = Mathf.Clamp(safeCurrent, 1f, maxHP);
        iFramesTimer = 0f;
        overlayAlpha = 0f;
        if (damageOverlay != null)
            damageOverlay.color = new Color(1f, 0f, 0f, 0f);
        OnHealthChanged?.Invoke(CurrentHP, maxHP);
    }
}
