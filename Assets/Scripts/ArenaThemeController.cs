using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Runtime theme switcher between Arena 1 and Arena 2.
/// Swaps the floor sprite and zap-trap animation frames when transitioning through the portal.
/// Resolves Floor_Map / Background_Plane per arena root (including inactive) and reapplies floor scale
/// so sMap vs sMap2 bounds/PPU differences never stretch the floor.
/// </summary>
public class ArenaThemeController : MonoBehaviour
{
    /// <summary>Must match <c>EnvironmentSprites.FloorMapDesiredWorldSize_Arena1</c> (editor).</summary>
    public const float FloorMapDesiredWorldSize_Arena1 = 74f;
    /// <summary>Must match <c>EnvironmentSprites.FloorMapDesiredWorldSize_Arena2</c> (editor).</summary>
    public const float FloorMapDesiredWorldSize_Arena2 = 92f;

    [Header("Floor")]
    public Sprite arena1FloorSprite;
    public Sprite arena2FloorSprite;

    [Header("Background")]
    public Sprite arena1BgSprite;
    public Sprite arena2BgSprite;

    [Header("Zap Trap Animations")]
    public Sprite[] zapTrapBlueFrames;
    public Sprite[] zapTrapRedFrames;

    void Start()
    {
        ApplyArena1Theme();
    }

    public void ApplyArena1Theme()
    {
        ApplyFloorAndBg("=== LEVEL (ProBuilder) ===",       arena1FloorSprite, arena1BgSprite, FloorMapDesiredWorldSize_Arena1);
        ApplyTrapFrames(false);
        Debug.Log("[ArenaThemeController] Arena 1 theme applied.");
    }

    public void ApplyArena2Theme()
    {
        DisableArena1FloorRenderers();
        ApplyFloorAndBg("=== LEVEL (ProBuilder) Arena2 ===", arena2FloorSprite, arena2BgSprite, FloorMapDesiredWorldSize_Arena2);
        ApplyTrapFrames(true);
        Debug.Log("[ArenaThemeController] Arena 2 theme applied.");
    }

    /// <summary>
    /// Belt-and-suspenders: disable Arena 1's Floor_Map and Background_Plane sprite renderers
    /// so they cannot bleed through Arena 2 even if they are accidentally at scene-root level.
    /// </summary>
    void DisableArena1FloorRenderers()
    {
        Transform a1 = FindSceneRootTransform("=== LEVEL (ProBuilder) ===");
        if (a1 == null) return; // already inactive or missing

        foreach (Transform t in a1.GetComponentsInChildren<Transform>(true))
        {
            if (t.name != "Floor_Map" && t.name != "Background_Plane") continue;
            SpriteRenderer sr = t.GetComponent<SpriteRenderer>();
            if (sr != null) sr.enabled = false;
        }
    }

    /// <summary>Transform.Find only sees direct children; search the full arena subtree.</summary>
    static Transform FindDeepChildInSceneRoot(string rootName, string childName)
    {
        foreach (GameObject r in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (r.name != rootName) continue;
            foreach (Transform t in r.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == childName) return t;
            }
        }
        return null;
    }

    static Transform FindSceneRootTransform(string rootName)
    {
        foreach (GameObject r in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (r.name == rootName) return r.transform;
        }
        return null;
    }

    void ApplyFloorAndBg(string arenaRootName, Sprite floorSprite, Sprite bgSprite, float desiredWorldSize)
    {
        Transform arenaRoot = FindSceneRootTransform(arenaRootName);
        if (arenaRoot == null) return;

        // Hide every ProBuilder floor mesh in the arena so only the Floor_Map sprite shows.
        foreach (Transform t in arenaRoot.GetComponentsInChildren<Transform>(true))
        {
            if (t.name != "Floor") continue;
            MeshRenderer mr = t.GetComponent<MeshRenderer>();
            if (mr != null) mr.enabled = false;
        }

        bool isArena2 = arenaRootName.IndexOf("Arena2", System.StringComparison.Ordinal) >= 0;
        Sprite s = floorSprite;
        if (s == null)
        {
            // Cold-load fallback: serialized sprite refs may not be saved yet if Setup was
            // run without the final scene save.  Load directly from the asset path instead.
            string fallbackPath = isArena2 ? "Assets/Sprites/sMap2.png" : "Assets/Sprites/sMap.png";
#if UNITY_EDITOR
            s = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(fallbackPath);
#endif
        }

        // Destroy every existing Floor_Map in the entire subtree to prevent stale duplicates.
        var toDestroy = new System.Collections.Generic.List<GameObject>();
        foreach (Transform t in arenaRoot.GetComponentsInChildren<Transform>(true))
            if (t != null && t.name == "Floor_Map") toDestroy.Add(t.gameObject);
        foreach (GameObject go in toDestroy) Destroy(go);

        if (s == null)
        {
            Debug.LogWarning($"[ArenaThemeController] No floor sprite for {arenaRootName} — ProBuilder mesh left visible.");
            return;
        }

        // Create one clean Floor_Map for this arena.
        GameObject mapPlane = new GameObject("Floor_Map");
        mapPlane.transform.SetParent(arenaRoot, false);
        mapPlane.transform.position   = new Vector3(0f, 0.05f, 0f);
        mapPlane.transform.rotation   = Quaternion.Euler(90f, 0f, 0f);
        mapPlane.transform.localScale = Vector3.one;
        SpriteRenderer sr = mapPlane.AddComponent<SpriteRenderer>();
        sr.sprite       = s;
        sr.sortingOrder = -50;
        sr.drawMode     = SpriteDrawMode.Simple;
        ApplyFloorMapScale(mapPlane.transform, s, desiredWorldSize);
        Debug.Log($"[ArenaThemeController] Floor_Map created for {arenaRootName} using '{s.name}'.");

        // Background plane (legacy; usually removed — update if still present)
        Transform bgT = FindDeepChildInSceneRoot(arenaRootName, "Background_Plane");
        if (bgT != null && bgSprite != null)
        {
            SpriteRenderer bgSr = bgT.GetComponent<SpriteRenderer>();
            if (bgSr != null) bgSr.sprite = bgSprite;
        }
    }

    static void ApplyFloorMapScale(Transform mapPlane, Sprite sprite, float desiredWorldSize)
    {
        if (mapPlane == null || sprite == null) return;
        float w = sprite.bounds.size.x;
        if (w > 0.001f)
        {
            float s = desiredWorldSize / w;
            mapPlane.localScale = new Vector3(s, s, 1f);
        }
    }

    void ApplyTrapFrames(bool arena2)
    {
        Sprite[] frames = arena2 ? zapTrapRedFrames : zapTrapBlueFrames;
        if (frames == null || frames.Length == 0) return;

        foreach (ArenaTrap trap in FindObjectsByType<ArenaTrap>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Transform visual = trap.transform.Find("TrapVisual");
            if (visual == null) continue;
            TrapSpriteAnimator anim = visual.GetComponent<TrapSpriteAnimator>();
            if (anim == null) continue;
            anim.SetFrames(frames);
        }
    }
}
