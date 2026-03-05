using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Picks 3 weighted-random upgrades and applies the chosen one to the player.
/// Called by UpgradeUI when the player selects a card.
/// </summary>
public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // ── Selection ─────────────────────────────────────────────────────────

    /// <summary>
    /// Returns 3 unique upgrades sampled with rarity-weighted random.
    /// If <paramref name="forceRare"/> is true, at least one Rare is included.
    /// </summary>
    public List<UpgradeData> GetOptions(int waveIndex, bool forceRare)
    {
        List<UpgradeData> pool = new List<UpgradeData>(UpgradeCatalogue.All);
        List<UpgradeData> result = new List<UpgradeData>();

        if (forceRare)
        {
            // Grab one rare first
            List<UpgradeData> rares = pool.FindAll(u => u.Rarity == UpgradeRarity.Rare);
            if (rares.Count > 0)
            {
                UpgradeData rare = rares[Random.Range(0, rares.Count)];
                result.Add(rare);
                pool.Remove(rare);
            }
        }

        // Fill remaining slots with weighted random
        while (result.Count < 3 && pool.Count > 0)
        {
            UpgradeData pick = WeightedPick(pool, waveIndex);
            result.Add(pick);
            pool.Remove(pick);
        }

        return result;
    }

    private UpgradeData WeightedPick(List<UpgradeData> pool, int waveIndex)
    {
        float totalWeight = 0f;
        foreach (var u in pool)
            totalWeight += UpgradeCatalogue.RarityWeight(u.Rarity, waveIndex);

        float roll = Random.Range(0f, totalWeight);
        float cumulative = 0f;
        foreach (var u in pool)
        {
            cumulative += UpgradeCatalogue.RarityWeight(u.Rarity, waveIndex);
            if (roll <= cumulative) return u;
        }
        return pool[pool.Count - 1];
    }

    // ── Application ───────────────────────────────────────────────────────

    public void Apply(UpgradeData upgrade)
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        PlayerController ctrl   = gm.PlayerController;
        PlayerWeapon     weapon = gm.PlayerWeapon;
        PlayerHealth     health = gm.PlayerHealth;
        PlayerXP         xp     = gm.PlayerXP;

        switch (upgrade.ID)
        {
            case "dmg":
                if (weapon) weapon.damage *= 1.20f;
                break;
            case "atkspd":
                if (weapon) weapon.fireRate *= 1.15f;
                break;
            case "movespd":
                if (ctrl) ctrl.moveSpeed *= 1.10f;
                break;
            case "maxhp":
                if (health) health.AddMaxHP(20f);
                break;
            case "pickup":
                if (xp) xp.pickupRadius *= 1.25f;
                break;
            case "pierce":
                if (weapon) weapon.pierceCount += 1;
                break;
            case "multishot":
                if (weapon) weapon.projectileCount += 1;
                break;
            case "dash":
                if (ctrl) ctrl.dashCooldown *= 0.75f;
                break;
        }

        Debug.Log($"[Upgrade] Applied: {upgrade.DisplayName}");
    }
}
