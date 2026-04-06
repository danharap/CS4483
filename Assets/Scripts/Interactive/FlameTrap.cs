using System.Collections;
using UnityEngine;

/// <summary>
/// Arena 2 flame trap.  When the player steps on it, they receive burn damage
/// as repeating ticks for a short duration.  Unlike the spike trap there is no
/// stun.  The flame animation plays continuously (fire is always lit).
/// </summary>
[RequireComponent(typeof(Collider))]
public class FlameTrap : MonoBehaviour
{
    [Header("Burn Effect")]
    [Tooltip("Damage applied each burn tick.")]
    [SerializeField] private float damagePerTick  = 5f;
    [Tooltip("Seconds between each damage tick.  Should be >= PlayerHealth.iFramesDuration.")]
    [SerializeField] private float tickInterval   = 0.6f;
    [Tooltip("Total seconds the player burns after stepping on the trap.")]
    [SerializeField] private float burnDuration   = 3f;
    [Tooltip("How long before this trap can burn the same player again.")]
    [SerializeField] private float triggerCooldown = 4f;

    [Header("Flame Animation")]
    [SerializeField] public TrapSpriteAnimator spriteAnimator;

    private float cooldownTimer;
    private Coroutine burnRoutine;

    void Start()
    {
        // Fire is always burning — start the animation immediately.
        if (spriteAnimator != null)
            spriteAnimator.SetActive(true);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (cooldownTimer > 0f) return;

        PlayerHealth health = other.GetComponent<PlayerHealth>();
        if (health == null) return;

        if (burnRoutine != null) StopCoroutine(burnRoutine);
        burnRoutine = StartCoroutine(BurnPlayer(health));
        cooldownTimer = triggerCooldown;
    }

    private IEnumerator BurnPlayer(PlayerHealth health)
    {
        float elapsed = 0f;
        while (elapsed < burnDuration)
        {
            if (health == null) yield break;
            health.TakeDamage(damagePerTick);
            elapsed += tickInterval;
            yield return new WaitForSeconds(tickInterval);
        }
    }

    void Update()
    {
        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;
    }
}
