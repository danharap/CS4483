using UnityEngine;

/// <summary>
/// Basic enemy type: moves straight toward the player, high HP, low speed.
/// Color: dark red. Available from Wave 1.
/// </summary>
public class ChaserEnemy : EnemyBase
{
    // Chaser uses all defaults from EnemyBase.
    // Override MoveTowardPlayer here if we want custom behavior (e.g. slight zigzag).
    protected override void Awake()
    {
        base.Awake();
        // Default stats set in inspector / prefab; these are sensible overrides
        // if the fields haven't been set in the prefab yet.
        if (maxHP <= 0f)      maxHP      = 60f;
        if (moveSpeed <= 0f)  moveSpeed  = 3.5f;
        if (xpDrop <= 0f)     xpDrop     = 10f;
    }
}
