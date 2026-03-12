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
    private static Material matFloor, matWall, matHub, matBoss, matObstacle;

    private const string ObstacleSpritePath = "Assets/Sprites/sObstacleBox.png";
    private static UnityEngine.Sprite s_obstacleSprite;
    const string Arena2RootName = "=== LEVEL (ProBuilder) Arena2 ===";
    const string Arena1RootName = "=== LEVEL (ProBuilder) ===";
    const float ArenaRadius = 38f;   // Circular wall radius (diameter = 76)
    const float ArenaFloorSize = 78f; // Floor cube size to contain the circle

    [MenuItem("CS4483/1 - Build ProBuilder Graybox Level")]
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
        CreateRockObstacles();
        CreateTraps();
        CreateSpawnPoints();
        CreateDeadBodies();
        CreateLighting();

        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("[ProBuilderLevelBuilder] ✓ ProBuilder graybox level built. Save scene (Ctrl+S) and bake NavMesh.");
    }

    [MenuItem("CS4483/2 - Build Arena 2 (for wave 6+)")]
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
        CreateCircularWalls(walls.transform, matWall2, ArenaRadius, 8f, 48);

        GameObject rocks = new GameObject("Rock_Obstacles");
        rocks.transform.SetParent(levelRoot);
        CreateObstacleSprite(rocks.transform, "Box1", new Vector3(10f, 0f, 10f),  2f);
        CreateObstacleSprite(rocks.transform, "Box2", new Vector3(-10f, 0f, -12f), 2.25f);
        CreateObstacleSprite(rocks.transform, "Box3", new Vector3(15f, 0f, -3f),  2.25f);
        CreateObstacleSprite(rocks.transform, "Box4", new Vector3(-14f, 0f, 6f),  2.5f);
        CreateObstacleSprite(rocks.transform, "Box5", new Vector3(0f, 0f, 8f),    2.5f);

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

    static void EnsureMaterials()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            AssetDatabase.CreateFolder("Assets", "Materials");

        matFloor    = GetOrCreateMat("M_Floor",    new Color(0.60f, 0.60f, 0.60f));
        // Dark metal-ish border walls to match floor edge
        matWall     = GetOrCreateMat("M_Wall",     new Color(0.16f, 0.18f, 0.22f));
        matHub      = GetOrCreateMat("M_Hub",      new Color(0.85f, 0.85f, 0.85f));
        matBoss     = GetOrCreateMat("M_Boss",     new Color(0.40f, 0.02f, 0.02f));
        matObstacle = GetOrCreateMat("M_Obstacle", new Color(0.45f, 0.40f, 0.35f));
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

    /// <summary>Create a circular boundary wall from segments. radius and wallHeight in world units.</summary>
    static void CreateCircularWalls(Transform parent, Material wallMat, float radius = 25f, float wallHeight = 8f, int segments = 48)
    {
        float angleStep = 360f / segments;
        float segmentWidth = (2f * Mathf.PI * radius) / segments;
        float wallThickness = 0.5f;
        for (int i = 0; i < segments; i++)
        {
            float angleDeg = i * angleStep;
            float angleRad = angleDeg * Mathf.Deg2Rad;
            float x = radius * Mathf.Cos(angleRad);
            float z = radius * Mathf.Sin(angleRad);
            Vector3 pos = new Vector3(x, wallHeight * 0.5f, z);
            Vector3 size = new Vector3(segmentWidth, wallHeight, wallThickness);
            GameObject seg = PBCube($"Wall_Seg_{i}", pos, size, wallMat, parent);
            seg.transform.rotation = Quaternion.Euler(0f, 90f - angleDeg, 0f);
        }
    }

    // ── Boundary Walls ────────────────────────────────────────────────────

    static void CreateBoundaryWalls()
    {
        GameObject p = new GameObject("Boundary_Walls");
        p.transform.SetParent(levelRoot);
        CreateCircularWalls(p.transform, matWall, ArenaRadius, 8f, 48);
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
            importer.spritePixelsPerUnit = 40f; // 40x40 sprite = 1 world unit at scale 1
            importer.alphaIsTransparency = true;
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
        // 40 PPU: 40px sprite at scale 1 = 1 world unit. Slightly smaller than before.
        float visualSize = scaleXZ * 1.8f;
        GameObject visual = new GameObject("Visual");
        visual.transform.SetParent(go.transform);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        visual.transform.localScale = new Vector3(visualSize, visualSize, 1f);

        SpriteRenderer sr = visual.AddComponent<SpriteRenderer>();
        sr.sprite = s_obstacleSprite;
        sr.sortingOrder = 1; // above floor/map
        visual.AddComponent<Billboard>();

        // Collider on parent (upright, not rotated): blocks player (CharacterController) and enemies.
        // Size is in local space. We scale X/Z to match visual footprint, and keep Y as a reasonable height.
        BoxCollider col = go.AddComponent<BoxCollider>();
        col.size = new Vector3(visualSize, 1.2f, visualSize);
        col.center = new Vector3(0f, col.size.y * 0.5f, 0f); // sit on ground

        // No Rigidbody: static collider so both CharacterController (player) and Rigidbody (enemies) collide with boxes
        go.layer = LayerMask.NameToLayer("Default");

        GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);
    }

    static void CreateRockObstacles()
    {
        GameObject p = new GameObject("Rock_Obstacles");
        p.transform.SetParent(levelRoot);
        
        CreateObstacleSprite(p.transform, "Box1", new Vector3(-8, 0, 5),   2f);
        CreateObstacleSprite(p.transform, "Box2", new Vector3(6, 0, 8),    2.25f);
        CreateObstacleSprite(p.transform, "Box3", new Vector3(-10, 0, -8), 2.25f);
        CreateObstacleSprite(p.transform, "Box4", new Vector3(12, 0, -5),  2.5f);
        CreateObstacleSprite(p.transform, "Box5", new Vector3(5, 0, -10),  2.5f);
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

            // Visual will be added by SpriteSetup (animated zap trap sprites)
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
        dirLight.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        Light dir = dirLight.AddComponent<Light>();
        dir.type = LightType.Directional;
        dir.color = Color.white;
        dir.intensity = 1.0f;
    }

    // ── Raised Platforms (removed - no jump mechanic) ────────────────────
    
    static void CreateRaisedPlatforms()
    {
        // Intentionally empty - removed due to no jump mechanic in game
    }
}
