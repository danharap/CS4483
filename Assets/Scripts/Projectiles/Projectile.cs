using UnityEngine;

/// <summary>
/// Player projectile. Travels in a straight line, damages enemies, supports piercing.
/// Auto-destroys after maxLifetime.
/// </summary>
public class Projectile : MonoBehaviour
{
    // ── Tunables ──────────────────────────────────────────────────────────
    [Header("Projectile Settings")]
    [SerializeField] private float maxLifetime = 3f;

    // ── Runtime state (set via Init) ──────────────────────────────────────
    private Vector3 direction;
    private float   speed;
    private float   damage;
    private int     pierceLeft;  // how many additional enemies to pierce through
    private float   lifetime;

    public void Init(Vector3 dir, float dmg, float spd, int pierce)
    {
        direction  = dir.normalized;
        damage     = dmg;
        speed      = spd;
        pierceLeft = pierce;
    }

    void Update()
    {
        transform.position += direction * speed * Time.deltaTime;
        lifetime += Time.deltaTime;
        if (lifetime >= maxLifetime)
            Destroy(gameObject);
    }

    void OnTriggerEnter(Collider other)
    {
        EnemyBase enemy = other.GetComponent<EnemyBase>();
        if (enemy == null) return;
        if (!enemy.IsAlive) return;

        enemy.TakeDamage(damage);

        if (pierceLeft <= 0)
            Destroy(gameObject);
        else
            pierceLeft--;
    }
}
