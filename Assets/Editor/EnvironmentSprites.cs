using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Applies background and wall sprites to the ProBuilder level
/// Run after creating the level
/// </summary>
public static class EnvironmentSprites
{
    /// <summary>World width/depth for the floor sprite (matches ProBuilder arena interior; see ApplyFloorMap).</summary>
    public const float FloorMapDesiredWorldSize = 86f;

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
        Debug.Log("[EnvironmentSprites] Applying background and floor sprites...");
        
        // Load sprites
        Sprite bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/sBg.png");
        Sprite mapSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/sMap.png");
        
        if (bgSprite == null || mapSprite == null)
        {
            Debug.LogError("[EnvironmentSprites] Sprites not found! Run '🎨 1. Slice Sprite Sheets' first.");
            return;
        }
        
        // Apply background (outer layer)
        ApplyBackground(bgSprite);
        
        // Apply floor map (playable area)
        ApplyFloorMap(mapSprite);
        
        // Re-enable ProBuilder walls (3D green walls)
        EnableProBuilderWalls();
        
        // Arena 2: same footprint/scale as Arena 1, nether visuals (sMap2 / sBg_Red)
        ApplyArena2EnvironmentSprites();

        Debug.Log("[EnvironmentSprites] ✓ Environment sprites applied! Using 3D green ProBuilder walls.");
    }

    /// <summary>Stage 2 floor/background: identical layout and scale to Stage 1; only sprites differ.</summary>
    public static void ApplyArena2EnvironmentSprites()
    {
        Sprite bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/sBg_Red.png");
        Sprite mapSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/sMap2.png");
        if (mapSprite == null)
            mapSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/sMap_Arena2_Red.png");

        if (bgSprite == null || mapSprite == null)
        {
            Debug.LogWarning("[EnvironmentSprites] Arena 2 sprites (sBg_Red / sMap2) missing — skip Arena 2 env.");
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

        ApplyBackgroundForRoot(levelRoot.transform, bgSprite);
        ApplyFloorMapForRoot(levelRoot.transform, mapSprite);
        EnableProBuilderWallsForRoot(levelRoot.transform);

        Debug.Log("[EnvironmentSprites] ✓ Arena 2 floor/background applied (same scale as Arena 1).");
    }
    
    static void ApplyBackground(Sprite bgSprite)
    {
        // Arena 1 level root — sprite planes are parented here so they deactivate with the arena.
        // Use FindRootByName (not GameObject.Find) to locate it even when inactive.
        GameObject levelRoot = FindRootByName("=== LEVEL (ProBuilder) ===");

        if (levelRoot != null)
        {
            Transform floor = levelRoot.transform.Find("Floor");
            if (floor != null)
            {
                MeshRenderer floorRenderer = floor.GetComponent<MeshRenderer>();
                if (floorRenderer != null)
                {
                    floorRenderer.enabled = false;
                    Debug.Log("[EnvironmentSprites] Disabled floor mesh renderer");
                }
            }
        }

        if (levelRoot == null)
        {
            Debug.LogWarning("[EnvironmentSprites] Arena 1 root not found for background.");
            return;
        }

        ApplyBackgroundForRoot(levelRoot.transform, bgSprite);
        Debug.Log("[EnvironmentSprites] ✓ Background applied (parented to Arena 1 root)");
    }

    static void ApplyBackgroundForRoot(Transform levelRoot, Sprite bgSprite)
    {
        GameObject bgPlane = levelRoot.Find("Background_Plane")?.gameObject;
        if (bgPlane == null)
        {
            bgPlane = new GameObject("Background_Plane");
            bgPlane.transform.SetParent(levelRoot, false);
            bgPlane.transform.position = new Vector3(0f, 0.01f, 0f);
            bgPlane.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            bgPlane.transform.localScale = new Vector3(15f, 15f, 1f);
        }

        SpriteRenderer sr = bgPlane.GetComponent<SpriteRenderer>();
        if (sr == null) sr = bgPlane.AddComponent<SpriteRenderer>();
        sr.sprite = bgSprite;
        sr.sortingOrder = -100;
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.tileMode = SpriteTileMode.Continuous;
        sr.size = new Vector2(100f, 100f);
    }

    static void ApplyFloorMap(Sprite mapSprite)
    {
        GameObject levelRoot = FindRootByName("=== LEVEL (ProBuilder) ===");
        if (levelRoot == null)
        {
            Debug.LogWarning("[EnvironmentSprites] Arena 1 root not found for floor map.");
            return;
        }

        ApplyFloorMapForRoot(levelRoot.transform, mapSprite);
        Debug.Log("[EnvironmentSprites] ✓ Floor map applied (fits within borders, background visible outside, parented to Arena 1 root)");
    }

    /// <summary>Same world footprint as Arena 1: scale from sprite bounds and <see cref="FloorMapDesiredWorldSize"/>.</summary>
    public static void ApplyFloorMapForRoot(Transform levelRoot, Sprite mapSprite)
    {
        GameObject mapPlane = levelRoot.Find("Floor_Map")?.gameObject;
        if (mapPlane == null)
        {
            mapPlane = new GameObject("Floor_Map");
            mapPlane.transform.SetParent(levelRoot, false);
            mapPlane.transform.position = new Vector3(0f, 0.1f, 0f);
            mapPlane.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            mapPlane.transform.localScale = Vector3.one;
        }

        SpriteRenderer sr = mapPlane.GetComponent<SpriteRenderer>();
        if (sr == null) sr = mapPlane.AddComponent<SpriteRenderer>();
        sr.sprite = mapSprite;
        sr.sortingOrder = -50;
        sr.drawMode = SpriteDrawMode.Simple;

        float spriteWorldSize = mapSprite.bounds.size.x;
        if (spriteWorldSize > 0.001f)
        {
            float scale = FloorMapDesiredWorldSize / spriteWorldSize;
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
