using UnityEngine;

/// <summary>
/// Player projectile. Travels in a straight line in the direction set at spawn (no homing, no stop at mouse).
/// Damages enemies, supports piercing. Auto-destroys after maxLifetime.
/// </summary>
public class Projectile : MonoBehaviour
{
    // ── Tunables ──────────────────────────────────────────────────────────
    [Header("Projectile Settings")]
    [SerializeField] private float maxTravelDistance = 30f;  // world units to travel before despawn

    // ── Runtime state (set via Init) ──────────────────────────────────────
    private Vector3 direction;
    private float   speed;
    private float   damage;
    private int     pierceLeft;   // how many additional enemies to pierce through
    private float   travelled;

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
            Destroy(gameObject);
    }

    void OnTriggerEnter(Collider other)
    {
        EnemyBase enemy = other.GetComponent<EnemyBase>();
        if (enemy != null)
        {
            if (enemy.IsAlive)
                enemy.TakeDamage(damage);
            if (pierceLeft <= 0)
                Destroy(gameObject);
            else
                pierceLeft--;
            return;
        }

        // Block on walls and obstacles (any solid non-trigger collider except player)
        if (!other.isTrigger && !other.CompareTag("Player"))
            Destroy(gameObject);
    }
}
