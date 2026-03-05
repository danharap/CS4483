using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.ProBuilder;

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

    [MenuItem("CS4483/1 - Build ProBuilder Graybox Level")]
    public static void BuildLevel()
    {
        Scene scene = EditorSceneManager.GetActiveScene();
        Debug.Log($"[ProBuilderLevelBuilder] Building ProBuilder graybox level in scene: {scene.name}");

        EnsureMaterials();

        GameObject root = new GameObject("=== LEVEL (ProBuilder) ===");
        levelRoot = root.transform;

        CreateFloor();
        CreateBoundaryWalls();
        CreateCentralHub();
        CreateBossArena();
        CreateRockObstacles();
        CreateSpawnPoints();
        CreateLighting();

        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("[ProBuilderLevelBuilder] ✓ ProBuilder graybox level built. Save scene (Ctrl+S) and bake NavMesh.");
    }

    // ── Materials ─────────────────────────────────────────────────────────

    static void EnsureMaterials()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            AssetDatabase.CreateFolder("Assets", "Materials");

        matFloor    = GetOrCreateMat("M_Floor",    new Color(0.60f, 0.60f, 0.60f));
        matWall     = GetOrCreateMat("M_Wall",     new Color(0.30f, 0.30f, 0.30f));
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
        
        if (material != null)
        {
            Renderer r = go.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = material;
        }
        
        pbMesh.ToMesh();
        pbMesh.Refresh();
        
        // Add collider for physics
        MeshCollider collider = go.AddComponent<MeshCollider>();
        collider.sharedMesh = go.GetComponent<MeshFilter>().sharedMesh;
        collider.convex = false;
        
        // Mark as static for NavMesh baking
        GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);
        
        return go;
    }

    static GameObject PBPlane(string name, Vector3 position, Vector3 size, Material material, Transform parent)
    {
        ProBuilderMesh pbMesh = ShapeGenerator.CreateShape(ShapeType.Plane);
        GameObject go = pbMesh.gameObject;
        go.name = name;
        go.transform.SetParent(parent);
        go.transform.position = position;
        go.transform.localScale = size;
        
        if (material != null)
        {
            Renderer r = go.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = material;
        }
        
        pbMesh.ToMesh();
        pbMesh.Refresh();
        
        // Add collider for physics (critical for floors!)
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
        // ProBuilder planes are 10x10 by default, scale to 50x50 world units
        GameObject floor = PBPlane("Floor", new Vector3(0f, 0f, 0f), new Vector3(5f, 1f, 5f), matFloor, levelRoot);
        floor.transform.rotation = Quaternion.Euler(0, 0, 0);
    }

    // ── Boundary Walls ────────────────────────────────────────────────────

    static void CreateBoundaryWalls()
    {
        GameObject p = new GameObject("Boundary_Walls");
        p.transform.SetParent(levelRoot);
        
        PBCube("Wall_North", new Vector3(0,    4,  25),  new Vector3(50f, 8f, 0.5f), matWall, p.transform);
        PBCube("Wall_South", new Vector3(0,    4, -25),  new Vector3(50f, 8f, 0.5f), matWall, p.transform);
        PBCube("Wall_East",  new Vector3(25,   4,   0),  new Vector3(0.5f, 8f, 50f), matWall, p.transform);
        PBCube("Wall_West",  new Vector3(-25,  4,   0),  new Vector3(0.5f, 8f, 50f), matWall, p.transform);
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

    // ── Rock Obstacles (for cover and navigation) ─────────────────────────

    static void CreateRockObstacles()
    {
        GameObject p = new GameObject("Rock_Obstacles");
        p.transform.SetParent(levelRoot);
        
        PBCube("Rock1", new Vector3(-8, 1, 5),   new Vector3(2f, 2f, 2f),   matObstacle, p.transform);
        PBCube("Rock2", new Vector3(6, 1, 8),    new Vector3(2.5f, 2f, 2f), matObstacle, p.transform);
        PBCube("Rock3", new Vector3(-10, 1, -8), new Vector3(2f, 2.5f, 2f), matObstacle, p.transform);
        PBCube("Rock4", new Vector3(12, 1, -5),  new Vector3(2f, 2f, 3f),   matObstacle, p.transform);
        PBCube("Rock5", new Vector3(5, 1, -10),  new Vector3(3f, 2f, 2f),   matObstacle, p.transform);
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
