using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// Runtime theme switcher between Arena 1 and Arena 2.
/// Swaps the floor sprite and zap-trap animation frames when transitioning through the portal.
/// Floor scale is driven by desired world size so sMap vs sMap2 PPU differences never stretch the floor.
/// Backdrop uses <c>sBg.png</c> for both arenas (large sprite under the arena); falls back to solid black if unset.
/// </summary>
public class ArenaThemeController : MonoBehaviour
{
    /// <summary>Must match <c>EnvironmentSprites.FloorMapDesiredWorldSize_Arena1</c> (editor).</summary>
    public const float FloorMapDesiredWorldSize_Arena1 = 92f;
    /// <summary>Must match <c>EnvironmentSprites.FloorMapDesiredWorldSize_Arena2</c> (editor).</summary>
    public const float FloorMapDesiredWorldSize_Arena2 = 92f;
    /// <summary>
    /// World width/height for the backdrop sprite (uniform scale). Slightly above arena floor (~92) so it still
    /// fills the view, but lower than the old 128 — packs more texels into the visible area (less blocky sBg).
    /// </summary>
    public const float BackdropDesiredWorldSize = 96f;

    [Header("Floor")]
    public Sprite arena1FloorSprite;
    public Sprite arena2FloorSprite;

    [Header("Backdrop (sBg — both arenas)")]
    public Sprite arena1BackdropSprite;

    [Header("Zap Trap Animations")]
    public Sprite[] zapTrapBlueFrames;
    public Sprite[] zapTrapRedFrames;

    void Start()
    {
        ApplyArena1Theme();
    }

    public void ApplyArena1Theme()
    {
        ApplyFloorAndBackdrop("=== LEVEL (ProBuilder) ===", arena1FloorSprite, FloorMapDesiredWorldSize_Arena1);
        ApplyTrapFrames(false);
        Debug.Log("[ArenaThemeController] Arena 1 theme applied.");
    }

    public void ApplyArena2Theme()
    {
        DisableArena1FloorRenderers();
        ApplyFloorAndBackdrop("=== LEVEL (ProBuilder) Arena2 ===", arena2FloorSprite, FloorMapDesiredWorldSize_Arena2);
        ApplyTrapFrames(true);
        Debug.Log("[ArenaThemeController] Arena 2 theme applied.");
    }

    /// <summary>
    /// Belt-and-suspenders: disable Arena 1 floor/backdrop renderers so they cannot bleed through Arena 2.
    /// </summary>
    void DisableArena1FloorRenderers()
    {
        Transform a1 = FindSceneRootTransform("=== LEVEL (ProBuilder) ===");
        if (a1 == null) return;

        foreach (Transform t in a1.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == "Floor_Map" || t.name == "Background_Plane")
            {
                SpriteRenderer sr = t.GetComponent<SpriteRenderer>();
                if (sr != null) sr.enabled = false;
            }
            else if (t.name == "Background_Black")
            {
                MeshRenderer mr = t.GetComponent<MeshRenderer>();
                if (mr != null) mr.enabled = false;
            }
        }
    }

    static Transform FindSceneRootTransform(string rootName)
    {
        foreach (GameObject r in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (r.name == rootName) return r.transform;
        }
        return null;
    }

    void ApplyFloorAndBackdrop(string arenaRootName, Sprite floorSprite, float desiredWorldSize)
    {
        Transform arenaRoot = FindSceneRootTransform(arenaRootName);
        if (arenaRoot == null)
        {
            Debug.LogWarning($"[ArenaThemeController] Arena root '{arenaRootName}' not found — skipping floor.");
            return;
        }

        // Hide every ProBuilder floor mesh in the arena so only the Floor_Map sprite shows.
        foreach (Transform t in arenaRoot.GetComponentsInChildren<Transform>(true))
        {
            if (t.name != "Floor") continue;
            MeshRenderer mr = t.GetComponent<MeshRenderer>();
            if (mr != null) mr.enabled = false;
        }

        bool isArena2 = arenaRootName.IndexOf("Arena2", StringComparison.Ordinal) >= 0;
        Sprite s = floorSprite;
        if (s == null)
        {
            string fallbackPath = isArena2 ? "Assets/Sprites/sMap2.png" : "Assets/Sprites/sMap.png";
#if UNITY_EDITOR
            s = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(fallbackPath);
#endif
        }

        // ── GLOBAL Floor_Map cleanup ──────────────────────────────────────
        // Search the ENTIRE scene (all roots, including inactive) to catch orphans
        // that might have ended up at scene-root level from earlier setup runs.
        NukeAllFloorMaps();

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

        float boundsW = s.bounds.size.x;
        if (boundsW > 0.001f)
        {
            float scale = desiredWorldSize / boundsW;
            mapPlane.transform.localScale = new Vector3(scale, scale, 1f);
            Debug.Log($"[ArenaThemeController] Floor_Map '{arenaRootName}' → sprite='{s.name}' " +
                      $"tex={s.texture.width}x{s.texture.height} PPU={s.pixelsPerUnit} " +
                      $"bounds.x={boundsW:F2} desiredSize={desiredWorldSize} scale={scale:F4}");
        }

        Sprite backdrop = arena1BackdropSprite;
        if (backdrop == null)
        {
#if UNITY_EDITOR
            backdrop = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/sBg.png");
#endif
        }

        EnsureArenaBackdrop(arenaRoot, backdrop);
    }

    /// <summary>
    /// Large horizontal backdrop under the arena (<see cref="Background_Plane"/>).
    /// Uses <paramref name="backdropSprite"/> when set; otherwise a solid black quad (<c>Background_Black</c>).
    /// Safe to call from editor tools — pass sprites loaded via <c>AssetDatabase</c>.
    /// </summary>
    public static void EnsureArenaBackdrop(Transform arenaRoot, Sprite backdropSprite)
    {
        if (arenaRoot == null) return;

        var remove = new List<GameObject>();
        foreach (Transform t in arenaRoot.GetComponentsInChildren<Transform>(true))
        {
            if (t == null) continue;
            if (t.name == "Background_Black" || t.name == "Background_Plane")
                remove.Add(t.gameObject);
        }
        foreach (GameObject go in remove)
            DestroyBackdropObject(go);

        if (backdropSprite != null)
        {
            GameObject bgGo = new GameObject("Background_Plane");
            bgGo.transform.SetParent(arenaRoot, false);
            bgGo.transform.position   = new Vector3(0f, -0.55f, 0f);
            bgGo.transform.rotation   = Quaternion.Euler(90f, 0f, 0f);
            bgGo.transform.localScale = Vector3.one;
            SpriteRenderer sr = bgGo.AddComponent<SpriteRenderer>();
            sr.sprite       = backdropSprite;
            sr.sortingOrder = -100;
            sr.drawMode     = SpriteDrawMode.Simple;

            float w = backdropSprite.bounds.size.x;
            if (w > 0.001f)
            {
                float sc = BackdropDesiredWorldSize / w;
                bgGo.transform.localScale = new Vector3(sc, sc, 1f);
            }
            return;
        }

        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.name = "Background_Black";
        quad.transform.SetParent(arenaRoot, false);
        quad.transform.position   = new Vector3(0f, -0.6f, 0f);
        quad.transform.rotation   = Quaternion.Euler(90f, 0f, 0f);
        quad.transform.localScale = new Vector3(480f, 480f, 1f);

        UnityEngine.Object.Destroy(quad.GetComponent<Collider>());

        MeshRenderer mr = quad.GetComponent<MeshRenderer>();
        mr.sharedMaterial = CreateBlackBackdropMaterial();
        mr.shadowCastingMode = ShadowCastingMode.Off;
        mr.receiveShadows    = false;
    }

    static void DestroyBackdropObject(GameObject go)
    {
        if (go == null) return;
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            UnityEngine.Object.DestroyImmediate(go);
            return;
        }
#endif
        UnityEngine.Object.Destroy(go);
    }

    static Material CreateBlackBackdropMaterial()
    {
        Shader sh = Shader.Find("Universal Render Pipeline/Unlit");
        if (sh == null) sh = Shader.Find("Universal Render Pipeline/Lit");
        if (sh == null) sh = Shader.Find("Unlit/Color");
        if (sh == null) sh = Shader.Find("Sprites/Default");
        if (sh == null) sh = Shader.Find("Standard");

        Material mat = new Material(sh);
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", Color.black);
        else if (mat.HasProperty("_Color"))
            mat.SetColor("_Color", Color.black);
        else
            mat.color = Color.black;

        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0f);
        if (mat.HasProperty("_Metallic"))   mat.SetFloat("_Metallic", 0f);
        return mat;
    }

    /// <summary>
    /// Destroy EVERY GameObject named "Floor_Map" in the entire scene — regardless of hierarchy.
    /// Immediately disables their SpriteRenderers so they can't render even during the deferred
    /// Destroy frame.
    /// </summary>
    static void NukeAllFloorMaps()
    {
        int count = 0;
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t == null || t.name != "Floor_Map") continue;

                // Immediately disable the renderer so it can't show during this frame.
                SpriteRenderer sr = t.GetComponent<SpriteRenderer>();
                if (sr != null) sr.enabled = false;

                Destroy(t.gameObject);
                count++;
            }
        }
        if (count > 0)
            Debug.Log($"[ArenaThemeController] NukeAllFloorMaps: destroyed {count} stale Floor_Map object(s).");
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
