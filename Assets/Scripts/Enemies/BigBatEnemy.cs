using UnityEngine;

/// <summary>
/// Big bat: fast like FastEnemy but with more HP.
/// Plays a bite animation when it damages the player via contact.
/// Spawns starting after Wave 5 (configured in EnemySpawner).
/// </summary>
public class BigBatEnemy : EnemyBase
{
    [Header("Bite")]
    [SerializeField] private float biteCooldown = 1f;
    private float biteTimer;
    private BatEnemyVisualController vis;

    protected override void Awake()
    {
        base.Awake();
        if (maxHP <= 0f)     maxHP     = 65f;
        if (moveSpeed <= 0f) moveSpeed = 6.2f;
        if (xpDrop <= 0f)    xpDrop    = 14f;
        vis = GetComponent<BatEnemyVisualController>();
    }

    protected override void Update()
    {
        base.Update();
        if (!IsAlive || player == null) return;

        biteTimer -= Time.deltaTime;
        if (biteTimer > 0f) return;

        // Mirror EnemyBase contact range so bite lines up with damage tick.
        float dist = Vector3.Distance(transform.position, player.position);
        if (dist < 1.2f)
        {
            if (vis == null) vis = GetComponent<BatEnemyVisualController>();
            vis?.PlayBite();
            biteTimer = biteCooldown;
        }
    }
}

