using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Sets up 2D sprite billboards for 3D top-down game
/// CS4483 → Setup Sprite Billboards
/// </summary>
public static class SpriteSetup
{
    [MenuItem("CS4483/🎨 1. Slice Sprite Sheets")]
    public static void SliceSpriteSheets()
    {
        Debug.Log("[SpriteSetup] Slicing sprite sheets...");
        
        // Slice player idle (4 frames)
        SliceSpriteSheet("Assets/Sprites/sPlayerIdle_strip4.png", 4);
        
        // Slice player run (7 frames)
        SliceSpriteSheet("Assets/Sprites/sPlayerRun_strip7.png", 7);
        
        // Slice enemy (7 frames)
        SliceSpriteSheet("Assets/Sprites/sEnemy_strip7.png", 7);
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[SpriteSetup] ✓ Sprite sheets sliced! Now run step 2.");
    }
    
    [MenuItem("CS4483/🎨 2. Apply Sprites to Prefabs")]
    public static void ApplySpritesToPrefabs()
    {
        Debug.Log("[SpriteSetup] Applying sprites to prefabs...");
        
        // Load sliced sprites
        Sprite[] playerIdle = LoadSlicedSprites("sPlayerIdle_strip4");
        Sprite[] playerRun = LoadSlicedSprites("sPlayerRun_strip7");
        Sprite[] enemy = LoadSlicedSprites("sEnemy_strip7");
        
        if (playerIdle.Length == 0 || enemy.Length == 0)
        {
            Debug.LogError("[SpriteSetup] Sprites not sliced! Run step 1 first.");
            return;
        }
        
        // Apply to enemy prefabs
        ApplySpriteToPrefab("Assets/Prefabs/Enemy_Chaser.prefab", enemy, new Color(1f, 0.3f, 0.3f)); // Red
        ApplySpriteToPrefab("Assets/Prefabs/Enemy_Fast.prefab", enemy, new Color(1f, 0.7f, 0.2f)); // Orange
        ApplySpriteToPrefab("Assets/Prefabs/Enemy_Boss.prefab", enemy, new Color(0.7f, 0.2f, 1f)); // Purple
        
        AssetDatabase.SaveAssets();
        Debug.Log("[SpriteSetup] ✓ Sprites applied to prefabs! Re-run SETUP EVERYTHING to see changes.");
    }
    
    [MenuItem("CS4483/🎨 3. Apply Sprites to Scene Objects")]
    public static void ApplySpritesToScene()
    {
        Debug.Log("[SpriteSetup] Applying sprites to scene objects...");
        
        Sprite[] playerIdle = LoadSlicedSprites("sPlayerIdle_strip4");
        Sprite[] enemy = LoadSlicedSprites("sEnemy_strip7");
        
        // Apply to player in scene
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            AddSpriteComponent(player, playerIdle, Color.white);
            Debug.Log("[SpriteSetup] ✓ Applied sprite to Player");
        }
        
        // Apply to all enemies in scene
        int count = 0;
        EnemyBase[] enemies = Object.FindObjectsOfType<EnemyBase>();
        foreach (EnemyBase e in enemies)
        {
            Color tint = Color.white;
            if (e is ChaserEnemy) tint = new Color(1f, 0.3f, 0.3f);
            else if (e is FastEnemy) tint = new Color(1f, 0.7f, 0.2f);
            else if (e is BossEnemy) tint = new Color(0.7f, 0.2f, 1f);
            
            AddSpriteComponent(e.gameObject, enemy, tint);
            count++;
        }
        
        Debug.Log($"[SpriteSetup] ✓ Applied sprites to {count} enemies in scene!");
    }
    
    // ── Helper Methods ────────────────────────────────────────────────────
    
    private static void SliceSpriteSheet(string path, int frameCount)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return;
        
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.filterMode = FilterMode.Point;
        importer.spritePixelsPerUnit = 32; // Adjust for pixel art
        
        // Get texture dimensions
        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (tex == null) return;
        
        int frameWidth = tex.width / frameCount;
        int frameHeight = tex.height;
        
        // Create sprite metadata for each frame
        List<SpriteMetaData> spriteSheet = new List<SpriteMetaData>();
        for (int i = 0; i < frameCount; i++)
        {
            SpriteMetaData meta = new SpriteMetaData();
            meta.name = $"frame_{i}";
            meta.rect = new Rect(i * frameWidth, 0, frameWidth, frameHeight);
            meta.pivot = new Vector2(0.5f, 0.5f);
            meta.alignment = (int)SpriteAlignment.Center;
            spriteSheet.Add(meta);
        }
        
        importer.spritesheet = spriteSheet.ToArray();
        importer.SaveAndReimport();
        
        Debug.Log($"[SpriteSetup] Sliced {path} into {frameCount} frames");
    }
    
    private static Sprite[] LoadSlicedSprites(string name)
    {
        Object[] sprites = AssetDatabase.LoadAllAssetsAtPath($"Assets/Sprites/{name}.png");
        List<Sprite> result = new List<Sprite>();
        
        foreach (Object obj in sprites)
        {
            if (obj is Sprite sprite && sprite.name.StartsWith("frame_"))
                result.Add(sprite);
        }
        
        result.Sort((a, b) => a.name.CompareTo(b.name));
        return result.ToArray();
    }
    
    private static void AddSpriteComponent(GameObject target, Sprite[] frames, Color tint)
    {
        SpriteCharacter spriteChar = target.GetComponent<SpriteCharacter>();
        if (spriteChar == null)
            spriteChar = target.AddComponent<SpriteCharacter>();
        
        spriteChar.animationFrames = frames;
        spriteChar.tintColor = tint;
        spriteChar.frameRate = 10f;
        spriteChar.spriteScale = new Vector3(1.5f, 1.5f, 1f);
    }
    
    private static void ApplySpriteToPrefab(string prefabPath, Sprite[] frames, Color tint)
    {
        using (var scope = new PrefabUtility.EditPrefabContentsScope(prefabPath))
        {
            GameObject root = scope.prefabContentsRoot;
            AddSpriteComponent(root, frames, tint);
            Debug.Log($"[SpriteSetup] Applied sprite to {root.name}");
        }
    }
}
