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
            // Hide the ProBuilder mesh renderer
            MeshRenderer meshRenderer = wall.GetComponent<MeshRenderer>();
            Bounds bounds = meshRenderer != null ? meshRenderer.bounds : new Bounds(wall.position, Vector3.one);
            if (meshRenderer != null)
                meshRenderer.enabled = false;
            
            // Create child for sprite billboard
            GameObject wallSpriteObj = wall.Find("Wall_Sprite")?.gameObject;
            if (wallSpriteObj == null)
            {
                wallSpriteObj = new GameObject("Wall_Sprite");
                wallSpriteObj.transform.SetParent(wall);
                wallSpriteObj.transform.localPosition = new Vector3(0, 1.5f, 0);
                wallSpriteObj.transform.localScale = Vector3.one;
            }
            
            // Add sprite renderer to child
            SpriteRenderer sr = wallSpriteObj.GetComponent<SpriteRenderer>();
            if (sr == null)
                sr = wallSpriteObj.AddComponent<SpriteRenderer>();
            
            sr.sprite = wallSprite;
            sr.sortingOrder = 5;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.tileMode = SpriteTileMode.Continuous;
            
            // Calculate tiling based on wall bounds
            float width = Mathf.Max(bounds.size.x, bounds.size.z);
            sr.size = new Vector2(width, 3f); // Fixed height for walls
            
            // Add billboard to face camera
            if (wallSpriteObj.GetComponent<Billboard>() == null)
                wallSpriteObj.AddComponent<Billboard>();
            
            wallCount++;
        }
        
        Debug.Log($"[EnvironmentSprites] ✓ Applied wall sprites to {wallCount} walls");
    }
}
