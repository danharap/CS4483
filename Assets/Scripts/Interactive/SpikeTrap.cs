using System.Collections;
using UnityEngine;

/// <summary>
/// Spike trap. Hidden → warn ring → spikes rise → damage player inside radius → retract → cooldown.
/// Works with or without a TrapSpriteAnimator child.
/// The trap deals damage every tick while the player stays inside the active area.
/// </summary>
public class SpikeTrap : ArenaTrap
{
    [Header("Spike Stats")]
    [Tooltip("Damage per hit while player is inside the active radius.")]
    [SerializeField] private float damagePerTick   = 15f;
    [Tooltip("Seconds between damage ticks when player stays inside.")]
    [SerializeField] private float tickInterval    = 0.4f;
    [Tooltip("Radius the spike occupies when active. Should match warnRadius.")]
    [SerializeField] private float damageRadius    = 1.2f;

    [Header("Spike Visuals")]
    [SerializeField] private TrapSpriteAnimator spriteAnimator;
    [Tooltip("Optional: child GameObject to scale up/down as the spike rising effect.")]
    [SerializeField] private Transform spikeVisual;
    [Tooltip("Y-scale of spikeVisual when fully risen.")]
    [SerializeField] private float spikeRiseScale  = 1f;
    [SerializeField] private float riseSpeed       = 8f;

    private Coroutine damageLoop;

    protected override void Start()
    {
        // Initialise the base cycle (randomise initial offset so traps don't all fire at once).
        initialDelay = Random.Range(0f, cooldownDuration);
        warnRadius   = damageRadius;
        base.Start();
    }

    protected override void OnWarnStart()
    {
        // Keep sprite hidden / retracted during warning phase — only the ground ring is visible.
        if (spikeVisual != null)
            spikeVisual.localScale = Vector3.zero;
    }

    protected override void OnActivate()
    {
        if (spriteAnimator != null) spriteAnimator.SetActive(true);
        if (spikeVisual != null) StartCoroutine(RiseSpike(true));

        // Start ticking damage while active.
        if (damageLoop != null) StopCoroutine(damageLoop);
        damageLoop = StartCoroutine(DamageTick());
    }

    protected override void OnDeactivate()
    {
        if (spriteAnimator != null) spriteAnimator.SetActive(false);
        if (spikeVisual != null) StartCoroutine(RiseSpike(false));
        if (damageLoop != null) { StopCoroutine(damageLoop); damageLoop = null; }
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private IEnumerator DamageTick()
    {
        while (isActive)
        {
            DamagePlayersInRadius();
            yield return new WaitForSeconds(tickInterval);
        }
    }

    private void DamagePlayersInRadius()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, damageRadius);
        foreach (Collider c in hits)
        {
            if (!c.CompareTag("Player")) continue;
            PlayerHealth ph = c.GetComponent<PlayerHealth>();
            if (ph != null) ph.TakeDamage(damagePerTick);
        }
    }

    private IEnumerator RiseSpike(bool rise)
    {
        Vector3 target = rise ? new Vector3(1f, spikeRiseScale, 1f) : Vector3.zero;
        float speed = riseSpeed;
        while (spikeVisual != null &&
               Vector3.Distance(spikeVisual.localScale, target) > 0.01f)
        {
            spikeVisual.localScale = Vector3.MoveTowards(
                spikeVisual.localScale, target, speed * Time.deltaTime);
            yield return null;
        }
        if (spikeVisual != null) spikeVisual.localScale = target;
    }
}
