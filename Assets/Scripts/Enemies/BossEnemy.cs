using System.Collections;
using UnityEngine;

/// <summary>
/// Boss enemy: very high HP, charges at the player every few seconds.
/// Spawns every 5 waves. On death, notifies WaveManager.OnBossKilled.
/// </summary>
public class BossEnemy : EnemyBase
{
    // ── Tunables ──────────────────────────────────────────────────────────
    [Header("Boss Behaviour")]
    [SerializeField] private float chargeSpeed     = 14f;
    [SerializeField] private float chargeCooldown  = 5f;
    [SerializeField] private float chargeDuration  = 0.8f;
    [SerializeField] private float chargeWindup    = 0.5f;   // pause before charge

    // ── State ─────────────────────────────────────────────────────────────
    private float chargeTimer;
    private bool  isCharging;
    private Vector3 chargeDir;

    protected override void Awake()
    {
        base.Awake();
        if (maxHP <= 0f)     maxHP     = 500f;
        if (moveSpeed <= 0f) moveSpeed = 2.5f;
        if (xpDrop <= 0f)    xpDrop    = 100f;
        chargeTimer = chargeCooldown * 0.5f; // first charge comes sooner
    }

    protected override void Update()
    {
        if (!IsAlive || player == null) return;

        if (!isCharging)
        {
            base.Update();          // normal move + contact damage
            chargeTimer -= Time.deltaTime;
            if (chargeTimer <= 0f)
                StartCoroutine(ChargeRoutine());
        }
    }

    private IEnumerator ChargeRoutine()
    {
        isCharging = true;
        chargeTimer = chargeCooldown;

        // Wind-up: flash white and stand still
        yield return new WaitForSeconds(chargeWindup);

        if (!IsAlive) { isCharging = false; yield break; }

        chargeDir = (player.position - transform.position);
        chargeDir.y = 0f;
        chargeDir.Normalize();

        float elapsed = 0f;
        while (elapsed < chargeDuration)
        {
            transform.position += chargeDir * chargeSpeed * Time.deltaTime;
            elapsed += Time.deltaTime;
            yield return null;
        }

        isCharging = false;
    }

    protected override void Die()
    {
        base.Die(); // handles XP drop, registry, destroy

        // Signal the WaveManager that the boss is dead
        GameManager.Instance?.WaveManager?.NotifyBossKilled();
    }
}
