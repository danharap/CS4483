using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spike trap. Hidden → warn ring → spikes rise → damage player inside radius → retract → cooldown.
/// Works with or without a TrapSpriteAnimator child.
/// The trap deals damage every tick while the player stays inside the active area.
/// </summary>
[RequireComponent(typeof(Collider))]
public class SpikeTrap : MonoBehaviour
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
    [Tooltip("How long before this trap can be re-activated after the player leaves.")]
    [SerializeField] private float triggerCooldown = 1.5f;

    private Coroutine damageLoop;
    private readonly HashSet<PlayerHealth> playersInTrap = new HashSet<PlayerHealth>();
    private float cooldownTimer;
    private bool isActive;

    private void Awake()
    {
        if (spikeVisual != null)
            spikeVisual.localScale = Vector3.zero;

        if (spriteAnimator != null)
            spriteAnimator.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (cooldownTimer > 0f) return;

        PlayerHealth health = other.GetComponent<PlayerHealth>();
        if (health == null) return;

        playersInTrap.Add(health);

        if (!isActive)
            ActivateTrap();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        PlayerHealth health = other.GetComponent<PlayerHealth>();
        if (health != null)
            playersInTrap.Remove(health);

        if (playersInTrap.Count == 0 && isActive)
            DeactivateTrap();
    }

    private void Update()
    {
        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;
    }

    private void ActivateTrap()
    {
        isActive = true;
        if (spriteAnimator != null)
            spriteAnimator.SetActive(true);
        if (spikeVisual != null)
            StartCoroutine(RiseSpike(true));

        if (damageLoop != null)
            StopCoroutine(damageLoop);
        damageLoop = StartCoroutine(DamageTick());
    }

    private void DeactivateTrap()
    {
        isActive = false;
        if (spriteAnimator != null)
            spriteAnimator.SetActive(false);
        if (spikeVisual != null)
            StartCoroutine(RiseSpike(false));
        if (damageLoop != null)
        {
            StopCoroutine(damageLoop);
            damageLoop = null;
        }

        cooldownTimer = triggerCooldown;
    }

    private IEnumerator DamageTick()
    {
        while (isActive)
        {
            DamagePlayersInTrap();
            yield return new WaitForSeconds(tickInterval);
        }
    }

    private void DamagePlayersInTrap()
    {
        playersInTrap.RemoveWhere(p => p == null);
        foreach (PlayerHealth player in playersInTrap)
            player.TakeDamage(damagePerTick);
    }

    private void OnDisable()
    {
        playersInTrap.Clear();
        if (damageLoop != null)
        {
            StopCoroutine(damageLoop);
            damageLoop = null;
        }
        isActive = false;
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
