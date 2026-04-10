using UnityEngine;

/// <summary>
/// Attached alongside a tutorial demo trap to override the ArenaTrap cycle timings
/// so the warning ring and activation appear quickly — before Start() fires on
/// the sibling ArenaTrap component.
///
/// Sets a short initial delay (0.3–0.8 s random) and a short cooldown (1.4 s)
/// so the player sees at least 2–3 full warn→active→retract cycles during the
/// trap explanation section.
///
/// Self-destructs after applying settings.
/// </summary>
[DefaultExecutionOrder(-10)]   // run before ArenaTrap.Start()
public class DemoTrapFastCycle : MonoBehaviour
{
    void Awake()
    {
        ArenaTrap trap = GetComponent<ArenaTrap>();
        if (trap == null) { Destroy(this); return; }

        // Use reflection-free approach: directly set the serialized backing fields
        // via the public-facing protected properties we can expose through a subclass
        // trick — instead just rely on the field defaults and override via the
        // Unity serialized field injection pattern used by our own trap subclasses.
        // The simplest reliable path: use SendMessage to call a setup method
        // (no boxing, no reflection).
        trap.SendMessage("SetDemoCycleTimings", SendMessageOptions.DontRequireReceiver);

        Destroy(this);   // one-shot — remove after Awake
    }
}
