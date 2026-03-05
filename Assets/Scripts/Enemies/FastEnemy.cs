using UnityEngine;

/// <summary>
/// Fast enemy: lower HP but moves quickly. Unlocked from Wave 3.
/// Color: orange/yellow.
/// </summary>
public class FastEnemy : EnemyBase
{
    protected override void Awake()
    {
        base.Awake();
        if (maxHP <= 0f)     maxHP     = 30f;
        if (moveSpeed <= 0f) moveSpeed = 6f;
        if (xpDrop <= 0f)    xpDrop    = 8f;
    }
}
