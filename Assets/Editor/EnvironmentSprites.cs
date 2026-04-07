using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Applies background and wall sprites to the ProBuilder level
/// Run after creating the level
/// </summary>
public static class EnvironmentSprites
{
        // Arena inner diameter = ArenaRadius*2 = 76.  Wall inner face at radius 37.75 → inner diameter 75.5.
    // Keep Arena 1 map INSIDE the walls (74) so it never clips the wall mesh (kills z-fighting tears).
    public const float FloorMapDesiredWorldSize_Arena1 = 74f;
    // Arena 2: player reported sprite is slightly too small — bump up so it fills the full interior.
    public const float FloorMapDesiredWorldSize_Arena2 = 92f;
    // Legacy alias kept for any external callers.
    public const float FloorMapDesiredWorldSize = FloorMapDesiredWorldSize_Arena1;

    /// <summary>Finds a root GameObject by name including inactive objects.</summary>
    static GameObject FindRootByName(string name)
    {
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
            if (root.name == name) return root;
        return null;
    }
    [MenuItem("CS4483/🎨 4. Apply Environment Sprites")]
    public static void ApplyEnvironmentSprites()
    {
        Debug.Log("[EnvironmentSprites] Applying floor sprites...");

        // Remove any stale Background_Plane objects left by previous setup runs.
        // The background sprite (sBg.png) was causing its tiled texture to appear on wall
        // and stair surfaces via shadow projection. The Floor_Map sprite is sufficient.
        RemoveBackgroundPlanes();

        Sprite mapSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/sMap.png");
        if (mapSprite == null)
        {
            Debug.LogError("[EnvironmentSprites] sMap.png not found! Run '🎨 1. Slice Sprite Sheets' first.");
            return;
        }

        // Apply floor map (playable area)
        ApplyFloorMap(mapSprite);

        // Re-enable ProBuilder walls (3D walls)
        EnableProBuilderWalls();

        // Arena 2: same footprint/scale as Arena 1, nether visuals (sMap2)
        ApplyArena2EnvironmentSprites();

        Debug.Log("[EnvironmentSprites] ✓ Environment sprites applied (Floor_Map only, no background plane).");
    }

    /// <summary>Stage 2 floor: same layout as Stage 1; only the floor sprite differs.</summary>
    public static void ApplyArena2EnvironmentSprites()
    {
        Sprite mapSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/sMap2.png");
        if (mapSprite == null)
            mapSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/sMap_Arena2_Red.png");

        if (mapSprite == null)
        {
            Debug.LogWarning("[EnvironmentSprites] Arena 2 floor sprite (sMap2.png) missing — skip Arena 2 env.");
            return;
        }

        GameObject levelRoot = FindRootByName("=== LEVEL (ProBuilder) Arena2 ===");
        if (levelRoot == null)
        {
            Debug.LogWarning("[EnvironmentSprites] Arena 2 level root not found — run Build Level first.");
            return;
        }

        Transform floor = levelRoot.transform.Find("Floor");
        if (floor != null)
        {
            MeshRenderer floorRenderer = floor.GetComponent<MeshRenderer>();
            if (floorRenderer != null) floorRenderer.enabled = false;
        }

        // No Background_Plane for Arena 2 either — Floor_Map is sufficient.
        ApplyFloorMapForRoot(levelRoot.transform, mapSprite, FloorMapDesiredWorldSize_Arena2);
        EnableProBuilderWallsForRoot(levelRoot.transform);

        Debug.Log("[EnvironmentSprites] ✓ Arena 2 floor applied (enlarged to fill arena, no background plane).");
    }
    
    /// <summary>
    /// Removes any Background_Plane GameObjects from both arena roots and the lobby.
    /// These were causing sBg.png to project its tiled texture onto wall/stair surfaces
    /// via shadow casting. The Floor_Map sprite is sufficient for the ground visual.
    /// </summary>
    static void RemoveBackgroundPlanes()
    {
        string[] roots = new[]
        {
            "=== LEVEL (ProBuilder) ===",
            "=== LEVEL (ProBuilder) Arena2 ===",
            "=== LEVEL (Lobby) ===",
        };
        int removed = 0;
        foreach (string rootName in roots)
        {
            GameObject root = FindRootByName(rootName);
            if (root == null) continue;
            Transform bg = root.transform.Find("Background_Plane");
            if (bg != null)
            {
                Object.DestroyImmediate(bg.gameObject);
                removed++;
            }
        }
        if (removed > 0)
            Debug.Log($"[EnvironmentSprites] Removed {removed} Background_Plane object(s) from scene roots.");
    }

    static void ApplyFloorMap(Sprite mapSprite)
    {
        GameObject levelRoot = FindRootByName("=== LEVEL (ProBuilder) ===");
        if (levelRoot == null)
        {
            Debug.LogWarning("[EnvironmentSprites] Arena 1 root not found for floor map.");
            return;
        }

        // Hide the ProBuilder floor mesh so the Floor_Map sprite is what the player sees.
        // (Stage 2 does the same — without this the opaque mesh renders on top of the sprite.)
        Transform floorMesh = levelRoot.transform.Find("Floor");
        if (floorMesh != null)
        {
            MeshRenderer mr = floorMesh.GetComponent<MeshRenderer>();
            if (mr != null) mr.enabled = false;
        }

        ApplyFloorMapForRoot(levelRoot.transform, mapSprite, FloorMapDesiredWorldSize_Arena1);
        Debug.Log("[EnvironmentSprites] ✓ Arena 1 floor applied (sMap.png, ProBuilder mesh hidden).");
    }

    /// <summary>
    /// Places / updates the Floor_Map sprite plane.
    /// <paramref name="desiredWorldSize"/> controls the sprite's rendered world diameter.
    /// Keep this value ≤ 75 for Arena 1 so the sprite never clips the boundary walls
    /// (wall inner face sits at radius 37.75 → diameter 75.5).
    /// </summary>
    public static void ApplyFloorMapForRoot(Transform levelRoot, Sprite mapSprite,
                                             float desiredWorldSize = FloorMapDesiredWorldSize_Arena1)
    {
        // Destroy every existing Floor_Map in the entire subtree so stale duplicates never accumulate.
        // (Transform.Find only checks direct children, so deep leftovers from old setup runs stack up.)
        foreach (Transform t in levelRoot.GetComponentsInChildren<Transform>(true))
        {
            if (t != null && t.name == "Floor_Map")
                Object.DestroyImmediate(t.gameObject);
        }

        GameObject mapPlane = new GameObject("Floor_Map");
        mapPlane.transform.SetParent(levelRoot, false);
        mapPlane.transform.rotation    = Quaternion.Euler(90f, 0f, 0f);
        mapPlane.transform.localScale  = Vector3.one;

        // Y = 0.05 keeps the sprite below the wall-base height (wall bottom at y = 0).
        mapPlane.transform.position = new Vector3(0f, 0.05f, 0f);

        SpriteRenderer sr = mapPlane.GetComponent<SpriteRenderer>();
        if (sr == null) sr = mapPlane.AddComponent<SpriteRenderer>();
        sr.sprite = mapSprite;
        sr.sortingOrder = -50;
        sr.drawMode = SpriteDrawMode.Simple;

        float spriteWorldSize = mapSprite.bounds.size.x;
        if (spriteWorldSize > 0.001f)
        {
            float scale = desiredWorldSize / spriteWorldSize;
            mapPlane.transform.localScale = new Vector3(scale, scale, 1f);
        }
    }
    
    static void EnableProBuilderWalls()
    {
        GameObject levelRoot = FindRootByName("=== LEVEL (ProBuilder) ===");
        if (levelRoot == null)
        {
            Debug.LogWarning("[EnvironmentSprites] Level root not found!");
            return;
        }
        EnableProBuilderWallsForRoot(levelRoot.transform);
    }

    static void EnableProBuilderWallsForRoot(Transform levelRoot)
    {
        Transform wallsParent = levelRoot.Find("Boundary_Walls");
        if (wallsParent == null)
        {
            Debug.LogWarning("[EnvironmentSprites] Boundary_Walls not found under " + levelRoot.name + "!");
            return;
        }

        int wallCount = 0;
        foreach (Transform wall in wallsParent)
        {
            MeshRenderer meshRenderer = wall.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
                meshRenderer.enabled = true;

            Transform oldSprite = wall.Find("Wall_Sprite");
            if (oldSprite != null)
                Object.DestroyImmediate(oldSprite.gameObject);

            wallCount++;
        }

        Debug.Log($"[EnvironmentSprites] ✓ Enabled {wallCount} 3D ProBuilder walls on {levelRoot.name}");
    }
}
