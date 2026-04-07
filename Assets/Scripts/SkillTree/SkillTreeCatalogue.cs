using System.Collections.Generic;
using UnityEngine;

// ── Node types ────────────────────────────────────────────────────────────────

public enum SkillNodeKind { SmallStat, Capstone }

[System.Serializable]
public class SkillNode
{
    public string        id;
    public string        displayName;
    public string        description;
    public SkillNodeKind kind;
    public string        trackId;          // "striker" | "survivor" | "skirmisher"
    public string        previousNodeId;   // null = first node in track; must unlock this first
    public int           minAccountLevel;  // account level gate (in addition to chain)
    public int           cost;             // skill points to unlock (almost always 1)
}

// ── Catalogue ─────────────────────────────────────────────────────────────────

/// <summary>
/// All skill tree nodes. IDs are stable keys stored in PlayerPrefs — never rename them.
/// Each node has exactly one previousNodeId (or null for a track's first node).
/// </summary>
public static class SkillTreeCatalogue
{
    // ─── Track A – Striker ────────────────────────────────────────────────────
    // Small: +4% / +4% / +5% permanent base weapon damage
    // Capstone: Opening Strike — first shot each wave deals double damage

    // ─── Track B – Survivor ───────────────────────────────────────────────────
    // Small: +12 / +12 / +15 permanent base max HP at run start
    // Capstone: Second Wind — survive one lethal hit per run at 1 HP

    // ─── Track C – Skirmisher ─────────────────────────────────────────────────
    // Small: +3% / +3% move speed, -5% dash cooldown
    // Capstone: Ghost Step — brief invuln after dash (internal CD)

    public static readonly List<SkillNode> All = new List<SkillNode>
    {
        // ── Track A – Striker ─────────────────────────────────────────────
        new SkillNode
        {
            id              = "meta_strike_1",
            displayName     = "Keen Edge I",
            description     = "Permanently increase base weapon damage by 4%.",
            kind            = SkillNodeKind.SmallStat,
            trackId         = "striker",
            previousNodeId  = null,
            minAccountLevel = 1,
            cost            = 1,
        },
        new SkillNode
        {
            id              = "meta_strike_2",
            displayName     = "Keen Edge II",
            description     = "Permanently increase base weapon damage by another 4%.",
            kind            = SkillNodeKind.SmallStat,
            trackId         = "striker",
            previousNodeId  = "meta_strike_1",
            minAccountLevel = 1,
            cost            = 1,
        },
        new SkillNode
        {
            id              = "meta_strike_3",
            displayName     = "Keen Edge III",
            description     = "Permanently increase base weapon damage by another 5%.",
            kind            = SkillNodeKind.SmallStat,
            trackId         = "striker",
            previousNodeId  = "meta_strike_2",
            minAccountLevel = 2,
            cost            = 1,
        },
        new SkillNode
        {
            id              = "meta_first_blood",
            displayName     = "Opening Strike",
            description     = "Your first shot of each wave deals 100% bonus damage. Once per wave.",
            kind            = SkillNodeKind.Capstone,
            trackId         = "striker",
            previousNodeId  = "meta_strike_3",
            minAccountLevel = 3,
            cost            = 1,
        },

        // ── Track B – Survivor ────────────────────────────────────────────
        new SkillNode
        {
            id              = "meta_vit_1",
            displayName     = "Thick Skin I",
            description     = "Start every run with +12 max HP.",
            kind            = SkillNodeKind.SmallStat,
            trackId         = "survivor",
            previousNodeId  = null,
            minAccountLevel = 1,
            cost            = 1,
        },
        new SkillNode
        {
            id              = "meta_vit_2",
            displayName     = "Thick Skin II",
            description     = "Start every run with another +12 max HP.",
            kind            = SkillNodeKind.SmallStat,
            trackId         = "survivor",
            previousNodeId  = "meta_vit_1",
            minAccountLevel = 1,
            cost            = 1,
        },
        new SkillNode
        {
            id              = "meta_vit_3",
            displayName     = "Thick Skin III",
            description     = "Start every run with another +15 max HP.",
            kind            = SkillNodeKind.SmallStat,
            trackId         = "survivor",
            previousNodeId  = "meta_vit_2",
            minAccountLevel = 2,
            cost            = 1,
        },
        new SkillNode
        {
            id              = "meta_second_wind",
            displayName     = "Second Wind",
            description     = "Once per run, survive a lethal blow at 1 HP instead of dying.",
            kind            = SkillNodeKind.Capstone,
            trackId         = "survivor",
            previousNodeId  = "meta_vit_3",
            minAccountLevel = 3,
            cost            = 1,
        },

        // ── Track C – Skirmisher ──────────────────────────────────────────
        new SkillNode
        {
            id              = "meta_mob_1",
            displayName     = "Light Foot I",
            description     = "Permanently increase base move speed by 3%.",
            kind            = SkillNodeKind.SmallStat,
            trackId         = "skirmisher",
            previousNodeId  = null,
            minAccountLevel = 1,
            cost            = 1,
        },
        new SkillNode
        {
            id              = "meta_mob_2",
            displayName     = "Light Foot II",
            description     = "Permanently increase base move speed by another 3%.",
            kind            = SkillNodeKind.SmallStat,
            trackId         = "skirmisher",
            previousNodeId  = "meta_mob_1",
            minAccountLevel = 1,
            cost            = 1,
        },
        new SkillNode
        {
            id              = "meta_mob_3",
            displayName     = "Quick Recovery",
            description     = "Permanently reduce base dash cooldown by 5%.",
            kind            = SkillNodeKind.SmallStat,
            trackId         = "skirmisher",
            previousNodeId  = "meta_mob_2",
            minAccountLevel = 2,
            cost            = 1,
        },
        new SkillNode
        {
            id              = "meta_ghost_step",
            displayName     = "Ghost Step",
            description     = "After dashing you become briefly invulnerable (0.3 s). 3 s internal cooldown.",
            kind            = SkillNodeKind.Capstone,
            trackId         = "skirmisher",
            previousNodeId  = "meta_mob_3",
            minAccountLevel = 3,
            cost            = 1,
        },
    };

    // ── Lookup helpers ────────────────────────────────────────────────────────

    private static Dictionary<string, SkillNode> s_byId;
    public static SkillNode Get(string id)
    {
        if (s_byId == null)
        {
            s_byId = new Dictionary<string, SkillNode>();
            foreach (var n in All) s_byId[n.id] = n;
        }
        s_byId.TryGetValue(id, out SkillNode result);
        return result;
    }

    public static List<SkillNode> GetTrack(string trackId)
    {
        var track = new List<SkillNode>();
        foreach (var n in All)
            if (n.trackId == trackId) track.Add(n);
        return track;
    }
}
