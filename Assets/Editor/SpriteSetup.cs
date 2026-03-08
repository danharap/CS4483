using UnityEditor;
using UnityEngine;

/// <summary>
/// Sets up 2D sprite billboards for 3D top-down game
/// CS4483 → Setup Sprite Billboards
/// </summary>
public static class SpriteSetup
{
    [MenuItem("CS4483/🎨 Setup Sprite Billboards")]
    public static void SetupSpriteBillboards()
    {
        Debug.Log("[SpriteSetup] Starting sprite billboard setup...");
        
        // Load sprites
        Sprite playerIdle = LoadSprite("sPlayerIdle_strip4");
        Sprite playerRun = LoadSprite("sPlayerRun_strip7");
        Sprite enemy = LoadSprite("sEnemy_strip7");
        Sprite bullet = LoadSprite("sBullet");
        
        if (playerIdle == null || enemy == null)
        {
            Debug.LogError("[SpriteSetup] Could not load sprites! Make sure they're in Assets/Sprites/");
            return;
        }
        
        // Apply to Player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            ApplySpriteBillboard(player, playerIdle, new Vector3(0.8f, 1.2f, 1f), Color.white);
            Debug.Log("[SpriteSetup] ✓ Applied sprite to Player");
        }
        
        // Apply to all enemies
        EnemyBase[] enemies = Object.FindObjectsOfType<EnemyBase>();
        foreach (EnemyBase e in enemies)
        {
            Color enemyColor = Color.white;
            
            // Color code by enemy type
            if (e is ChaserEnemy)
                enemyColor = new Color(1f, 0.3f, 0.3f); // Red tint
            else if (e is FastEnemy)
                enemyColor = new Color(1f, 0.7f, 0.2f); // Orange tint
            else if (e is BossEnemy)
                enemyColor = new Color(0.7f, 0.2f, 1f); // Purple tint
            
            ApplySpriteBillboard(e.gameObject, enemy, new Vector3(0.8f, 1.2f, 1f), enemyColor);
        }
        Debug.Log($"[SpriteSetup] ✓ Applied sprites to {enemies.Length} enemies");
        
        Debug.Log("[SpriteSetup] ✓ Sprite billboard setup complete!");
    }
    
    private static Sprite LoadSprite(string name)
    {
        // Try loading from Assets/Sprites/
        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/Sprites/{name}.png");
        if (tex == null) return null;
        
        // Convert texture to sprite if needed
        string path = AssetDatabase.GetAssetPath(tex);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null && importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 100;
            importer.filterMode = FilterMode.Point; // Pixel art style
            importer.SaveAndReimport();
        }
        
        return AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Sprites/{name}.png");
    }
    
    private static void ApplySpriteBillboard(GameObject target, Sprite sprite, Vector3 scale, Color tint)
    {
        // Remove existing mesh renderer if present
        MeshRenderer existingRenderer = target.GetComponent<MeshRenderer>();
        if (existingRenderer != null)
        {
            existingRenderer.enabled = false; // Hide 3D mesh but keep for reference
        }
        
        // Create sprite child object
        GameObject spriteObj = new GameObject("Sprite_Billboard");
        spriteObj.transform.SetParent(target.transform);
        spriteObj.transform.localPosition = Vector3.zero;
        spriteObj.transform.localScale = scale;
        
        // Add sprite renderer
        SpriteRenderer sr = spriteObj.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = tint;
        sr.sortingOrder = 10; // Render on top
        
        // Add billboard script to face camera
        spriteObj.AddComponent<Billboard>();
        
        Debug.Log($"[SpriteSetup] Applied sprite to {target.name} with tint {tint}");
    }
}
