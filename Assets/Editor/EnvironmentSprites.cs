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
        
        Debug.Log("[EnvironmentSprites] ✓ Environment sprites applied! (Walls disabled - 2D sprites don't work well from all angles)");
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
        
        // Find or create background plane
        GameObject bgPlane = GameObject.Find("Background_Plane");
        if (bgPlane == null)
        {
            bgPlane = new GameObject("Background_Plane");
            
            // Position at floor level (y = 0)
            bgPlane.transform.position = new Vector3(0f, 0f, 0f);
            bgPlane.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            bgPlane.transform.localScale = new Vector3(10f, 10f, 1f);
        }
        
        // Add sprite renderer
        SpriteRenderer sr = bgPlane.GetComponent<SpriteRenderer>();
        if (sr == null)
            sr = bgPlane.AddComponent<SpriteRenderer>();
        
        sr.sprite = bgSprite;
        sr.sortingOrder = -100; // Far behind everything
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.tileMode = SpriteTileMode.Continuous;
        sr.size = new Vector2(60f, 60f); // Larger than level to cover everything
        
        Debug.Log("[EnvironmentSprites] ✓ Background applied and floor mesh hidden");
    }
    
    static void ApplyFloorMap(Sprite mapSprite)
    {
        // Find or create floor map plane (playable arena)
        GameObject mapPlane = GameObject.Find("Floor_Map");
        if (mapPlane == null)
        {
            mapPlane = new GameObject("Floor_Map");
            
            // Position slightly above background, at ground level
            mapPlane.transform.position = new Vector3(0f, 0.01f, 0f);
            mapPlane.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            mapPlane.transform.localScale = new Vector3(8f, 8f, 1f);
        }
        
        // Add sprite renderer
        SpriteRenderer sr = mapPlane.GetComponent<SpriteRenderer>();
        if (sr == null)
            sr = mapPlane.AddComponent<SpriteRenderer>();
        
        sr.sprite = mapSprite;
        sr.sortingOrder = -90; // Above background (-100), below everything else
        
        Debug.Log("[EnvironmentSprites] ✓ Floor map applied for playable area");
    }
}
