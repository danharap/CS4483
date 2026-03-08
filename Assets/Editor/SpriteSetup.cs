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
        
        // Single sprites
        ConfigureSingleSprite("Assets/Sprites/sEnemyDead.png");
        ConfigureSingleSprite("Assets/Sprites/sBullet.png");
        ConfigureSingleSprite("Assets/Sprites/sGun.png");
        ConfigureSingleSprite("Assets/Sprites/sBg.png");
        ConfigureSingleSprite("Assets/Sprites/sWall.png");
        
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
        Sprite deathSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/sEnemyDead.png");
        Sprite bulletSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/sBullet.png");
        Sprite gunSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/sGun.png");
        
        if (playerIdle.Length == 0 || enemy.Length == 0)
        {
            Debug.LogError("[SpriteSetup] Sprites not sliced! Run step 1 first.");
            return;
        }
        
        // Apply to enemy prefabs
        ApplySpriteToPrefab("Assets/Prefabs/Enemy_Chaser.prefab", enemy, deathSprite, new Color(1f, 0.3f, 0.3f)); // Red
        ApplySpriteToPrefab("Assets/Prefabs/Enemy_Fast.prefab", enemy, deathSprite, new Color(1f, 0.7f, 0.2f)); // Orange
        ApplySpriteToPrefab("Assets/Prefabs/Enemy_Boss.prefab", enemy, deathSprite, new Color(0.7f, 0.2f, 1f)); // Purple
        
        // Apply bullet sprite to projectile prefab
        ApplyBulletSpriteToPrefab("Assets/Prefabs/Projectile.prefab", bulletSprite);
        
        AssetDatabase.SaveAssets();
        Debug.Log("[SpriteSetup] ✓ Sprites applied to prefabs! Re-run SETUP EVERYTHING to see changes.");
    }
    
    [MenuItem("CS4483/🎨 3. Apply Sprites to Scene Objects")]
    public static void ApplySpritesToScene()
    {
        Debug.Log("[SpriteSetup] Applying sprites to scene objects...");
        
        Sprite[] playerIdle = LoadSlicedSprites("sPlayerIdle_strip4");
        Sprite[] enemy = LoadSlicedSprites("sEnemy_strip7");
        Sprite deathSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/sEnemyDead.png");
        Sprite gunSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/sGun.png");
        Sprite bulletSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/sBullet.png");
        
        // Apply to player in scene
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            AddSpriteComponent(player, playerIdle, null, Color.white);
            
            // Add gun sprite
            PlayerGun gunScript = player.GetComponent<PlayerGun>();
            if (gunScript == null)
                gunScript = player.AddComponent<PlayerGun>();
            gunScript.gunSprite = gunSprite;
            
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
            
            AddSpriteComponent(e.gameObject, enemy, deathSprite, tint);
            count++;
        }
        
        // Apply to all projectiles in scene
        int projCount = 0;
        Projectile[] projectiles = Object.FindObjectsOfType<Projectile>();
        foreach (Projectile proj in projectiles)
        {
            ProjectileSprite projSprite = proj.GetComponent<ProjectileSprite>();
            if (projSprite == null)
                projSprite = proj.gameObject.AddComponent<ProjectileSprite>();
            projSprite.bulletSprite = bulletSprite;
            projCount++;
        }
        
        Debug.Log($"[SpriteSetup] ✓ Applied sprites to {count} enemies and {projCount} projectiles in scene!");
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
    
    private static void AddSpriteComponent(GameObject target, Sprite[] frames, Sprite deathSprite, Color tint)
    {
        SpriteCharacter spriteChar = target.GetComponent<SpriteCharacter>();
        if (spriteChar == null)
            spriteChar = target.AddComponent<SpriteCharacter>();
        
        spriteChar.animationFrames = frames;
        spriteChar.deathSprite = deathSprite;
        spriteChar.tintColor = tint;
        spriteChar.frameRate = 10f;
        spriteChar.spriteScale = new Vector3(1.5f, 1.5f, 1f);
    }
    
    private static void ApplySpriteToPrefab(string prefabPath, Sprite[] frames, Sprite deathSprite, Color tint)
    {
        using (var scope = new PrefabUtility.EditPrefabContentsScope(prefabPath))
        {
            GameObject root = scope.prefabContentsRoot;
            AddSpriteComponent(root, frames, deathSprite, tint);
            Debug.Log($"[SpriteSetup] Applied sprite to {root.name}");
        }
    }
    
    private static void ConfigureSingleSprite(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return;
        
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.filterMode = FilterMode.Point;
        importer.spritePixelsPerUnit = 32;
        importer.SaveAndReimport();
        
        Debug.Log($"[SpriteSetup] Configured single sprite: {path}");
    }
    
    private static void ApplyBulletSpriteToPrefab(string prefabPath, Sprite bulletSprite)
    {
        using (var scope = new PrefabUtility.EditPrefabContentsScope(prefabPath))
        {
            GameObject root = scope.prefabContentsRoot;
            ProjectileSprite projSprite = root.GetComponent<ProjectileSprite>();
            if (projSprite == null)
                projSprite = root.AddComponent<ProjectileSprite>();
            projSprite.bulletSprite = bulletSprite;
            Debug.Log($"[SpriteSetup] Applied bullet sprite to {root.name}");
        }
    }
}
