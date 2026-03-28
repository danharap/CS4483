using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Player projectile. Travels in a straight line in the direction set at spawn (no homing, no stop at mouse).
/// Damages enemies, supports piercing. Uses overlap check each frame so fast-moving enemies don't get tunneled through.
/// </summary>
public class Projectile : MonoBehaviour
{
    // ── Tunables ──────────────────────────────────────────────────────────
    [Header("Projectile Settings")]
    [SerializeField] private float maxTravelDistance = 30f;  // world units to travel before despawn
    [SerializeField] private float hitCheckRadius = 0.6f;     // overlap sphere radius to catch enemies (avoids tunneling)

    // ── Runtime state (set via Init) ──────────────────────────────────────
    private Vector3 direction;
    private float   speed;
    private float   damage;
    private int     pierceLeft;   // how many additional enemies to pierce through
    private float   travelled;
    private HashSet<EnemyBase>            hitEnemies   = new HashSet<EnemyBase>();
    private HashSet<SatanBossController>  hitSatans    = new HashSet<SatanBossController>();

    public void Init(Vector3 dir, float dmg, float spd, int pierce)
    {
        dir.y = 0f;                 // ensure ground-plane travel only
        direction  = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector3.forward;
        damage     = dmg;
        speed      = spd;
        pierceLeft = pierce;
    }

    void Awake()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    void Update()
    {
        // Move only by our fixed direction (ignore any physics)
        float step = speed * Time.deltaTime;
        transform.position += direction * step;
        travelled += step;
        if (travelled >= maxTravelDistance)
        {
            Destroy(gameObject);
            return;
        }

        // Per-frame overlap so we don't tunnel through fast enemies (trigger can miss between physics steps)
        CheckOverlapHit();
    }

    private void CheckOverlapHit()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, hitCheckRadius);
        foreach (Collider c in hits)
        {
            if (c.isTrigger && c.CompareTag("Player")) continue;

            // Regular enemies (EnemyBase subclasses)
            EnemyBase enemy = c.GetComponent<EnemyBase>();
            if (enemy != null && enemy.IsAlive && !hitEnemies.Contains(enemy))
            {
                hitEnemies.Add(enemy);
                enemy.TakeDamage(damage);
                if (pierceLeft <= 0) { Destroy(gameObject); return; }
                pierceLeft--;
                continue;
            }

            // Satan boss (not an EnemyBase subclass — handled separately)
            SatanBossController satan = c.GetComponent<SatanBossController>();
            if (satan != null && satan.IsAlive && !hitSatans.Contains(satan))
            {
                hitSatans.Add(satan);
                satan.TakeDamage(damage);
                if (pierceLeft <= 0) { Destroy(gameObject); return; }
                pierceLeft--;
                continue;
            }

            // Block on solid (non-trigger) obstacles
            if (!c.isTrigger && !c.CompareTag("Player"))
            {
                Destroy(gameObject);
                return;
            }
        }
    }

    /// <summary>Called from SatanBossController.OnTriggerEnter as secondary hit detection.</summary>
    public void TryHitSatan(SatanBossController satan)
    {
        if (satan == null || !satan.IsAlive || hitSatans.Contains(satan)) return;
        hitSatans.Add(satan);
        satan.TakeDamage(damage);
        if (pierceLeft <= 0) Destroy(gameObject);
        else pierceLeft--;
    }

    void OnTriggerEnter(Collider other)
    {
        // Regular enemies
        EnemyBase enemy = other.GetComponent<EnemyBase>();
        if (enemy != null)
        {
            bool didHitNewEnemy = enemy.IsAlive && !hitEnemies.Contains(enemy);
            if (didHitNewEnemy)
            {
                hitEnemies.Add(enemy);
                enemy.TakeDamage(damage);
                if (pierceLeft <= 0) Destroy(gameObject);
                else pierceLeft--;
            }
            return;
        }

        // Satan boss
        SatanBossController satan = other.GetComponent<SatanBossController>();
        if (satan != null)
        {
            TryHitSatan(satan);
            return;
        }

        if (!other.isTrigger && !other.CompareTag("Player"))
            Destroy(gameObject);
    }
}
