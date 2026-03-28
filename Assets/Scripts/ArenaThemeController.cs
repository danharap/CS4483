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
    /// <summary>Must match <c>EnvironmentSprites.FloorMapDesiredWorldSize</c> (editor).</summary>
    public const float FloorMapDesiredWorldSize = 86f;

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
        ApplyFloorAndBg(
            "=== LEVEL (ProBuilder) ===",
            arena1FloorSprite,
            arena1BgSprite);
        ApplyTrapFrames(false);
        Debug.Log("[ArenaThemeController] Arena 1 theme applied.");
    }

    public void ApplyArena2Theme()
    {
        // Explicitly disable Arena 1's floor renderers in case they are stray (at scene root).
        DisableArena1FloorRenderers();

        ApplyFloorAndBg(
            "=== LEVEL (ProBuilder) Arena2 ===",
            arena2FloorSprite,
            arena2BgSprite);
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

    void ApplyFloorAndBg(string arenaRootName, Sprite floorSprite, Sprite bgSprite)
    {
        bool isArena2 = arenaRootName.IndexOf("Arena2", StringComparison.Ordinal) >= 0;
        Transform arenaRoot = FindSceneRootTransform(arenaRootName);

        if (isArena2 && arenaRoot != null)
        {
            // Match editor pipeline: hide ProBuilder floor so the sprite map is visible (BuildArena2 has no Floor_Map until EnvironmentSprites runs).
            Transform floorMesh = arenaRoot.Find("Floor");
            if (floorMesh != null)
            {
                MeshRenderer mr = floorMesh.GetComponent<MeshRenderer>();
                if (mr != null) mr.enabled = false;
            }
        }

        Sprite s = floorSprite;
        if (s == null && isArena2)
            s = Resources.Load<Sprite>("ArenaTheme/sMap2");

        Transform floorT = FindDeepChildInSceneRoot(arenaRootName, "Floor_Map");
        if (floorT == null && isArena2 && arenaRoot != null && s != null)
        {
            GameObject mapPlane = new GameObject("Floor_Map");
            mapPlane.transform.SetParent(arenaRoot, false);
            mapPlane.transform.position = new Vector3(0f, 0.1f, 0f);
            mapPlane.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            mapPlane.transform.localScale = Vector3.one;
            SpriteRenderer nsr = mapPlane.AddComponent<SpriteRenderer>();
            nsr.sortingOrder = -50;
            nsr.drawMode = SpriteDrawMode.Simple;
            floorT = mapPlane.transform;
        }

        if (floorT != null)
        {
            SpriteRenderer sr = floorT.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                if (s != null)
                {
                    sr.sprite  = s;
                    sr.enabled = true; // re-enable in case suppressed by stray-floor pass
                    ApplyFloorMapScale(floorT, s);
                    Debug.Log($"[ArenaThemeController] Floor_Map sprite set to '{s.name}' on {arenaRootName}.");
                }
                else
                {
                    // No sprite available — ensure the renderer stays enabled so the
                    // ProBuilder floor mesh below is at least visible instead of nothing.
                    sr.enabled = false;
                    if (isArena2)
                        Debug.LogError("[ArenaThemeController] Stage 2 floor sprite missing. " +
                            "Assign arena2FloorSprite on ArenaThemeController, run CS4483 → Apply Sprites, " +
                            "or ensure Resources/ArenaTheme/sMap2.png is imported as a Sprite.");
                }
            }
        }
        else if (isArena2)
            Debug.LogError("[ArenaThemeController] Stage 2: Floor_Map not found inside " +
                "=== LEVEL (ProBuilder) Arena2 ===. Run CS4483 → Apply Environment Sprites, " +
                "or ensure the arena2FloorSprite / Resources/ArenaTheme/sMap2 is set up.");

        Transform bgT = FindDeepChildInSceneRoot(arenaRootName, "Background_Plane");
        if (bgT != null && bgSprite != null)
        {
            SpriteRenderer sr = bgT.GetComponent<SpriteRenderer>();
            if (sr != null) sr.sprite = bgSprite;
        }
    }

    static void ApplyFloorMapScale(Transform mapPlane, Sprite sprite)
    {
        if (mapPlane == null || sprite == null) return;
        float w = sprite.bounds.size.x;
        if (w > 0.001f)
        {
            float s = FloorMapDesiredWorldSize / w;
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
