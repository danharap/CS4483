using System.Collections;
using UnityEngine;

/// <summary>
/// Flame trap (Arena 2). Warn ring → burst flame → DOT while player inside → retract → cooldown.
/// Inherits telegraphed cycle from ArenaTrap.
/// </summary>
public class FlameTrap : ArenaTrap
{
    [Header("Flame Stats")]
    [SerializeField] private float damagePerTick   = 6f;
    [Tooltip("Seconds between burn ticks while player is inside the flame.")]
    [SerializeField] private float tickInterval    = 0.5f;
    [Tooltip("Horizontal radius of the flame hazard.")]
    [SerializeField] private float flameRadius     = 1.4f;

    [Header("Flame Visuals")]
    [SerializeField] private TrapSpriteAnimator spriteAnimator;
    [Tooltip("Optional particle system child for flame effect.")]
    [SerializeField] private ParticleSystem flameParticles;

    private Coroutine damageLoop;

    protected override void Start()
    {
        initialDelay = Random.Range(0f, cooldownDuration);
        warnRadius   = flameRadius;
        warnColor    = new Color(1f, 0.45f, 0.05f, 0.6f);
        base.Start();
    }

    protected override void OnWarnStart()
    {
        // Flame sprite dims during warning; only the ground ring warns the player.
        if (spriteAnimator != null) spriteAnimator.SetActive(false);
        if (flameParticles != null) flameParticles.Stop();
    }

    protected override void OnActivate()
    {
        if (spriteAnimator != null) spriteAnimator.SetActive(true);
        if (flameParticles != null) flameParticles.Play();
        if (damageLoop != null) StopCoroutine(damageLoop);
        damageLoop = StartCoroutine(DamageTick());
    }

    protected override void OnDeactivate()
    {
        if (spriteAnimator != null) spriteAnimator.SetActive(false);
        if (flameParticles != null) flameParticles.Stop();
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
        Collider[] hits = Physics.OverlapSphere(transform.position, flameRadius);
        foreach (Collider c in hits)
        {
            if (!c.CompareTag("Player")) continue;
            c.GetComponent<PlayerHealth>()?.TakeDamage(damagePerTick);
        }
    }
}
