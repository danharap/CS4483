using System.Collections.Generic;
using UnityEngine;

// ── Rarity ───────────────────────────────────────────────────────────────────
public enum UpgradeRarity { Common, Uncommon, Rare }

// ── Single upgrade definition ─────────────────────────────────────────────────
[System.Serializable]
public class UpgradeData
{
    public string        ID;
    public string        DisplayName;
    public string        Description;
    public UpgradeRarity Rarity;
}

// ── Static catalogue of all upgrades ─────────────────────────────────────────
public static class UpgradeCatalogue
{
    public static readonly List<UpgradeData> All = new List<UpgradeData>
    {
        new UpgradeData { ID = "dmg",       DisplayName = "+Damage",        Description = "Projectile damage +20%",        Rarity = UpgradeRarity.Common   },
        new UpgradeData { ID = "atkspd",    DisplayName = "+Attack Speed",  Description = "Fire rate +15%",                Rarity = UpgradeRarity.Common   },
        new UpgradeData { ID = "movespd",   DisplayName = "+Move Speed",    Description = "Movement speed +10%",           Rarity = UpgradeRarity.Common   },
        new UpgradeData { ID = "maxhp",     DisplayName = "+Max HP",        Description = "Gain +20 max HP and heal 20",   Rarity = UpgradeRarity.Common   },
        new UpgradeData { ID = "pickup",    DisplayName = "+Pickup Radius", Description = "XP orb attraction radius +25%", Rarity = UpgradeRarity.Common   },
        new UpgradeData { ID = "pierce",    DisplayName = "+Pierce",        Description = "Projectiles pierce +1 enemy",   Rarity = UpgradeRarity.Uncommon },
        new UpgradeData { ID = "multishot", DisplayName = "+Multishot",     Description = "+1 simultaneous projectile",    Rarity = UpgradeRarity.Rare     },
        new UpgradeData { ID = "dash",      DisplayName = "+Agility",       Description = "Dash cooldown -25%",            Rarity = UpgradeRarity.Uncommon },
    };

    // Weight per rarity based on wave index
    public static float RarityWeight(UpgradeRarity r, int waveIndex)
    {
        // Rare upgrades become more common on later waves
        return r switch
        {
            UpgradeRarity.Common   => 6f,
            UpgradeRarity.Uncommon => 3f + waveIndex * 0.2f,
            UpgradeRarity.Rare     => 1f + waveIndex * 0.15f,
            _                     => 1f
        };
    }
}
