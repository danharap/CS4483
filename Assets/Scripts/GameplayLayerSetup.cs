using UnityEngine;

/// <summary>
/// Configures physics layer ignores at runtime so:
/// - Boss vs Enemy: enemies do not physically interact with the boss volume (no piling / blocking).
/// - BossProjectile vs ArenaProp: boss bullets pass through cover crates / props.
/// - BossProjectile vs Boss: boss bullets do not interact with Satan's own colliders.
/// - BossProjectile vs Enemy: bullets pass through regular enemies (optional; avoids accidental blocks).
///
/// Layers must exist in Edit → Project Settings → Tags and Layers (see TagManager.asset):
/// Boss, Enemy, ArenaProp, BossProjectile
/// </summary>
static class GameplayLayerSetup
{
    /// <summary>Existing scenes built before ArenaProp layer: retag cover boxes under Rock_Obstacles.</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void MigrateRockObstacleLayers()
    {
        int propLayer = LayerMask.NameToLayer("ArenaProp");
        if (propLayer < 0) return;

        GameObject rockRoot = GameObject.Find("Rock_Obstacles");
        if (rockRoot == null) return;

        foreach (Transform child in rockRoot.transform)
        {
            if (child == null) continue;
            if (child.gameObject.layer == propLayer) continue;
            child.gameObject.layer = propLayer;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void ConfigureLayerCollisions()
    {
        int boss = LayerMask.NameToLayer("Boss");
        int enemy = LayerMask.NameToLayer("Enemy");
        int arenaProp = LayerMask.NameToLayer("ArenaProp");
        int bossProjectile = LayerMask.NameToLayer("BossProjectile");

        if (boss >= 0 && enemy >= 0)
            Physics.IgnoreLayerCollision(boss, enemy, true);

        if (bossProjectile >= 0 && arenaProp >= 0)
            Physics.IgnoreLayerCollision(bossProjectile, arenaProp, true);

        if (bossProjectile >= 0 && boss >= 0)
            Physics.IgnoreLayerCollision(bossProjectile, boss, true);

        if (bossProjectile >= 0 && enemy >= 0)
            Physics.IgnoreLayerCollision(bossProjectile, enemy, true);
    }
}
