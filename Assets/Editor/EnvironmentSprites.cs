using UnityEditor;
using UnityEngine;

/// <summary>
/// Applies background and wall sprites to the ProBuilder level
/// Run after creating the level
/// </summary>
public static class EnvironmentSprites
{
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
        // Hide the ProBuilder floor mesh so background is visible
        GameObject levelRoot = GameObject.Find("=== LEVEL (ProBuilder) ===");
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
        
        // Find or create background plane (outer area beyond walls)
        GameObject bgPlane = GameObject.Find("Background_Plane");
        if (bgPlane == null)
        {
            bgPlane = new GameObject("Background_Plane");
            
            // Position at floor level
            bgPlane.transform.position = new Vector3(0f, 0.01f, 0f);
            bgPlane.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            bgPlane.transform.localScale = new Vector3(15f, 15f, 1f); // Very large to show outside arena
        }
        
        // Add sprite renderer
        SpriteRenderer sr = bgPlane.GetComponent<SpriteRenderer>();
        if (sr == null)
            sr = bgPlane.AddComponent<SpriteRenderer>();
        
        sr.sprite = bgSprite;
        sr.sortingOrder = -100; // Far behind everything
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.tileMode = SpriteTileMode.Continuous;
        sr.size = new Vector2(100f, 100f); // Very large to show beyond walls
        
        Debug.Log("[EnvironmentSprites] ✓ Background applied (shows outside walls)");
    }
    
    static void ApplyFloorMap(Sprite mapSprite)
    {
        // Find or create floor map plane (playable arena interior)
        GameObject mapPlane = GameObject.Find("Floor_Map");
        if (mapPlane == null)
        {
            mapPlane = new GameObject("Floor_Map");
            
            // Position higher above background so it's clearly visible inside arena
            mapPlane.transform.position = new Vector3(0f, 0.1f, 0f);
            mapPlane.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            // Arena is 50x50 units with walls at ±25. Scale to ~9.6f to fit within walls
            // This leaves the border visible as the edge marker
            mapPlane.transform.localScale = new Vector3(9.6f, 9.6f, 1f);
        }
        
        // Add sprite renderer
        SpriteRenderer sr = mapPlane.GetComponent<SpriteRenderer>();
        if (sr == null)
            sr = mapPlane.AddComponent<SpriteRenderer>();
        
        sr.sprite = mapSprite;
        sr.sortingOrder = -50; // Above background (-100), below gameplay objects (0+)
        sr.drawMode = SpriteDrawMode.Simple; // Use simple mode, not tiled
        
        Debug.Log("[EnvironmentSprites] ✓ Floor map applied (fits within borders, background visible outside)");
    }
    
    static void EnableProBuilderWalls()
    {
        // Find all boundary wall objects and enable their 3D mesh renderers
        GameObject levelRoot = GameObject.Find("=== LEVEL (ProBuilder) ===");
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
