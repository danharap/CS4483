using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.Shapes;
using Unity.AI.Navigation;

/// <summary>
/// Editor tool: CS4483 → Build ProBuilder Graybox Level
/// Creates the complete graybox scene geometry using ProBuilder API:
///   floor, boundary walls, central hub, boss arena, rock obstacles, spawn points, lights.
/// Uses ProBuilder meshes instead of primitives for proper graybox prototyping.
/// </summary>
public static class ProBuilderLevelBuilder
{
    private static Transform levelRoot;
    private static Material matFloor, matWall, matHub, matBoss, matObstacle, matStands, matStandsArena2;

    private const string ObstacleSpritePath = "Assets/Sprites/sBox.png";
    private static UnityEngine.Sprite s_obstacleSprite;
    const string Arena2RootName  = "=== LEVEL (ProBuilder) Arena2 ===";
    const string Arena1RootName  = "=== LEVEL (ProBuilder) ===";
    const string LobbyRootName   = "=== LEVEL (Lobby) ===";
    const float ArenaRadius = 38f;   // Circular wall radius (diameter = 76)
    const float ArenaFloorSize = 78f; // Floor cube size to contain the circle

    // sBox cover props: mid-arena ring (~25–28m from center), same in Arena 1 and Arena 2.
    static readonly Vector3[] ObstacleBoxPositions =
    {
        new Vector3(-23f, 0f, 15f),
        new Vector3(18f, 0f, 20f),
        new Vector3(-20f, 0f, -18f),
        new Vector3(22f, 0f, -12f),
        new Vector3(8f, 0f, -25f),
    };
    static readonly float[] ObstacleBoxScalesXZ = { 2f, 2.25f, 2.25f, 2.5f, 2.5f };

    // Called from SetupAll; no menu item (use Full Setup instead)
    public static void BuildLevel()
    {
        Scene scene = EditorSceneManager.GetActiveScene();
        Debug.Log($"[ProBuilderLevelBuilder] Building ProBuilder graybox level in scene: {scene.name}");

        EnsureMaterials();

        DestroyArena1IfExists();

        GameObject root = new GameObject("=== LEVEL (ProBuilder) ===");
        levelRoot = root.transform;

        CreateFloor();
        CreateBoundaryWalls();
        CreateAudienceStands();
        CreateRockObstacles();
        CreateTraps();
        CreateSpawnPoints();
        CreateDeadBodies();
        CreateLighting();

        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("[ProBuilderLevelBuilder] ✓ ProBuilder graybox level built. Save scene (Ctrl+S) and bake NavMesh.");
    }

    // Called from SetupAll; no menu item (use Full Setup instead)
    public static void BuildArena2()
    {
        Scene scene = EditorSceneManager.GetActiveScene();
        Debug.Log("[ProBuilderLevelBuilder] Building Arena 2...");

        EnsureMaterials();
        Material matFloor2 = GetOrCreateMat("M_Floor_Arena2",  new Color(0.25f, 0.22f, 0.35f));
        // Arena 2 walls: similar dark metal tone so the circular border matches the map edge
        Material matWall2  = GetOrCreateMat("M_Wall_Arena2",   new Color(0.18f, 0.20f, 0.24f));

        DestroyArena2IfExists();

        GameObject root = new GameObject("=== LEVEL (ProBuilder) Arena2 ===");
        root.SetActive(false);
        levelRoot = root.transform;

        PBCube("Floor", new Vector3(0f, -0.2f, 0f), new Vector3(ArenaFloorSize, 0.4f, ArenaFloorSize), matFloor2, levelRoot);

        GameObject walls = new GameObject("Boundary_Walls");
        walls.transform.SetParent(levelRoot);
        CreateOctagonWalls(walls.transform, matWall2, ArenaRadius, 8f, 0.5f);

        // Same stepped stands as Arena 1 (crowd / layout parity); nether-tinted material
        CreateAudienceStands(matStandsArena2);

        // No sBox cover props in Stage 2 (open arena)

        CreateTraps();
        CreateSpawnPoints();
        CreateLighting();

        if (root.GetComponent<NavMeshSurface>() == null)
            root.AddComponent<NavMeshSurface>();

        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("[ProBuilderLevelBuilder] ✓ Arena 2 built (disabled). Run Setup All to wire spawn points.");
    }

    // ── Materials ─────────────────────────────────────────────────────────

    static void DestroyArena1IfExists()
    {
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (root.name == Arena1RootName)
            {
                Object.DestroyImmediate(root);
                Debug.Log("[ProBuilderLevelBuilder] Removed existing Arena 1 (level root).");
            }
        }
    }

    static void DestroyArena2IfExists()
    {
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (root.name == Arena2RootName)
            {
                Object.DestroyImmediate(root);
                Debug.Log("[ProBuilderLevelBuilder] Removed existing Arena 2.");
            }
        }
    }

    static void DestroyLobbyIfExists()
    {
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (root.name == LobbyRootName)
            {
                Object.DestroyImmediate(root);
                Debug.Log("[ProBuilderLevelBuilder] Removed existing Lobby level root.");
            }
        }
    }

    static void EnsureMaterials()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            AssetDatabase.CreateFolder("Assets", "Materials");

        matFloor    = GetOrCreateMat("M_Floor",    new Color(0.60f, 0.60f, 0.60f));
        // Warm earthy brown stone for colosseum-style arena walls (Stage 1)
        matWall     = GetOrCreateMat("M_Wall",     new Color(0.55f, 0.35f, 0.15f));
        matHub      = GetOrCreateMat("M_Hub",      new Color(0.85f, 0.85f, 0.85f));
        matBoss     = GetOrCreateMat("M_Boss",     new Color(0.40f, 0.02f, 0.02f));
        matObstacle = GetOrCreateMat("M_Obstacle", new Color(0.45f, 0.40f, 0.35f));
        // Warm neutral grey stone for Stage 1 audience stands — distinct from walls, fits sMap floor
        matStands   = GetOrCreateMat("M_Stands",   new Color(0.48f, 0.46f, 0.42f));
        matStandsArena2 = GetOrCreateMat("M_Stands_Arena2", new Color(0.16f, 0.07f, 0.09f)); // nether / darker stone
    }

    static Material GetOrCreateMat(string name, Color color)
    {
        string path = $"Assets/Materials/{name}.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Standard")) { color = color };
            AssetDatabase.CreateAsset(mat, path);
        }
        else
        {
            // Always sync the color so re-running the builder reflects updated values
            mat.color = color;
            EditorUtility.SetDirty(mat);
        }
        return mat;
    }

    // ── ProBuilder Helpers ────────────────────────────────────────────────

    static GameObject PBCube(string name, Vector3 position, Vector3 size, Material material, Transform parent)
    {
        ProBuilderMesh pbMesh = ShapeGenerator.CreateShape(ShapeType.Cube);
        GameObject go = pbMesh.gameObject;
        go.name = name;
        go.transform.SetParent(parent);
        go.transform.position = position;
        go.transform.localScale = size;

        pbMesh.ToMesh();
        pbMesh.Refresh();

        // Apply material after mesh is final so it sticks to the rendered mesh
        if (material != null)
        {
            Renderer r = go.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = material;
        }

        // Add collider for physics
        MeshCollider collider = go.AddComponent<MeshCollider>();
        collider.sharedMesh = go.GetComponent<MeshFilter>().sharedMesh;
        collider.convex = false;

        // Mark as static for NavMesh baking
        GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);

        return go;
    }

    // ── Floor ─────────────────────────────────────────────────────────────

    static void CreateFloor()
    {
        GameObject floor = PBCube("Floor", new Vector3(0f, -0.2f, 0f), new Vector3(ArenaFloorSize, 0.4f, ArenaFloorSize), matFloor, levelRoot);
    }

    // ── Lobby Level Builder ───────────────────────────────────────────────

    // Called from SetupAll; builds a dedicated === LEVEL (Lobby) === root.
    public static void BuildLobbyLevel()
    {
        Scene scene = EditorSceneManager.GetActiveScene();
        Debug.Log("[ProBuilderLevelBuilder] Building Lobby level...");

        EnsureMaterials();
        DestroyLobbyIfExists();

        GameObject root = new GameObject(LobbyRootName);
        levelRoot = root.transform;

        CreateLobbyGeometry();
        CreateLighting();

        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("[ProBuilderLevelBuilder] ✓ Lobby level built.");
    }

    /// <summary>
    /// Create an 8‑sided arena (regular octagon) with a flat top edge.
    /// apothem = distance from center to the middle of each side (in world units).
    /// </summary>
    static void CreateOctagonWalls(Transform parent, Material wallMat, float apothem = 25f, float wallHeight = 8f, float wallThickness = 0.5f)
    {
        const int sides = 8;
        float angleStep = 360f / sides;                 // 45°
        float halfInteriorAngleRad = Mathf.PI / sides;  // π/8

        // Side length for a regular polygon from apothem: s = 2 * a * tan(π/n)
        float sideLength = 2f * apothem * Mathf.Tan(halfInteriorAngleRad);

        for (int i = 0; i < sides; i++)
        {
            // Flat edge at the top: center of top side at (0, 0, +apothem)
            float angleDeg = i * angleStep;
            float angleRad = angleDeg * Mathf.Deg2Rad;

            float x = apothem * Mathf.Sin(angleRad);
            float z = apothem * Mathf.Cos(angleRad);

            Vector3 pos = new Vector3(x, wallHeight * 0.5f, z);
            Vector3 size = new Vector3(sideLength, wallHeight, wallThickness);

            GameObject seg = PBCube($"Wall_Seg_{i}", pos, size, wallMat, parent);

            // Long axis of the wall runs tangentially around the arena:
            // rotate so the X axis is tangent (perpendicular to the radius vector).
            seg.transform.rotation = Quaternion.Euler(0f, angleDeg, 0f);
        }
    }

    // ── Boundary Walls ────────────────────────────────────────────────────

    static void CreateBoundaryWalls()
    {
        GameObject p = new GameObject("Boundary_Walls");
        p.transform.SetParent(levelRoot);
        CreateOctagonWalls(p.transform, matWall, ArenaRadius, 8f, 0.5f);
    }

    // ── Audience Stands (decorative colosseum seating) ────────────────────

    static void CreateAudienceStands()
    {
        CreateAudienceStands(matStands);
    }

    static void CreateAudienceStands(Material standMaterial)
    {
        // Decorative only: no gameplay collision, just dark stepped stone rows behind the walls.
        Transform wallsParent = levelRoot.Find("Boundary_Walls");
        if (wallsParent == null)
        {
            Debug.LogWarning("[ProBuilderLevelBuilder] Boundary_Walls not found; audience stands not created.");
            return;
        }

        GameObject standsRoot = new GameObject("Audience_Stands");
        standsRoot.transform.SetParent(levelRoot);

        // For each wall segment, build a few shallow steps just outside the wall, following its length.
        const int stepsPerRing = 14;
        const float stepHeight = 0.6f;
        const float stepDepth  = 1.0f;
        const float gapBehindWall = 0.5f; // small gap between wall and first row

        foreach (Transform wall in wallsParent)
        {
            // Wall center on XZ plane
            Vector3 wallPos = wall.position;
            wallPos.y = 0f;

            // Outward direction away from arena center, to push stands behind the wall
            Vector3 outward = (wallPos - Vector3.zero);
            outward.y = 0f;
            if (outward.sqrMagnitude < 0.0001f)
                continue;
            outward.Normalize();

            // Length and height of this wall segment from our PBCube sizing convention
            float length = wall.localScale.x * 1.35f; // extra overhang to fully close gaps between segments
            float wallHeight = wall.localScale.y;
            float wallTopY = wallHeight; // top of wall (center at wallHeight * 0.5f)

            for (int i = 0; i < stepsPerRing; i++)
            {
                float verticalCenter = wallTopY + (stepHeight * 0.5f) + (i * stepHeight * 0.9f);
                float radialOffset   = ArenaRadius + gapBehindWall + (i * stepDepth);

                Vector3 center = outward * radialOffset;
                center.y = verticalCenter;

                Vector3 size = new Vector3(length, stepHeight, stepDepth);
                GameObject step = PBCube($"Stand_Step_{wall.name}_{i}", center, size, standMaterial, standsRoot.transform);

                // Rotate to follow the wall tangent so the long edge lines up with the octagon edge.
                step.transform.rotation = wall.rotation;

                // Decorative only: disable collider so it doesn't affect NavMesh or gameplay.
                MeshCollider mc = step.GetComponent<MeshCollider>();
                if (mc != null) mc.enabled = false;
            }
        }
    }

    // ── Central Hub ───────────────────────────────────────────────────────

    static void CreateCentralHub()
    {
        GameObject p = new GameObject("Central_Hub");
        p.transform.SetParent(levelRoot);
        
        // Just a landmark pillar, no separate floor overlay
        GameObject pillar = PBCube("Blue_Pillar", new Vector3(0, 3, 0), new Vector3(0.6f, 6f, 0.6f), null, p.transform);
        pillar.GetComponent<Renderer>().sharedMaterial = GetOrCreateMat("M_Blue", new Color(0.1f, 0.3f, 1f));
    }

    // ── Simple Lobby (gray square + square walls + portal to arena) ───────

    // Builds lobby geometry under the current levelRoot.
    static void CreateLobbyGeometry()
    {
        GameObject lobby = new GameObject("Lobby");
        lobby.transform.SetParent(levelRoot);

        float lobbyZ = 0f;

        // Dedicated plain grey material for the lobby — never shared with arena floors
        // so EnvironmentSprites or any other pass can't accidentally replace it.
        Material matLobbyFloor = GetOrCreateMat("M_LobbyFloor", new Color(0.55f, 0.55f, 0.55f));

        // Floor — placed at y=0.15 so it renders above the global Background_Plane (y=0.01)
        // and Floor_Map (y=0.1) sprites that EnvironmentSprites places in the scene.
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "Lobby_Floor";
        floor.transform.SetParent(lobby.transform);
        floor.transform.position = new Vector3(0f, 0f, lobbyZ);
        floor.transform.localScale = new Vector3(20f, 0.4f, 20f);
        var floorRenderer = floor.GetComponent<Renderer>();
        if (floorRenderer != null) floorRenderer.sharedMaterial = matLobbyFloor;

        // Square walls around the floor
        float wallHeight = 3f;
        float halfSize = 10f;
        float thickness = 0.5f;

        // North wall (forward, where the portal will be)
        GameObject wallN = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wallN.name = "Lobby_Wall_North";
        wallN.transform.SetParent(lobby.transform);
        wallN.transform.position = new Vector3(0f, wallHeight * 0.5f, lobbyZ + halfSize);
        wallN.transform.localScale = new Vector3(20f, wallHeight, thickness);
        wallN.GetComponent<Renderer>().sharedMaterial = matWall;

        // South wall
        GameObject wallS = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wallS.name = "Lobby_Wall_South";
        wallS.transform.SetParent(lobby.transform);
        wallS.transform.position = new Vector3(0f, wallHeight * 0.5f, lobbyZ - halfSize);
        wallS.transform.localScale = new Vector3(20f, wallHeight, thickness);
        wallS.GetComponent<Renderer>().sharedMaterial = matWall;

        // East wall
        GameObject wallE = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wallE.name = "Lobby_Wall_East";
        wallE.transform.SetParent(lobby.transform);
        wallE.transform.position = new Vector3(halfSize, wallHeight * 0.5f, lobbyZ);
        wallE.transform.localScale = new Vector3(thickness, wallHeight, 20f);
        wallE.GetComponent<Renderer>().sharedMaterial = matWall;

        // West wall
        GameObject wallW = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wallW.name = "Lobby_Wall_West";
        wallW.transform.SetParent(lobby.transform);
        wallW.transform.position = new Vector3(-halfSize, wallHeight * 0.5f, lobbyZ);
        wallW.transform.localScale = new Vector3(thickness, wallHeight, 20f);
        wallW.GetComponent<Renderer>().sharedMaterial = matWall;

        // Portal embedded in the north wall, centered
        GameObject portal = GameObject.CreatePrimitive(PrimitiveType.Cube);
        portal.name = "LobbyToArena_Portal";
        portal.transform.SetParent(lobby.transform);
        portal.transform.position = new Vector3(0f, 1.5f, lobbyZ + halfSize - thickness * 0.5f - 0.01f);
        portal.transform.localScale = new Vector3(2f, 3f, 0.3f);
        var portalRenderer = portal.GetComponent<Renderer>();
        if (portalRenderer != null) portalRenderer.sharedMaterial = matHub;

        // Make portal trigger with BoxCollider + kinematic Rigidbody (no MeshCollider)
        BoxCollider box = portal.GetComponent<BoxCollider>();
        box.isTrigger = true;
        Rigidbody rb = portal.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        portal.AddComponent<LobbyPortal>();
    }

    // ── Boss Arena ────────────────────────────────────────────────────────

    static void CreateBossArena()
    {
        GameObject p = new GameObject("Boss_Arena_South");
        p.transform.SetParent(levelRoot);
        
        // Just a landmark pillar in the south, no separate floor overlay
        GameObject pillar = PBCube("Boss_Pillar", new Vector3(0, 3, -19), new Vector3(0.6f, 6f, 0.6f), matBoss, p.transform);
        pillar.GetComponent<Renderer>().sharedMaterial = matBoss;
    }

    // ── Obstacle Boxes (for cover and navigation) ─────────────────────────
    
    /// <summary>Configure the obstacle box sprite as a single sprite with correct PPU and transparency.</summary>
    static void EnsureObstacleSprite()
    {
        if (s_obstacleSprite != null) return;

        const string path = ObstacleSpritePath;
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Point;
            importer.spritePixelsPerUnit = 20f; // match GlobalPPU (40x40 sprite = 2 world units at scale 1)
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.crunchedCompression = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = 4096;
            importer.SaveAndReimport();
        }

        s_obstacleSprite = AssetDatabase.LoadAssetAtPath<UnityEngine.Sprite>(path);
    }
    
    /// <summary>Create one obstacle box using the box sprite. Same gameplay footprint as previous rocks.</summary>
    static void CreateObstacleSprite(Transform parent, string name, Vector3 position, float scaleXZ)
    {
        EnsureObstacleSprite();
        if (s_obstacleSprite == null)
        {
            Debug.LogWarning("[ProBuilderLevelBuilder] Obstacle sprite not found; skipping obstacle " + name);
            return;
        }
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.position = position;
        go.transform.rotation = Quaternion.identity;

        // Visual (sprite) as a child so we can rotate/billboard it without messing up collider orientation.
        // Use the sprite's actual pixel size and PPU (sBox.png or any replacement) so scale stays correct.
        float ppu = s_obstacleSprite.pixelsPerUnit;
        float px = Mathf.Max(s_obstacleSprite.rect.width, s_obstacleSprite.rect.height);
        float spriteWorldSize = px / ppu;
        float desiredWorldSize = scaleXZ * 1.8f;
        float visualScale = desiredWorldSize / spriteWorldSize;
        GameObject visual = new GameObject("Visual");
        visual.transform.SetParent(go.transform);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        visual.transform.localScale = new Vector3(visualScale, visualScale, 1f);

        SpriteRenderer sr = visual.AddComponent<SpriteRenderer>();
        sr.sprite = s_obstacleSprite;
        sr.sortingOrder = 1; // above floor/map
        visual.AddComponent<Billboard>();

        // Collider on parent (upright, not rotated): blocks player (CharacterController) and enemies.
        // Size is in local space. We scale X/Z to match visual footprint, and keep Y as a reasonable height.
        BoxCollider col = go.AddComponent<BoxCollider>();
        col.size = new Vector3(desiredWorldSize, 1.2f, desiredWorldSize);
        col.center = new Vector3(0f, col.size.y * 0.5f, 0f); // sit on ground

        // Cover / props: BossProjectile ignores this layer (see GameplayLayerSetup + SatanBullet).
        int propLayer = LayerMask.NameToLayer("ArenaProp");
        go.layer = propLayer >= 0 ? propLayer : 0;

        GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);
    }

    static void CreateRockObstacles()
    {
        GameObject p = new GameObject("Rock_Obstacles");
        p.transform.SetParent(levelRoot);
        PlaceObstacleBoxes(p.transform);
    }

    static void PlaceObstacleBoxes(Transform parent)
    {
        for (int i = 0; i < ObstacleBoxPositions.Length; i++)
            CreateObstacleSprite(parent, $"Box{i + 1}", ObstacleBoxPositions[i], ObstacleBoxScalesXZ[i]);
    }

    // ── Arena Traps (damage + stun on step) ─────────────────────────────────

    static void CreateTraps()
    {
        GameObject root = new GameObject("Traps");
        root.transform.SetParent(levelRoot);

        Vector3[] positions = new[]
        {
            new Vector3(10f,  0.3f,  10f),
            new Vector3(-10f, 0.3f,  10f),
            new Vector3(10f,  0.3f, -10f),
            new Vector3(-10f, 0.3f, -10f)
        };

        for (int i = 0; i < positions.Length; i++)
        {
            GameObject trap = new GameObject($"Trap_{i + 1}");
            trap.transform.SetParent(root.transform);
            trap.transform.position = positions[i];

            BoxCollider trigger = trap.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size = new Vector3(2f, 0.5f, 2f);
            trigger.center = Vector3.zero;

            ArenaTrap arenaTrap = trap.AddComponent<ArenaTrap>();

            // Visual child – sprite-based spike trap animation will be added by SpriteSetup.
            GameObject visual = new GameObject("TrapVisual");
            visual.transform.SetParent(trap.transform, false);
            visual.transform.localPosition = Vector3.zero;
        }
    }

    // ── Spawn Points ──────────────────────────────────────────────────────

    static void CreateSpawnPoints()
    {
        GameObject spawnRoot = new GameObject("SpawnPoints");
        spawnRoot.transform.SetParent(levelRoot);

        CreateSpawn(spawnRoot.transform, "Spawn_N1",  new Vector3(-5,  1,  15));
        CreateSpawn(spawnRoot.transform, "Spawn_N2",  new Vector3(5,   1,  15));
        CreateSpawn(spawnRoot.transform, "Spawn_E1",  new Vector3(15,  1,  5));
        CreateSpawn(spawnRoot.transform, "Spawn_E2",  new Vector3(15,  1, -5));
        CreateSpawn(spawnRoot.transform, "Spawn_W1",  new Vector3(-15, 1,  5));
        CreateSpawn(spawnRoot.transform, "Spawn_W2",  new Vector3(-15, 1, -5));
        CreateSpawn(spawnRoot.transform, "Spawn_Boss", new Vector3(0,   1, -18));
    }

    static void CreateSpawn(Transform parent, string name, Vector3 pos)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.position = pos;
    }

    // ── Dead Bodies (ambient props; sprite + fly sound applied by SpriteSetup / AudioSetup) ──

    static void CreateDeadBodies()
    {
        GameObject root = new GameObject("DeadBodies");
        root.transform.SetParent(levelRoot);

        // 2–3 bodies near the edges of the map (XZ ±20–24)
        CreateDeadBody(root.transform, "DeadBody_1", new Vector3(20f,  0.5f,  20f));
        CreateDeadBody(root.transform, "DeadBody_2", new Vector3(-20f, 0.5f, -18f));
        CreateDeadBody(root.transform, "DeadBody_3", new Vector3(22f,  0.5f,  -5f));
    }

    static void CreateDeadBody(Transform parent, string name, Vector3 position)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.position = position;

        SphereCollider col = go.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = 4f;

        go.AddComponent<DeadBody>();
    }

    // ── Lighting ──────────────────────────────────────────────────────────

    static void CreateLighting()
    {
        GameObject lightRoot = new GameObject("Lighting");
        lightRoot.transform.SetParent(levelRoot);

        GameObject dirLight = new GameObject("DirectionalLight");
        dirLight.transform.SetParent(lightRoot.transform);
        dirLight.transform.rotation = Quaternion.Euler(65f, -35f, 0f);
        Light dir = dirLight.AddComponent<Light>();
        dir.type = LightType.Directional;
        dir.color = Color.white;
        dir.intensity = 1.3f;
        dir.shadows = LightShadows.Soft;
    }

    // ── Raised Platforms (removed - no jump mechanic) ────────────────────
    
    static void CreateRaisedPlatforms()
    {
        // Intentionally empty - removed due to no jump mechanic in game
    }
}
