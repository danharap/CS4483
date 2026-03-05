using UnityEngine;

/// <summary>
/// Extends NarrativeTrigger for the arena's Watcher pillars.
/// Each pillar has pre-written lore tied to the world's backstory.
/// Satisfies Activity 3: Prop-based storytelling + Interactive Narrative Triggers.
/// </summary>
public class LorePillar : NarrativeTrigger
{
    public enum PillarType { Central, North, East, West, Boss }

    [Header("Pillar Identity")]
    [SerializeField] private PillarType pillarType = PillarType.Central;

    // ── Lore Database ──────────────────────────────────────────────────────
    // Each Watcher has a name and a lore entry explaining part of the world's history.
    private static readonly (string title, string body)[] LoreDB = {
        // Central
        (
            "THE FRACTURE — Watcher Record",
            "In the age of the Architects, warriors sought glory here.\n\n" +
            "When the Shard of Chaos was brought within these walls, reality fractured. " +
            "All who die here are bound to repeat the cycle. Their essence becomes the enemy you face.\n\n" +
            "You are not the first to enter. You will not be the last.\n\nFight well."
        ),
        // North
        (
            "WATCHER NARA — Keeper of Restoration",
            "I am Nara, last Watcher of Restoration. My power fades with each cycle.\n\n" +
            "The shrine nearby still draws from what little light I hold. " +
            "Those who seek healing may approach it — but do not linger. " +
            "The Corrupted One grows stronger with every wave you survive.\n\n" +
            "Seek it out. Face it. Perhaps you can end what the Architects could not."
        ),
        // East
        (
            "WATCHER VEX — Keeper of Growth",
            "I am Vex, Watcher of Growth. I have recorded every warrior who stood here.\n\n" +
            "The upgrades you receive are not random gifts. They are echoes — " +
            "the accumulated strength of those who fought before you, " +
            "distilled into power for those who follow.\n\n" +
            "When you grow stronger, you carry them with you. Choose wisely. " +
            "Their sacrifice is your advantage."
        ),
        // West
        (
            "WATCHER DRAK — Keeper of Passage",
            "I am Drak, Watcher of Passage. I guard the threshold.\n\n" +
            "Beyond the southern gate lies the Corrupted Champion. " +
            "It was once called Thorn — the greatest warrior to ever enter these grounds. " +
            "The Fracture claimed them first.\n\n" +
            "What you fight is not a warrior. It is what remains when a soul refuses defeat. " +
            "Be warned: Thorn remembers every warrior who came before you."
        ),
        // Boss
        (
            "THE CORRUPTED CHAMPION — Final Record",
            "This is where it ends. Or where it begins again.\n\n" +
            "Thorn has waited through countless cycles. Each warrior who defeated it " +
            "fed the cycle further — their victory becoming Thorn's next evolution.\n\n" +
            "But perhaps you are different. Perhaps this time, the cycle can be broken.\n\n" +
            "Or perhaps you will simply add another echo to the Grounds.\n\n" +
            "Enter."
        )
    };

    protected override void Awake()
    {
        base.Awake();

        int idx = (int)pillarType;
        if (idx >= 0 && idx < LoreDB.Length)
        {
            // Set the fields that NarrativeTrigger will display
            var so = LoreDB[idx];
            // We use reflection-style approach via the serialized fields — just set directly
            narrativeTitle = so.title;
            narrativeBody  = so.body;
        }
    }
}
