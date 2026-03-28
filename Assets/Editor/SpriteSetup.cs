using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Sets up 2D sprite billboards for 3D top-down game
/// CS4483 → Setup Sprite Billboards
/// </summary>
public static class SpriteSetup
{
    // Global pixel-art scale target:
    // - 40x40 sprites at PPU 20 = 2 world units (bigger, readable)
    // - 1024x1024 arena map at PPU 20 = 51.2 world units (fits ~50 unit arena with small scale tweak)
    private const float GlobalPPU = 20f;
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
        ConfigureSingleSprite("Assets/Sprites/sBg_Red.png");
        ConfigureSingleSprite("Assets/Sprites/sMap.png");
        ConfigureSingleSprite("Assets/Sprites/sMap_Arena2_Red.png");
        ConfigureSingleSprite("Assets/Sprites/sMap2.png"); // Arena 2 floor
        ConfigureSingleSprite("Assets/Sprites/sWall.png");
        ConfigureSingleSprite("Assets/Sprites/sExperience.png"); // Custom XP orb
        ConfigureSingleSprite("Assets/Sprites/sMedkit.png"); // Custom health pack
        ConfigureSingleSprite("Assets/Sprites/sdeadPlayer.png"); // Dead body prop
        ConfigureSingleSprite("Assets/Sprites/sHeavyHead.png"); // Heavy enemy head (up/down)
        ConfigureSingleSprite("Assets/Sprites/sHeavyHead_Right.png"); // Heavy enemy head (right)
        ConfigureSingleSprite("Assets/Sprites/sObstacleBox.png"); // Obstacle box 40x40

        // Heavy body frames (pre-cut individual PNGs in folders)
        ConfigureSpritesInFolder("Assets/Sprites/TankWalking");
        ConfigureSpritesInFolder("Assets/Sprites/Tank Walking Right");

        // Bat enemies (pre-cut individual PNGs in folders)
        ConfigureSpritesInFolder("Assets/Sprites/small Bat Fast");
        ConfigureSpritesInFolder("Assets/Sprites/big Bat Fast");
        ConfigureSpritesInFolder("Assets/Sprites/big Bat Fast Biting");

        // Boss sprites (pre-cut individual PNGs in folders)
        ConfigureSpritesInFolder("Assets/Sprites/Boss/Boss Body Walking");
        ConfigureSpritesInFolder("Assets/Sprites/Boss/Boss Body Walking Right");
        ConfigureSpritesInFolder("Assets/Sprites/Boss/Boss Body Attacking");
        ConfigureSpritesInFolder("Assets/Sprites/Boss/Boss Head Walking");
        ConfigureSpritesInFolder("Assets/Sprites/Boss/Boss Head Attacking");

        // Zap trap sheets (legacy) / pre-cut frames
        // If you use pre-cut frames, run CS4483 → ⚡ Setup ZapTrap Blue Frames (Pre-cut)
        // and SpriteSetup will load frames from Assets/Sprites/ZapTrapBlueFrames.

        // ── Final Boss (Satan) sprites ────────────────────────────────────
        ConfigureSpritesInFolder("Assets/Sprites/Final Boss/Satan Awakening");
        ConfigureSpritesInFolder("Assets/Sprites/Final Boss/Satan Death");
        ConfigureSpritesInFolder("Assets/Sprites/Final Boss/Satan Direct Attack");
        ConfigureSpritesInFolder("Assets/Sprites/Final Boss/Satan Down Beam Attack");
        ConfigureSpritesInFolder("Assets/Sprites/Final Boss/Satan Fan Attack or Dual Hand Beam Attack");
        ConfigureSingleSprite("Assets/Sprites/Final Boss/Satan Foot.png");
        ConfigureSingleSprite("Assets/Sprites/Final Boss/Bullets.png");
        
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
        // Fast = small bat (no tint) - slightly larger
        Sprite[] smallBatFast = LoadSpritesInFolder("Assets/Sprites/small Bat Fast");
        if (smallBatFast.Length == 0) smallBatFast = enemy;
        ApplySpriteToPrefab("Assets/Prefabs/Enemy_Fast.prefab", smallBatFast, deathSprite, Color.white);
        // With GlobalPPU=20, keep Fast enemy at scale 1
        using (var scope = new PrefabUtility.EditPrefabContentsScope("Assets/Prefabs/Enemy_Fast.prefab"))
        {
            GameObject root = scope.prefabContentsRoot;
            SpriteCharacter sc = root.GetComponent<SpriteCharacter>();
            if (sc != null) sc.spriteScale = Vector3.one;
        }
        // Boss uses Heavy-style Body+Head visuals + slam attack frames (no tint)
        Sprite[] bossBodyWalk = LoadSpritesInFolder("Assets/Sprites/Boss/Boss Body Walking");
        Sprite[] bossBodyWalkRight = LoadSpritesInFolder("Assets/Sprites/Boss/Boss Body Walking Right");
        Sprite[] bossBodyAttack = LoadSpritesInFolder("Assets/Sprites/Boss/Boss Body Attacking");
        Sprite[] bossHeadWalk = LoadSpritesInFolder("Assets/Sprites/Boss/Boss Head Walking");
        Sprite[] bossHeadAttack = LoadSpritesInFolder("Assets/Sprites/Boss/Boss Head Attacking");
        ApplyBossSprites("Assets/Prefabs/Enemy_Boss.prefab", bossBodyWalk, bossBodyWalkRight, bossBodyAttack, bossHeadWalk, bossHeadAttack, deathSprite);

        // ── Satan Final Boss sprites ──────────────────────────────────────
        ApplySatanSprites();
        // Heavy: animated body from your custom tank frame folders + head sprites (flip when moving left)
        // Put your folders inside the Unity project, e.g.:
        //   Assets/Sprites/TankWalking/
        //   Assets/Sprites/Tank Walking Right/
        Sprite[] tankWalkUpDown = LoadSpritesInFolder("Assets/Sprites/TankWalking");
        Sprite[] tankWalkRight  = LoadSpritesInFolder("Assets/Sprites/Tank Walking Right");
        if (tankWalkUpDown.Length == 0 && tankWalkRight.Length == 0)
            Debug.LogWarning("[SpriteSetup] Heavy tank body frames not found. Put frames in Assets/Sprites/TankWalking and Assets/Sprites/Tank Walking Right, then run step 1 + step 2 again.");
        Sprite heavyHead = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/sHeavyHead.png");
        Sprite heavyHeadRight = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/sHeavyHead_Right.png");
        ApplyHeavyEnemySprites("Assets/Prefabs/Enemy_Heavy.prefab", tankWalkUpDown, tankWalkRight, heavyHead, heavyHeadRight);

        // Big bat: normal flight + bite one-shot on contact
        Sprite[] bigBatFast = LoadSpritesInFolder("Assets/Sprites/big Bat Fast");
        Sprite[] bigBatBite = LoadSpritesInFolder("Assets/Sprites/big Bat Fast Biting");
        ApplyBigBatSprites("Assets/Prefabs/Enemy_BigBat.prefab", bigBatFast, bigBatBite, deathSprite);
        
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
        // Prefer sMap2.png for arena 2 floor; fall back to the red variant if not present
        Sprite map2 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/sMap2.png");
        theme.arena2FloorSprite = map2 != null ? map2 : AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/sMap_Arena2_Red.png");

        // Background sprites
        theme.arena1BgSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/sBg.png");
        theme.arena2BgSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/sBg_Red.png");

        // Trap animations (use unified spike trap frames for both arenas)
        Sprite[] spikeFrames = LoadSpikeTrapFrames();
        theme.zapTrapBlueFrames = spikeFrames;
        theme.zapTrapRedFrames = spikeFrames;
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
            if (e is HeavyEnemy) continue; // Heavy uses prefab Body+Head+HeavyEnemyVisualController, not SpriteCharacter
            Color tint = Color.white;
            Sprite[] frames = enemy;
            if (e is FastEnemy) { tint = new Color(1f, 0.9f, 0.25f); }
            else if (e is BossEnemy) { tint = new Color(0.7f, 0.2f, 1f); }
            AddSpriteComponent(e.gameObject, frames, deathSprite, tint);
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

        // Apply spike trap sprites to all traps in scene (same frames for both arenas, from Assets/Sprites/Spike Trap)
        Sprite[] spikeFrames = LoadSpikeTrapFrames();
        int trapCount = 0;
        foreach (ArenaTrap trap in Resources.FindObjectsOfTypeAll<ArenaTrap>())
        {
            if (trap == null) continue;
            // Skip prefabs/assets, only operate on scene objects
            if (EditorUtility.IsPersistent(trap.gameObject)) continue;

            Transform visual = trap.transform.Find("TrapVisual");
            if (visual == null)
            {
                GameObject v = new GameObject("TrapVisual");
                v.transform.SetParent(trap.transform, false);
                v.transform.localPosition = Vector3.zero;
                visual = v.transform;
            }

            // Clean any old sprite renderers / children
            foreach (Transform child in visual)
            {
                if (child.name == "Trap_Sprite")
                    Object.DestroyImmediate(child.gameObject);
            }
            SpriteRenderer existingSr = visual.GetComponent<SpriteRenderer>();
            if (existingSr != null) Object.DestroyImmediate(existingSr);

            TrapSpriteAnimator anim = visual.GetComponent<TrapSpriteAnimator>();
            if (anim == null) anim = visual.gameObject.AddComponent<TrapSpriteAnimator>();
            anim.frameRate = 18f;
            anim.sortingOrder = 2;
            anim.playOnAwake = false; // stay on first frame until trap activates it
            anim.SetFrames(spikeFrames);

            // Wire animator back to ArenaTrap so it can toggle activation.
            SerializedObject soTrap = new SerializedObject(trap);
            soTrap.FindProperty("spriteAnimator").objectReferenceValue = anim;
            soTrap.ApplyModifiedPropertiesWithoutUndo();

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

    private static Sprite[] LoadSpikeTrapFrames()
    {
        // Frames placed as individual PNGs under Assets/Sprites/Spike Trap/0.png,1.png,...
        // Ensure each texture is imported as a Sprite first.
        string dir = "Assets/Sprites/Spike Trap";
        string[] fileNames = System.IO.Directory.GetFiles(dir, "*.png");
        foreach (string fsPath in fileNames)
        {
            string unityPath = fsPath.Replace("\\", "/");
            TextureImporter imp = AssetImporter.GetAtPath(unityPath) as TextureImporter;
            if (imp != null && imp.textureType != TextureImporterType.Sprite)
            {
                imp.textureType = TextureImporterType.Sprite;
                imp.spriteImportMode = SpriteImportMode.Single;
                imp.filterMode = FilterMode.Point;
                imp.spritePixelsPerUnit = GlobalPPU;
                imp.mipmapEnabled = false;
                imp.textureCompression = TextureImporterCompression.Uncompressed;
                imp.crunchedCompression = false;
                imp.npotScale = TextureImporterNPOTScale.None;
                imp.alphaIsTransparency = true;
                imp.SaveAndReimport();
            }
        }

        var list = new List<Sprite>();
        for (int i = 0; i < 32; i++)
        {
            string path = $"{dir}/{i}.png";
            Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (s != null) list.Add(s);
        }
        if (list.Count == 0)
        {
            Debug.LogWarning("[SpriteSetup] No spike trap frames found in Assets/Sprites/Spike Trap; traps will stay on default frame.");
            return System.Array.Empty<Sprite>();
        }
        Debug.Log($"[SpriteSetup] Loaded {list.Count} spike trap frame(s) from '{dir}'.");
        return list.ToArray();
    }
    
    // ── Helper Methods ────────────────────────────────────────────────────
    
    private static void SliceSpriteSheet(string path, int frameCount)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return;
        
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.filterMode = FilterMode.Point;
        importer.spritePixelsPerUnit = GlobalPPU;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.crunchedCompression = false;
        importer.alphaSource = TextureImporterAlphaSource.FromInput;
        importer.alphaIsTransparency = true;
        importer.npotScale = TextureImporterNPOTScale.None;
        
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
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.crunchedCompression = false;
        importer.npotScale = TextureImporterNPOTScale.None;

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
        // With GlobalPPU=20, sprites are already larger in world units; keep scale at 1 for consistency.
        spriteChar.spriteScale = Vector3.one;
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
        spriteChar.spriteScale = Vector3.one;
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

    private static void ApplyHeavyEnemySprites(string prefabPath, Sprite[] bodyUpDownFrames, Sprite[] bodyRightFrames, Sprite headUpDown, Sprite headRight)
    {
        using (var scope = new PrefabUtility.EditPrefabContentsScope(prefabPath))
        {
            GameObject root = scope.prefabContentsRoot;
            Transform bodyT = root.transform.Find("Body");
            if (bodyT == null)
                bodyT = root.transform.Find("VisualRoot/Body");
            if (bodyT != null)
            {
                SpriteRenderer bodySr = bodyT.GetComponent<SpriteRenderer>();
                if (bodySr != null)
                {
                    // Set an initial sprite so it shows up immediately in prefab preview/play mode
                    if (bodyUpDownFrames != null && bodyUpDownFrames.Length > 0)
                        bodySr.sprite = bodyUpDownFrames[0];
                    else if (bodyRightFrames != null && bodyRightFrames.Length > 0)
                        bodySr.sprite = bodyRightFrames[0];
                    bodySr.sortingOrder = 12; // Above head; also above floor/obstacles
                }
                Transform headT = bodyT.Find("Head");
                if (headT != null)
                {
                    SpriteRenderer headSr = headT.GetComponent<SpriteRenderer>();
                    if (headSr != null)
                        headSr.sortingOrder = 11;
                }
            }
            var vis = root.GetComponent<HeavyEnemyVisualController>();
            if (vis != null)
            {
                if (bodyUpDownFrames != null && bodyUpDownFrames.Length > 0) vis.bodyUpDownFrames = bodyUpDownFrames;
                if (bodyRightFrames != null && bodyRightFrames.Length > 0) vis.bodyRightFrames = bodyRightFrames;
                if (headUpDown != null) vis.headUpDownSprite = headUpDown;
                if (headRight != null) vis.headRightSprite = headRight;
                vis.deathSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/sEnemyDead.png");
            }
            Debug.Log($"[SpriteSetup] Applied body + head sprites to Heavy enemy prefab");
        }
    }

    private static void ApplyBigBatSprites(string prefabPath, Sprite[] normalFrames, Sprite[] biteFrames, Sprite deathSprite)
    {
        using (var scope = new PrefabUtility.EditPrefabContentsScope(prefabPath))
        {
            GameObject root = scope.prefabContentsRoot;

            BatEnemyVisualController vis = root.GetComponent<BatEnemyVisualController>();
            if (vis == null) vis = root.AddComponent<BatEnemyVisualController>();
            vis.normalFrames = normalFrames;
            vis.biteFrames = biteFrames;
            vis.deathSprite = deathSprite;
            // Slightly lower so it "sits" closer to the ground visually
            vis.spriteLocalOffset = new Vector3(0f, -0.06f, 0f);

            Debug.Log($"[SpriteSetup] Applied normal + bite sprites to BigBat prefab");
        }
    }

    private static void ApplyBossSprites(
        string prefabPath,
        Sprite[] bodyWalk,
        Sprite[] bodyWalkRight,
        Sprite[] bodyAttack,
        Sprite[] headWalk,
        Sprite[] headAttack,
        Sprite deathSprite)
    {
        using (var scope = new PrefabUtility.EditPrefabContentsScope(prefabPath))
        {
            GameObject root = scope.prefabContentsRoot;

            // Ensure no SpriteCharacter is left over from older setup
            SpriteCharacter sc = root.GetComponent<SpriteCharacter>();
            if (sc != null) Object.DestroyImmediate(sc);

            BossVisualController vis = root.GetComponent<BossVisualController>();
            if (vis == null) vis = root.AddComponent<BossVisualController>();
            vis.bodyWalkUpDown = bodyWalk;
            vis.bodyWalkRight = bodyWalkRight;
            vis.bodyAttackFrames = bodyAttack;
            vis.headWalkFrames = headWalk;
            vis.headAttackFrames = headAttack;
            vis.deathSprite = deathSprite;

            // Set initial sprites so prefab preview isn't blank
            Transform bodyT = root.transform.Find("Body");
            if (bodyT != null)
            {
                SpriteRenderer bodySr = bodyT.GetComponent<SpriteRenderer>();
                if (bodySr != null)
                {
                    if (bodyWalk != null && bodyWalk.Length > 0) bodySr.sprite = bodyWalk[0];
                    else if (bodyWalkRight != null && bodyWalkRight.Length > 0) bodySr.sprite = bodyWalkRight[0];
                    bodySr.sortingOrder = 20;
                }
                Transform headT = bodyT.Find("Head");
                if (headT != null)
                {
                    SpriteRenderer headSr = headT.GetComponent<SpriteRenderer>();
                    if (headSr != null)
                    {
                        if (headWalk != null && headWalk.Length > 0) headSr.sprite = headWalk[0];
                        else if (headAttack != null && headAttack.Length > 0) headSr.sprite = headAttack[0];
                        headSr.sortingOrder = 21;
                    }
                }
            }

            Debug.Log("[SpriteSetup] Applied boss body+head sprites to Boss prefab");
        }
    }

    // ── Satan Sprite Application ──────────────────────────────────────────

    private static void ApplySatanSprites()
    {
        const string base_dir = "Assets/Sprites/Final Boss";
        const string prefabPath = "Assets/Prefabs/Enemy_Satan.prefab";
        const string bulletPrefabPath = "Assets/Prefabs/SatanBullet.prefab";

        if (!System.IO.File.Exists(prefabPath))
        {
            Debug.LogWarning("[SpriteSetup] Enemy_Satan.prefab not found. Run CS4483 → 3 - Create Prefabs first.");
            return;
        }

        // Load all frame arrays (numerically sorted so 10 comes after 9)
        Sprite[] awakening   = LoadSatanFrames($"{base_dir}/Satan Awakening");
        Sprite[] death       = LoadSatanFrames($"{base_dir}/Satan Death");
        Sprite[] direct      = LoadSatanFrames($"{base_dir}/Satan Direct Attack");
        Sprite[] downBeam    = LoadSatanFrames($"{base_dir}/Satan Down Beam Attack");
        Sprite[] fan         = LoadSatanFrames($"{base_dir}/Satan Fan Attack or Dual Hand Beam Attack");
        Sprite   footSprite  = AssetDatabase.LoadAssetAtPath<Sprite>($"{base_dir}/Satan Foot.png");
        Sprite   bulletSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{base_dir}/Bullets.png");

        // Report what was found
        Debug.Log($"[SpriteSetup] Satan frames — Awakening:{awakening.Length} Death:{death.Length} " +
                  $"Direct:{direct.Length} DownBeam:{downBeam.Length} Fan:{fan.Length} " +
                  $"Foot:{(footSprite != null ? "OK" : "MISSING")} Bullet:{(bulletSprite != null ? "OK" : "MISSING")}");

        // ── Apply to Enemy_Satan prefab ────────────────────────────────────
        using (var scope = new PrefabUtility.EditPrefabContentsScope(prefabPath))
        {
            GameObject root = scope.prefabContentsRoot;
            if (root == null)
            {
                Debug.LogWarning("[SpriteSetup] Could not open Enemy_Satan.prefab — skipping Satan sprite application.");
            }
            else
            {
                SatanAnimationController anim = root.GetComponent<SatanAnimationController>();
                if (anim == null)
                {
                    Debug.LogWarning("[SpriteSetup] SatanAnimationController not found on Enemy_Satan prefab.");
                }
                else
                {
                    var so = new SerializedObject(anim);

                    SetSpriteArray(so, "awakeningFrames",    awakening);
                    // Idle loops the last awakening frame while in combat
                    SetSpriteArray(so, "idleFrames",         awakening.Length > 0 ? new[] { awakening[awakening.Length - 1] } : new Sprite[0]);
                    SetSpriteArray(so, "directAttackFrames", direct);
                    SetSpriteArray(so, "downBeamFrames",     downBeam);
                    SetSpriteArray(so, "fanAttackFrames",    fan);
                    SetSpriteArray(so, "dualHandBeamFrames", fan); // same folder covers dual hand beam
                    SetSpriteArray(so, "deathFrames",        death);

                so.ApplyModifiedPropertiesWithoutUndo();
                Debug.Log("[SpriteSetup] ✓ Applied Satan animation sprites to Enemy_Satan prefab.");
            }

            // Wire bullet sprite into SatanAttacks so bullets show Bullets.png at runtime
            SatanAttacks satanAttacks = root.GetComponent<SatanAttacks>();
            if (satanAttacks != null && bulletSprite != null)
            {
                var soAtk = new SerializedObject(satanAttacks);
                soAtk.FindProperty("bulletSprite").objectReferenceValue = bulletSprite;
                soAtk.ApplyModifiedPropertiesWithoutUndo();
                Debug.Log("[SpriteSetup] ✓ Wired Bullets.png into SatanAttacks.bulletSprite.");
            }

                // Wire foot sprite into SatanFootPhase
                SatanFootPhase footPhase = root.GetComponent<SatanFootPhase>();
                if (footPhase != null && footSprite != null)
                {
                    var soFoot = new SerializedObject(footPhase);
                    soFoot.FindProperty("footSprite").objectReferenceValue = footSprite;
                    soFoot.ApplyModifiedPropertiesWithoutUndo();
                    Debug.Log("[SpriteSetup] ✓ Applied Satan Foot sprite to SatanFootPhase.");
                }
            }
        }

        // ── Apply Bullets.png to SatanBullet prefab ───────────────────────
        if (bulletSprite == null)
        {
            Debug.LogWarning("[SpriteSetup] Bullets.png not loaded as sprite. Run Step 1 (Slice Sprite Sheets) first.");
        }
        else
        {
            string bulletGUID = AssetDatabase.AssetPathToGUID(bulletPrefabPath);
            if (string.IsNullOrEmpty(bulletGUID))
            {
                Debug.LogWarning("[SpriteSetup] SatanBullet.prefab not found in AssetDatabase. Run CS4483 → 3 - Create Prefabs first.");
            }
            else
            {
                using (var scope = new PrefabUtility.EditPrefabContentsScope(bulletPrefabPath))
                {
                    GameObject root = scope.prefabContentsRoot;
                    if (root == null)
                    {
                        Debug.LogWarning("[SpriteSetup] Could not open SatanBullet.prefab — skipping bullet sprite.");
                    }
                    else
                    {
                        SpriteRenderer sr = root.GetComponent<SpriteRenderer>();
                        if (sr == null) sr = root.AddComponent<SpriteRenderer>();
                        if (sr != null)
                        {
                            sr.sprite       = bulletSprite;
                            sr.sortingOrder = 15;
                        }
                        if (root.GetComponent<Billboard>() == null)
                            root.AddComponent<Billboard>();
                        Debug.Log("[SpriteSetup] ✓ Applied Bullets.png to SatanBullet prefab.");
                    }
                }
            }
        }
    }

    /// <summary>
    /// Load sprites from a folder sorted numerically by filename (0, 1, 2 … 9, 10, 11)
    /// instead of alphabetically (0, 1, 10, 11, 2 …).
    /// </summary>
    private static Sprite[] LoadSatanFrames(string folderPath)
    {
        if (!AssetDatabase.IsValidFolder(folderPath)) return new Sprite[0];

        string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { folderPath });
        var list = new List<(int idx, Sprite sprite)>();

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (s == null) continue;

            string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
            int.TryParse(fileName, out int idx);
            list.Add((idx, s));
        }

        list.Sort((a, b) => a.idx.CompareTo(b.idx));
        Sprite[] result = new Sprite[list.Count];
        for (int i = 0; i < list.Count; i++) result[i] = list[i].sprite;
        return result;
    }

    /// <summary>Set a Sprite[] serialized property from a managed array.</summary>
    private static void SetSpriteArray(SerializedObject so, string propertyName, Sprite[] sprites)
    {
        SerializedProperty prop = so.FindProperty(propertyName);
        if (prop == null)
        {
            Debug.LogWarning($"[SpriteSetup] Property '{propertyName}' not found on {so.targetObject.name}.");
            return;
        }
        prop.arraySize = sprites.Length;
        for (int i = 0; i < sprites.Length; i++)
            prop.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
    }

    // ─────────────────────────────────────────────────────────────────────

    private static Sprite[] LoadSpritesInFolder(string folderPath)
    {
        if (!AssetDatabase.IsValidFolder(folderPath))
            return new Sprite[0];

        string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { folderPath });
        var sprites = new List<Sprite>(guids.Length);
        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (s != null) sprites.Add(s);
        }

        sprites.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        return sprites.ToArray();
    }

    private static void ConfigureSpritesInFolder(string folderPath)
    {
        if (!AssetDatabase.IsValidFolder(folderPath))
            return;

        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folderPath });
        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null) continue;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Point;
            importer.spritePixelsPerUnit = GlobalPPU;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.crunchedCompression = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 8192;
            importer.SaveAndReimport();
        }
    }
    
    private static void ConfigureSingleSprite(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return;
        
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.filterMode = FilterMode.Point;
        importer.spritePixelsPerUnit = GlobalPPU;
        importer.alphaSource = TextureImporterAlphaSource.FromInput; // Preserve alpha channel
        importer.alphaIsTransparency = true; // Enable transparency
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.crunchedCompression = false;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.maxTextureSize = (path.Contains("sMap") || path.Contains("sBg")) ? 8192 : 4096;
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
