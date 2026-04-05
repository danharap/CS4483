using UnityEngine;

/// <summary>
/// Capstone: "Second Wind" (meta_second_wind)
/// Once per run, a lethal hit brings the player to 1 HP instead of killing them.
/// Hooks into PlayerHealth. Added to the player when the skill is unlocked.
/// </summary>
[RequireComponent(typeof(PlayerHealth))]
public class SecondWindPassive : MonoBehaviour
{
    private PlayerHealth health;
    private bool used = false;

    void Awake()
    {
        health = GetComponent<PlayerHealth>();
    }

    void Start()
    {
        if (health != null)
            health.OnAboutToTakeFatalDamage += TryAbsorb;
    }

    void OnDestroy()
    {
        if (health != null)
            health.OnAboutToTakeFatalDamage -= TryAbsorb;
    }

    /// <summary>
    /// Called by PlayerHealth before killing the player.
    /// Returns true if Second Wind was triggered (damage cancelled, HP set to 1).
    /// </summary>
    private bool TryAbsorb()
    {
        if (used) return false;
        used = true;
        Debug.Log("[SecondWind] Triggered! Surviving at 1 HP.");
        return true;
    }
}
