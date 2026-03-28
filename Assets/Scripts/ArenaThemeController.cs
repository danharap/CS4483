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
    }

    public void ApplyArena2Theme()
    {
        ApplyFloorAndBg(
            "=== LEVEL (ProBuilder) Arena2 ===",
            arena2FloorSprite,
            arena2BgSprite);
        ApplyTrapFrames(true);
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

    void ApplyFloorAndBg(string arenaRootName, Sprite floorSprite, Sprite bgSprite)
    {
        Transform floorT = FindDeepChildInSceneRoot(arenaRootName, "Floor_Map");
        if (floorT != null)
        {
            SpriteRenderer sr = floorT.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                Sprite s = floorSprite;
                if (s == null && arenaRootName.IndexOf("Arena2", StringComparison.Ordinal) >= 0)
                    s = Resources.Load<Sprite>("ArenaTheme/sMap2");
                if (s != null)
                {
                    sr.sprite = s;
                    ApplyFloorMapScale(floorT, s);
                }
                else if (arenaRootName.IndexOf("Arena2", StringComparison.Ordinal) >= 0)
                    Debug.LogError("[ArenaThemeController] Stage 2 floor sprite missing. Assign arena2FloorSprite on ArenaThemeController (=== MANAGERS ===), run CS4483 → 🎨 2. Apply Sprites to Prefabs, or add Resources/ArenaTheme/sMap2.");
            }
        }

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
