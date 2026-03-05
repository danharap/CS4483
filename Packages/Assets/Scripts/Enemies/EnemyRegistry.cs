using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Static registry of all live enemies. Used by PlayerWeapon for nearest-target lookup.
/// No MonoBehaviour overhead – enemies call Register/Unregister themselves.
/// </summary>
public static class EnemyRegistry
{
    private static readonly List<EnemyBase> active = new List<EnemyBase>();

    public static void Register(EnemyBase e)   { if (!active.Contains(e)) active.Add(e); }
    public static void Unregister(EnemyBase e) { active.Remove(e); }
    public static int  Count => active.Count;

    /// <summary>Returns the nearest alive enemy to <paramref name="pos"/>, or null if none.</summary>
    public static EnemyBase GetNearest(Vector3 pos)
    {
        EnemyBase nearest = null;
        float bestDist = float.MaxValue;
        foreach (EnemyBase e in active)
        {
            if (e == null || !e.IsAlive) continue;
            float d = (e.transform.position - pos).sqrMagnitude;
            if (d < bestDist) { bestDist = d; nearest = e; }
        }
        return nearest;
    }

    public static void Clear() { active.Clear(); }
}
