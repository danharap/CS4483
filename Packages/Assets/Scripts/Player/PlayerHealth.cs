using System;
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

    public event Action OnDeath;
    public event Action<float, float> OnHealthChanged; // current, max

    void Awake()
    {
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
            OnDeath?.Invoke();
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
}
