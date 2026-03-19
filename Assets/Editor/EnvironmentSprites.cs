using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Applies background and wall sprites to the ProBuilder level
/// Run after creating the level
/// </summary>
public static class EnvironmentSprites
{
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
        
        Debug.Log("[EnvironmentSprites] ✓ Environment sprites applied! Using 3D green ProBuilder walls.");
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

        // Find or create background plane — parented to Arena 1 root so it hides with the arena
        Transform bgParent = levelRoot != null ? levelRoot.transform : null;
        GameObject bgPlane = bgParent != null
            ? bgParent.Find("Background_Plane")?.gameObject
            : GameObject.Find("Background_Plane");

        if (bgPlane == null)
        {
            bgPlane = new GameObject("Background_Plane");
            if (bgParent != null) bgPlane.transform.SetParent(bgParent, false);
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

        Debug.Log("[EnvironmentSprites] ✓ Background applied (parented to Arena 1 root)");
    }

    static void ApplyFloorMap(Sprite mapSprite)
    {
        // Floor map is also parented to Arena 1 root (use FindRootByName for inactive objects)
        GameObject levelRoot = FindRootByName("=== LEVEL (ProBuilder) ===");
        Transform mapParent = levelRoot != null ? levelRoot.transform : null;

        GameObject mapPlane = mapParent != null
            ? mapParent.Find("Floor_Map")?.gameObject
            : GameObject.Find("Floor_Map");

        if (mapPlane == null)
        {
            mapPlane = new GameObject("Floor_Map");
            if (mapParent != null) mapPlane.transform.SetParent(mapParent, false);
            mapPlane.transform.position = new Vector3(0f, 0.1f, 0f);
            mapPlane.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            mapPlane.transform.localScale = Vector3.one;
        }

        SpriteRenderer sr = mapPlane.GetComponent<SpriteRenderer>();
        if (sr == null) sr = mapPlane.AddComponent<SpriteRenderer>();
        sr.sprite = mapSprite;
        sr.sortingOrder = -50; // Above background (-100), below gameplay objects (0+)
        sr.drawMode = SpriteDrawMode.Simple; // Use simple mode, not tiled

        // Fit sprite to arena interior. ProBuilder arena uses ArenaRadius=38 → diameter ≈ 76 world units.
        // Uses sprite bounds so it adapts to PPU changes.
        // Make the floor larger than the wall footprint so it fully covers the octagon interior.
        float desiredWorldSize = 86f;
        float spriteWorldSize = sr.sprite.bounds.size.x; // square map
        if (spriteWorldSize > 0.001f)
        {
            float scale = desiredWorldSize / spriteWorldSize;
            mapPlane.transform.localScale = new Vector3(scale, scale, 1f);
        }
        
        Debug.Log("[EnvironmentSprites] ✓ Floor map applied (fits within borders, background visible outside, parented to Arena 1 root)");
    }
    
    static void EnableProBuilderWalls()
    {
        // Find all boundary wall objects and enable their 3D mesh renderers
        GameObject levelRoot = FindRootByName("=== LEVEL (ProBuilder) ===");
        if (levelRoot == null)
        {
            Debug.LogWarning("[EnvironmentSprites] Level root not found!");
            return;
        }
        
        Transform wallsParent = levelRoot.transform.Find("Boundary_Walls");
        if (wallsParent == null)
        {
            Debug.LogWarning("[EnvironmentSprites] Boundary_Walls not found!");
            return;
        }
        
        int wallCount = 0;
        foreach (Transform wall in wallsParent)
        {
            // Enable the ProBuilder mesh renderer for 3D green walls
            MeshRenderer meshRenderer = wall.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                meshRenderer.enabled = true;
            }
            
            // Remove any old wall sprite billboard objects
            Transform oldSprite = wall.Find("Wall_Sprite");
            if (oldSprite != null)
                Object.DestroyImmediate(oldSprite.gameObject);
            
            wallCount++;
        }
        
        Debug.Log($"[EnvironmentSprites] ✓ Enabled {wallCount} 3D ProBuilder walls (green from material)");
    }
}
