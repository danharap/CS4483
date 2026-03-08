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
        Debug.Log("[EnvironmentSprites] Applying background and wall sprites...");
        
        // Load sprites
        Sprite bgSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/sBg.png");
        Sprite wallSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/sWall.png");
        
        if (bgSprite == null || wallSprite == null)
        {
            Debug.LogError("[EnvironmentSprites] Sprites not found! Run '🎨 1. Slice Sprite Sheets' first.");
            return;
        }
        
        // Apply background
        ApplyBackground(bgSprite);
        
        // Apply walls
        ApplyWalls(wallSprite);
        
        Debug.Log("[EnvironmentSprites] ✓ Environment sprites applied!");
    }
    
    static void ApplyBackground(Sprite bgSprite)
    {
        // Find or create background plane
        GameObject bgPlane = GameObject.Find("Background_Plane");
        if (bgPlane == null)
        {
            bgPlane = new GameObject("Background_Plane");
            
            // Position slightly below floor
            bgPlane.transform.position = new Vector3(0f, -0.5f, 0f);
            bgPlane.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            bgPlane.transform.localScale = new Vector3(10f, 10f, 1f);
        }
        
        // Add sprite renderer
        SpriteRenderer sr = bgPlane.GetComponent<SpriteRenderer>();
        if (sr == null)
            sr = bgPlane.AddComponent<SpriteRenderer>();
        
        sr.sprite = bgSprite;
        sr.sortingOrder = -10; // Behind everything
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.tileMode = SpriteTileMode.Continuous;
        sr.size = new Vector2(50f, 50f); // Match level size
        
        Debug.Log("[EnvironmentSprites] ✓ Background applied");
    }
    
    static void ApplyWalls(Sprite wallSprite)
    {
        // Find all boundary wall objects
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
            // Add sprite renderer to wall
            SpriteRenderer sr = wall.GetComponent<SpriteRenderer>();
            if (sr == null)
                sr = wall.gameObject.AddComponent<SpriteRenderer>();
            
            sr.sprite = wallSprite;
            sr.sortingOrder = 5;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.tileMode = SpriteTileMode.Continuous;
            
            // Calculate tiling based on wall size
            Renderer meshRenderer = wall.GetComponent<Renderer>();
            if (meshRenderer != null)
            {
                Bounds bounds = meshRenderer.bounds;
                sr.size = new Vector2(bounds.size.x, bounds.size.z);
            }
            
            // Rotate to face camera
            wall.gameObject.AddComponent<Billboard>();
            
            wallCount++;
        }
        
        Debug.Log($"[EnvironmentSprites] ✓ Applied wall sprites to {wallCount} walls");
    }
}
