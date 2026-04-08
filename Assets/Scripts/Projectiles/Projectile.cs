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
        // The projectile moves via transform.position in Update, not physics.
        // Kinematic bodies reject velocity/angularVelocity assignment, so we just
        // ensure the body is kinematic and leave the velocity fields alone.
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
            rb.isKinematic = true;
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

        // Pass 1 — enemies / Satan only. Must run before environment so floor/walls in the
        // same overlap sphere cannot destroy the projectile before a second pierce target is processed.
        foreach (Collider c in hits)
        {
            if (c.isTrigger && c.CompareTag("Player")) continue;

            EnemyBase enemy = c.GetComponent<EnemyBase>() ?? c.GetComponentInParent<EnemyBase>();
            if (enemy != null && enemy.IsAlive && !hitEnemies.Contains(enemy))
            {
                hitEnemies.Add(enemy);
                enemy.TakeDamage(damage);
                if (pierceLeft <= 0) { Destroy(gameObject); return; }
                pierceLeft--;
                continue;
            }

            SatanBossController satan = c.GetComponent<SatanBossController>() ?? c.GetComponentInParent<SatanBossController>();
            if (satan != null && satan.IsAlive && !hitSatans.Contains(satan))
            {
                hitSatans.Add(satan);
                satan.TakeDamage(damage);
                if (pierceLeft <= 0) { Destroy(gameObject); return; }
                pierceLeft--;
                continue;
            }
        }

        // Pass 2 — solid obstacles (ignore colliders that belong to an enemy / boss)
        foreach (Collider c in hits)
        {
            if (c.isTrigger || c.CompareTag("Player")) continue;
            if (c.GetComponentInParent<EnemyBase>() != null) continue;
            if (c.GetComponentInParent<SatanBossController>() != null) continue;
            // Arena / lobby floor slabs overlap the projectile's XZ path — do not treat as a wall stop,
            // or pierce (and sometimes basic shots) die on the floor before reaching the next target.
            if (IsArenaFloorLikeCollider(c)) continue;

            Destroy(gameObject);
            return;
        }
    }

    /// <summary>True for large thin horizontal colliders (ProBuilder "Floor", lobby ground, etc.).</summary>
    private static bool IsArenaFloorLikeCollider(Collider c)
    {
        if (c == null) return false;
        if (c.name.IndexOf("Floor", System.StringComparison.OrdinalIgnoreCase) >= 0) return true;
        Vector3 e = c.bounds.extents;
        return e.y < 1.25f && e.x > 4f && e.z > 4f;
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

    /// <summary>
    /// The raw damage value this projectile carries. Read by SatanFootController to
    /// route foot damage to the Phase 2 HP pool.
    /// </summary>
    public float DamageAmount => damage;

    /// <summary>
    /// Called by SatanFootController when a grounded foot registers a hit.
    /// Handles pierce decrement and self-destruction so the caller does not need
    /// to destroy the projectile directly.
    /// </summary>
    public void NotifyHit()
    {
        if (pierceLeft <= 0) Destroy(gameObject);
        else pierceLeft--;
    }

    void OnTriggerEnter(Collider other)
    {
        // Regular enemies
        EnemyBase enemy = other.GetComponent<EnemyBase>() ?? other.GetComponentInParent<EnemyBase>();
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
        SatanBossController satan = other.GetComponent<SatanBossController>() ?? other.GetComponentInParent<SatanBossController>();
        if (satan != null)
        {
            TryHitSatan(satan);
            return;
        }

        if (!other.isTrigger && !other.CompareTag("Player"))
        {
            if (other.GetComponentInParent<EnemyBase>() != null) return;
            if (other.GetComponentInParent<SatanBossController>() != null) return;
            if (IsArenaFloorLikeCollider(other)) return;
            Destroy(gameObject);
        }
    }
}
