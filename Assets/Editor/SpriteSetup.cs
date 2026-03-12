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
        ConfigureSingleSprite("Assets/Sprites/sMap.png");
        ConfigureSingleSprite("Assets/Sprites/sMap_Arena2_Red.png");
        ConfigureSingleSprite("Assets/Sprites/sWall.png");
        ConfigureSingleSprite("Assets/Sprites/sExperience.png"); // Custom XP orb
        ConfigureSingleSprite("Assets/Sprites/sMedkit.png"); // Custom health pack
        ConfigureSingleSprite("Assets/Sprites/sdeadPlayer.png"); // Dead body prop

        // Zap trap sheets (legacy) / pre-cut frames
        // If you use pre-cut frames, run CS4483 → ⚡ Setup ZapTrap Blue Frames (Pre-cut)
        // and SpriteSetup will load frames from Assets/Sprites/ZapTrapBlueFrames.
        
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
        Sprite xpOrbSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/sExperience.png");
        Sprite medkitSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/sMedkit.png");
        
        if (playerIdle.Length == 0 || enemy.Length == 0)
        {
            Debug.LogError("[SpriteSetup] Sprites not sliced! Run step 1 first.");
            return;
        }
        
        // Apply to enemy prefabs
        ApplySpriteToPrefab("Assets/Prefabs/Enemy_Chaser.prefab", enemy, deathSprite, Color.white); // No tint (default sprite color)
        ApplySpriteToPrefab("Assets/Prefabs/Enemy_Fast.prefab", enemy, deathSprite, new Color(1f, 0.3f, 0.3f)); // Red tint
        ApplySpriteToPrefab("Assets/Prefabs/Enemy_Boss.prefab", enemy, deathSprite, new Color(0.7f, 0.2f, 1f)); // Purple tint
        
        // Apply bullet sprite to projectile prefab
        ApplyBulletSpriteToPrefab("Assets/Prefabs/Projectile.prefab", bulletSprite);
        
        // Apply XP orb sprite
        ApplyPickupSpriteToPrefab("Assets/Prefabs/XPOrb.prefab", xpOrbSprite, false);

        // Apply HealthPack medkit sprite + green glow
        ApplyPickupSpriteToPrefab("Assets/Prefabs/HealthPack.prefab", medkitSprite, true);

        // Wire ArenaThemeController sprite references for runtime switching
        WireThemeControllerAssets();
        
        AssetDatabase.SaveAssets();
        Debug.Log("[SpriteSetup] ✓ Sprites applied to prefabs (XP orb + HealthPack medkit glow). Re-run SETUP EVERYTHING to see changes.");
    }

    private static void WireThemeControllerAssets()
    {
        GameObject mgr = GameObject.Find("=== MANAGERS ===");
        if (mgr == null) return;
        ArenaThemeController theme = mgr.GetComponent<ArenaThemeController>();
        if (theme == null) return;

        // Floor sprites
        theme.arena1FloorSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/sMap.png");
        theme.arena2FloorSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/sMap_Arena2_Red.png");

        // Trap animations
        theme.zapTrapBlueFrames = LoadSlicedSprites("sZapTrap_Blue");
        theme.zapTrapRedFrames = LoadSlicedSprites("sZapTrap_Red");
    }
    
    [MenuItem("CS4483/🎨 3. Apply Sprites to Scene Objects")]
    public static void ApplySpritesToScene()
    {
        Debug.Log("[SpriteSetup] Applying sprites to scene objects...");
        
        Sprite[] playerIdle = LoadSlicedSprites("sPlayerIdle_strip4");
        Sprite[] playerRun = LoadSlicedSprites("sPlayerRun_strip7");
        Sprite[] enemy = LoadSlicedSprites("sEnemy_strip7");
        Sprite deathSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/sEnemyDead.png");
        Sprite gunSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/sGun.png");
        Sprite bulletSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/sBullet.png");
        Sprite xpOrbSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/sExperience.png");
        Sprite medkitSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/sMedkit.png");
        Sprite deadBodySprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/sdeadPlayer.png");
        
        // Apply to player in scene
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            AddPlayerSpriteComponent(player, playerIdle, playerRun);
            
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
            Color tint = Color.white; // Default: no tint
            if (e is FastEnemy) tint = new Color(1f, 0.3f, 0.3f); // Red tint
            else if (e is BossEnemy) tint = new Color(0.7f, 0.2f, 1f); // Purple tint
            // Chaser uses default (no tint)
            
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
        
        // Apply to all XP orbs in scene
        int xpCount = 0;
        GameObject[] xpOrbs = GameObject.FindGameObjectsWithTag("XPOrb");
        foreach (GameObject orb in xpOrbs)
        {
            PickupSprite orbSprite = orb.GetComponent<PickupSprite>();
            if (orbSprite == null)
                orbSprite = orb.AddComponent<PickupSprite>();
            orbSprite.pickupSprite = xpOrbSprite;
            xpCount++;
        }
        
        // Health packs: keep as 3D plus sign with glow (no sprite)
        int healthCount = 0;
        HealthPack[] healthPacks = Object.FindObjectsOfType<HealthPack>();
        foreach (HealthPack hp in healthPacks)
        {
            // HealthPack prefab now uses medkit sprite + green glow via PickupSprite
            healthCount++;
        }

        // Apply zap trap animations (blue in Arena 1, red in Arena 2)
        Sprite[] zapBlue = LoadZapTrapBlueFrames();
        Sprite[] zapRed = LoadSlicedSprites("sZapTrap_Red");
        int trapCount = 0;
        foreach (ArenaTrap trap in Resources.FindObjectsOfTypeAll<ArenaTrap>())
        {
            if (trap == null) continue;
            // Skip prefabs/assets, only operate on scene objects
            if (EditorUtility.IsPersistent(trap.gameObject)) continue;

            // Remove any legacy 3D pad visuals
            Transform pad = trap.transform.Find("Pad");
            if (pad != null) Object.DestroyImmediate(pad.gameObject);

            Transform visual = trap.transform.Find("TrapVisual");
            if (visual == null)
            {
                GameObject v = new GameObject("TrapVisual");
                v.transform.SetParent(trap.transform, false);
                v.transform.localPosition = Vector3.zero;
                visual = v.transform;
            }

            TrapSpriteAnimator anim = visual.GetComponent<TrapSpriteAnimator>();
            if (anim == null) anim = visual.gameObject.AddComponent<TrapSpriteAnimator>();
            anim.frameRate = 12f;

            bool isArena2 = IsUnderRootNamed(trap.gameObject, "=== LEVEL (ProBuilder) Arena2 ===");
            anim.SetFrames(isArena2 ? zapRed : zapBlue);
            trapCount++;
        }
        
        // Apply to all dead bodies in scene (billboard sprite only)
        int deadBodyCount = 0;
        DeadBody[] deadBodies = Object.FindObjectsOfType<DeadBody>();
        foreach (DeadBody db in deadBodies)
        {
            AddDeadBodySprite(db.gameObject, deadBodySprite);
            deadBodyCount++;
        }
        
        Debug.Log($"[SpriteSetup] ✓ Applied sprites to {count} enemies, {projCount} projectiles, {xpCount} XP orbs, {healthCount} health packs, {trapCount} traps, and {deadBodyCount} dead bodies in scene!");
    }

    private static bool IsUnderRootNamed(GameObject go, string rootName)
    {
        Transform t = go != null ? go.transform : null;
        while (t != null)
        {
            if (t.parent == null && t.gameObject.name == rootName) return true;
            t = t.parent;
        }
        return false;
    }

    private static Sprite[] LoadZapTrapBlueFrames()
    {
        // Prefer pre-cut frames in Assets/Sprites/ZapTrapBlueFrames/0..11.png
        var list = new List<Sprite>();
        for (int i = 0; i < 12; i++)
        {
            string path = $"Assets/Sprites/ZapTrapBlueFrames/{i}.png";
            Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (s != null) list.Add(s);
        }
        if (list.Count == 12) return list.ToArray();

        // Fallback: if frames aren’t present, try legacy sliced sheet
        return LoadSlicedSprites("sZapTrap_Blue");
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

    private static void SliceSpriteSheetGrid(string path, int columns, int rows, float ppu)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.filterMode = FilterMode.Point;
        importer.spritePixelsPerUnit = ppu;
        importer.alphaSource = TextureImporterAlphaSource.FromInput;
        importer.alphaIsTransparency = true;

        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (tex == null) return;

        int frameWidth = tex.width / columns;
        int frameHeight = tex.height / rows;

        List<SpriteMetaData> metas = new List<SpriteMetaData>();
        int idx = 0;
        // Unity rects are bottom-left origin; iterate rows from bottom to top
        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < columns; x++)
            {
                SpriteMetaData meta = new SpriteMetaData();
                meta.name = $"frame_{idx++}";
                meta.rect = new Rect(x * frameWidth, y * frameHeight, frameWidth, frameHeight);
                meta.pivot = new Vector2(0.5f, 0.5f);
                meta.alignment = (int)SpriteAlignment.Center;
                metas.Add(meta);
            }
        }

        importer.spritesheet = metas.ToArray();
        importer.SaveAndReimport();
        Debug.Log($"[SpriteSetup] Sliced grid {path} into {columns * rows} frames");
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
        
        spriteChar.idleFrames = frames;
        spriteChar.deathSprite = deathSprite;
        spriteChar.tintColor = tint;
        spriteChar.frameRate = 10f;
        spriteChar.spriteScale = new Vector3(1.5f, 1.5f, 1f);
    }
    
    private static void AddPlayerSpriteComponent(GameObject target, Sprite[] idleFrames, Sprite[] runFrames)
    {
        SpriteCharacter spriteChar = target.GetComponent<SpriteCharacter>();
        if (spriteChar == null)
            spriteChar = target.AddComponent<SpriteCharacter>();
        
        spriteChar.idleFrames = idleFrames;
        spriteChar.runFrames = runFrames;
        spriteChar.tintColor = Color.white;
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
        importer.spritePixelsPerUnit = 40; // Changed from 32 to 40 for 40x40 assets
        importer.alphaSource = TextureImporterAlphaSource.FromInput; // Preserve alpha channel
        importer.alphaIsTransparency = true; // Enable transparency
        importer.SaveAndReimport();
        
        Debug.Log($"[SpriteSetup] Configured single sprite with transparency: {path}");
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
    
    private static void ApplyPickupSpriteToPrefab(string prefabPath, Sprite pickupSprite, bool enableGlow)
    {
        using (var scope = new PrefabUtility.EditPrefabContentsScope(prefabPath))
        {
            GameObject root = scope.prefabContentsRoot;
            PickupSprite pickup = root.GetComponent<PickupSprite>();
            if (pickup == null)
                pickup = root.AddComponent<PickupSprite>();
            pickup.pickupSprite = pickupSprite;
            pickup.enableGlow = enableGlow;
            if (enableGlow)
            {
                pickup.glowColor = new Color(0.2f, 1f, 0.3f);
                pickup.glowIntensity = 2.5f;
                pickup.glowRange = 3.5f;
            }
            Debug.Log($"[SpriteSetup] Applied pickup sprite to {root.name}");
        }
    }
    
    private static void AddDeadBodySprite(GameObject root, Sprite deadBodySprite)
    {
        if (deadBodySprite == null) return;
        Transform existing = root.transform.Find("DeadBody_Sprite");
        if (existing != null) Object.DestroyImmediate(existing.gameObject);
        GameObject spriteObj = new GameObject("DeadBody_Sprite");
        spriteObj.transform.SetParent(root.transform);
        spriteObj.transform.localPosition = Vector3.zero;
        spriteObj.transform.localScale = new Vector3(0.2f, 0.2f, 1f); // Double of 0.1 (~20x20)
        SpriteRenderer sr = spriteObj.AddComponent<SpriteRenderer>();
        sr.sprite = deadBodySprite;
        sr.sortingOrder = 5;
        spriteObj.AddComponent<Billboard>();
        Debug.Log($"[SpriteSetup] Applied dead body sprite to {root.name}");
    }
}
