using UnityEngine;

/// <summary>
/// Heavy/tank enemy: slower but much tankier than the basic chaser.
/// Uses the same core behaviour as EnemyBase (chase player, contact damage, XP/health drops).
/// </summary>
public class HeavyEnemy : EnemyBase
{
    private const float FallenBaseContactDamage = 10f;
    private const float TankDamageMultiplier = 1.26f;

    protected override void Awake()
    {
        base.Awake();

        // Defaults if not overridden in prefab
        if (maxHP <= 0f)     maxHP     = 180f; // ~3x basic chaser (60)
        if (moveSpeed <= 0f) moveSpeed = 2.0f; // Slower than chaser (3.5)
        if (xpDrop <= 0f)    xpDrop    = 18f;  // Slightly more reward
        ContactDamage = FallenBaseContactDamage * TankDamageMultiplier;
    }
}

