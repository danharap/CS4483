using UnityEngine;

/// <summary>
/// Reads the player's unlocked skill IDs from <see cref="AccountProgression"/> and applies
/// the matching passive stat boosts + behaviour components at the start of each run.
/// Call <see cref="ApplyAll"/> once after the player spawns / enters the arena.
/// </summary>
public static class MetaPassiveApplicator
{
    // ── Permanent stat values (must match SkillTreeCatalogue descriptions) ──

    // Striker track
    private const float DmgBoost1 = 0.04f;   // Keen Edge I
    private const float DmgBoost2 = 0.04f;   // Keen Edge II
    private const float DmgBoost3 = 0.05f;   // Keen Edge III

    // Survivor track
    private const float HP1 = 12f;            // Thick Skin I
    private const float HP2 = 12f;            // Thick Skin II
    private const float HP3 = 15f;            // Thick Skin III

    // Skirmisher track
    private const float Speed1    =  0.03f;   // Light Foot I
    private const float Speed2    =  0.03f;   // Light Foot II
    private const float DashCD3   = -0.05f;   // Quick Recovery (fraction of base)

    // ── Apply ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Applies all unlocked meta passives to the player. Should be called every time
    /// a new run begins (i.e., when entering Arena 1 from the lobby).
    /// Stat passives are additive on top of the post-reset base values.
    /// Capstone behaviours are added as MonoBehaviour components (duplicates are avoided).
    /// </summary>
    public static void ApplyAll(PlayerHealth health, PlayerWeapon weapon, PlayerController controller)
    {
        if (AccountProgression.Instance == null) return;
        var unlocked = AccountProgression.Instance.UnlockedSkills;

        // ── Striker stat nodes ────────────────────────────────────────────
        if (unlocked.Contains("meta_strike_1") && weapon != null)
            weapon.damage *= (1f + DmgBoost1);
        if (unlocked.Contains("meta_strike_2") && weapon != null)
            weapon.damage *= (1f + DmgBoost2);
        if (unlocked.Contains("meta_strike_3") && weapon != null)
            weapon.damage *= (1f + DmgBoost3);

        // ── Survivor stat nodes ───────────────────────────────────────────
        if (unlocked.Contains("meta_vit_1") && health != null)
            health.AddMaxHP(HP1);
        if (unlocked.Contains("meta_vit_2") && health != null)
            health.AddMaxHP(HP2);
        if (unlocked.Contains("meta_vit_3") && health != null)
            health.AddMaxHP(HP3);

        // ── Skirmisher stat nodes ─────────────────────────────────────────
        if (unlocked.Contains("meta_mob_1") && controller != null)
            controller.moveSpeed *= (1f + Speed1);
        if (unlocked.Contains("meta_mob_2") && controller != null)
            controller.moveSpeed *= (1f + Speed2);
        if (unlocked.Contains("meta_mob_3") && controller != null)
            controller.dashCooldown *= (1f + DashCD3); // DashCD3 is negative, so this reduces it

        // ── Capstone behaviour components ─────────────────────────────────
        GameObject playerGO = health != null ? health.gameObject
                            : weapon != null ? weapon.gameObject
                            : controller != null ? controller.gameObject : null;

        if (playerGO == null) return;

        if (unlocked.Contains("meta_first_blood"))
            EnsureComponent<OpeningStrikeTracker>(playerGO);
        else
            RemoveComponent<OpeningStrikeTracker>(playerGO);

        if (unlocked.Contains("meta_second_wind"))
            EnsureComponent<SecondWindPassive>(playerGO);
        else
            RemoveComponent<SecondWindPassive>(playerGO);

        if (unlocked.Contains("meta_ghost_step"))
            EnsureComponent<GhostStepPassive>(playerGO);
        else
            RemoveComponent<GhostStepPassive>(playerGO);

        Debug.Log("[MetaPassiveApplicator] Applied meta passives for this run.");
    }

    private static T EnsureComponent<T>(GameObject go) where T : Component
    {
        T existing = go.GetComponent<T>();
        return existing != null ? existing : go.AddComponent<T>();
    }

    private static void RemoveComponent<T>(GameObject go) where T : Component
    {
        T existing = go.GetComponent<T>();
        if (existing != null) Object.Destroy(existing);
    }
}
