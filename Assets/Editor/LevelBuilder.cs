using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Editor tool: CS4483 → Build Graybox Level
/// Creates the complete Main_Graybox scene geometry:
///   floor, boundary walls, central hub, 3 combat pockets, 2 raised platforms,
///   ramps, occlusion walls, boss arena, spawn points, landmarks, lights.
/// Run ONCE on a fresh empty scene.
/// </summary>
public static class LevelBuilder
{
    private static Transform levelRoot;

    // ── Materials (cached after creation) ────────────────────────────────
    private static Material matFloor, matWall, matHub, matGreen,
                             matYellow, matRed, matBoss, matRamp;

    [MenuItem("CS4483/1 - Build Graybox Level")]
    public static void BuildLevel()
    {
        // Create or open Main_Graybox scene
        Scene scene = EditorSceneManager.GetActiveScene();
        Debug.Log($"[LevelBuilder] Building level in scene: {scene.name}");

        EnsureMaterials();

        GameObject root = new GameObject("=== LEVEL ===");
        levelRoot = root.transform;

        CreateFloor();
        CreateBoundaryWalls();
        CreateCentralHub();
        // Removed colored pocket zones (green/yellow/red) - simplified open field design
        CreateBossArena();
        CreateRaisedPlatforms();
        CreateOcclusionWalls();
        CreateSpawnPoints();
        CreateLighting();

        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("[LevelBuilder] ✓ Level geometry built. Save the scene (Ctrl+S) and bake NavMesh.");
    }

    // ── Materials ─────────────────────────────────────────────────────────

    static void EnsureMaterials()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            AssetDatabase.CreateFolder("Assets", "Materials");

        matFloor  = GetOrCreateMat("M_Floor",    new Color(0.60f, 0.60f, 0.60f));
        matWall   = GetOrCreateMat("M_Wall",     new Color(0.30f, 0.30f, 0.30f));
        matHub    = GetOrCreateMat("M_Hub",      new Color(0.85f, 0.85f, 0.85f));
        matGreen  = GetOrCreateMat("M_Green",    new Color(0.15f, 0.70f, 0.15f));
        matYellow = GetOrCreateMat("M_Yellow",   new Color(0.85f, 0.80f, 0.10f));
        matRed    = GetOrCreateMat("M_Red",      new Color(0.75f, 0.15f, 0.15f));
        matBoss   = GetOrCreateMat("M_Boss",     new Color(0.40f, 0.02f, 0.02f));
        matRamp   = GetOrCreateMat("M_Ramp",     new Color(0.50f, 0.50f, 0.55f));
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

    // ── Floor ─────────────────────────────────────────────────────────────

    static void CreateFloor()
    {
        Box("Floor", Vector3.zero, new Vector3(50f, 0.4f, 50f), matFloor, levelRoot);
    }

    // ── Boundary Walls ────────────────────────────────────────────────────

    static void CreateBoundaryWalls()
    {
        Transform p = Child("BoundaryWalls");
        Box("Wall_N",  new Vector3( 0,  2,  25.5f), new Vector3(51, 4, 1), matWall, p);
        Box("Wall_S",  new Vector3( 0,  2, -25.5f), new Vector3(51, 4, 1), matWall, p);
        Box("Wall_E",  new Vector3( 25.5f, 2, 0),   new Vector3(1, 4, 51), matWall, p);
        Box("Wall_W",  new Vector3(-25.5f, 2, 0),   new Vector3(1, 4, 51), matWall, p);
    }

    // ── Central Hub ───────────────────────────────────────────────────────

    static void CreateCentralHub()
    {
        Transform p = Child("CentralHub");
        // Central area marker (slightly raised to prevent z-fighting)
        Box("Hub_Floor",  new Vector3(0, 0.01f, 0), new Vector3(9, 0.4f, 9), matHub, p);
        // Landmark pillar (BLUE = main wayfinding beacon)
        GameObject pillar = Box("Hub_Pillar", new Vector3(0, 4f, 0), new Vector3(1, 8, 1), null, p);
        pillar.GetComponent<Renderer>().material = GetOrCreateMat("M_Blue", new Color(0.1f, 0.3f, 1f));
    }

    // ── North Pocket (GREEN – Healing Shrine) ─────────────────────────────

    static void CreateNorthPocket()
    {
        Transform p = Child("Pocket_North_Green");
        Box("Pocket_N_Floor",  new Vector3(0, 0.01f, 15),   new Vector3(12, 0.4f, 9),  matGreen, p);
        // Landmark (GREEN pillar)
        GameObject pillar = Box("Green_Pillar", new Vector3(0, 3, 17), new Vector3(0.6f, 6, 0.6f), matGreen, p);
        // Healing shrine marker
        GameObject shrine = Box("HealingShrine", new Vector3(-3, 0.6f, 16), new Vector3(1.5f, 1.2f, 1.5f), matGreen, p);
        shrine.tag = "Untagged";
        GameObject prompt = new GameObject("ShrinePrompt_Anchor");
        prompt.transform.SetParent(shrine.transform);
        prompt.transform.localPosition = Vector3.up * 1.5f;
    }

    // ── East Pocket (YELLOW – Upgrade Area) ──────────────────────────────

    static void CreateEastPocket()
    {
        Transform p = Child("Pocket_East_Yellow");
        Box("Pocket_E_Floor",  new Vector3(16, 0.01f, 0),  new Vector3(9, 0.4f, 12), matYellow, p);
        Box("Yellow_Pillar",   new Vector3(18, 3, 0),  new Vector3(0.6f, 6, 0.6f), matYellow, p);
    }

    // ── West Pocket (RED – pre-Boss area) ────────────────────────────────

    static void CreateWestPocket()
    {
        Transform p = Child("Pocket_West_Red");
        Box("Pocket_W_Floor",  new Vector3(-16, 0.01f, 0),  new Vector3(9, 0.4f, 12), matRed, p);
        Box("Red_Pillar",      new Vector3(-18, 3, 0),  new Vector3(0.6f, 6, 0.6f), matRed, p);
    }

    // ── Boss Arena ────────────────────────────────────────────────────────

    static void CreateBossArena()
    {
        Transform p = Child("BossArena");
        Box("Boss_Floor",    new Vector3(0, 0.01f, -17),   new Vector3(14, 0.4f, 8), matBoss, p);
        // Boss landmark (Red pillar)
        Box("Boss_Pillar",   new Vector3(0, 3, -19),   new Vector3(1, 6, 1), matBoss, p);
        // Gate door (GateDoor component added by SceneSetup)
        GameObject gate = Box("GateDoor", new Vector3(0, 2, -12.8f), new Vector3(5, 4, 0.4f), matRed, p);
        gate.name = "GateDoor";
    }

    // ── Raised Platforms (REMOVED - no jump mechanic) ────────────────────

    static void CreateRaisedPlatforms()
    {
        // Removed platforms and ramps since there's no jump mechanic
        // Keeping this method to avoid breaking the setup flow
    }

    static void CreateRamp(Transform parent, string name, Vector3 foot, Vector3 head, float width)
    {
        Vector3 dir    = head - foot;
        float   horiz  = Mathf.Abs(dir.z) < 0.001f ? Mathf.Abs(dir.x) : Mathf.Abs(dir.z);
        float   vert   = dir.y;
        float   length = Mathf.Sqrt(horiz * horiz + vert * vert);
        float   angle  = Mathf.Atan2(vert, horiz) * Mathf.Rad2Deg;

        GameObject ramp = Box(name, (foot + head) / 2f, new Vector3(width, 0.25f, length), matRamp, parent);
        ramp.transform.rotation = Quaternion.Euler(-angle, 0f, 0f);
    }

    // ── Rock Obstacles ────────────────────────────────────────────────────

    static void CreateOcclusionWalls()
    {
        Transform p = Child("RockObstacles");
        // Small rock-like obstacles scattered across the field for tactical gameplay
        Box("Rock_A", new Vector3( 8,  0.6f,  6),   new Vector3(2, 1.2f, 2),   matWall, p);
        Box("Rock_B", new Vector3(-6,  0.4f, -4),   new Vector3(1.5f, 0.8f, 1.5f), matWall, p);
        Box("Rock_C", new Vector3( 10, 0.5f,  -8),  new Vector3(1.8f, 1f, 1.8f),   matWall, p);
        Box("Rock_D", new Vector3(-10, 0.7f, 8),    new Vector3(2.2f, 1.4f, 2.2f), matWall, p);
        Box("Rock_E", new Vector3( 3,  0.4f, -8),   new Vector3(1.5f, 0.8f, 1.5f), matWall, p);
        Box("Rock_F", new Vector3(-4,  0.5f,  10),  new Vector3(1.6f, 1f, 1.6f),   matWall, p);
        Box("Rock_G", new Vector3( 12, 0.6f,  -2),  new Vector3(2f, 1.2f, 2f),     matWall, p);
        Box("Rock_H", new Vector3(-12, 0.5f,  -6),  new Vector3(1.8f, 1f, 1.8f),   matWall, p);
    }

    // ── Spawn Points ──────────────────────────────────────────────────────

    static void CreateSpawnPoints()
    {
        Transform p = Child("SpawnPoints");
        Vector3[] pts = {
            new Vector3( 20,  1, 20), new Vector3(-20,  1, 20),
            new Vector3( 20,  1, -5), new Vector3(-20,  1, -5),
            new Vector3(  5,  1, 22), new Vector3( -5,  1, 22),
            new Vector3( 20,  1, 10), new Vector3(-20,  1, 10),
            new Vector3( 12,  1, -8), new Vector3(-12,  1, -8),
        };
        foreach (var pt in pts)
        {
            GameObject sp = new GameObject("SpawnPoint");
            sp.transform.SetParent(p);
            sp.transform.position = pt;
        }
    }

    // ── Lighting ──────────────────────────────────────────────────────────

    static void CreateLighting()
    {
        Transform p = Child("Lighting");
        // Simple directional light for even lighting across the arena
        GameObject dirLight = new GameObject("DirectionalLight");
        dirLight.transform.SetParent(p);
        dirLight.transform.position = Vector3.zero;
        dirLight.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        Light dl = dirLight.AddComponent<Light>();
        dl.type = LightType.Directional;
        dl.color = Color.white;
        dl.intensity = 1.2f;
    }

    static void AddPointLight(Transform parent, string name, Vector3 pos, Color color, float intensity, float range)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.position = pos;
        Light l = go.AddComponent<Light>();
        l.type      = LightType.Point;
        l.color     = color;
        l.intensity = intensity;
        l.range     = range;
    }

    // ── Utilities ─────────────────────────────────────────────────────────

    static GameObject Box(string name, Vector3 pos, Vector3 scale, Material mat, Transform parent)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name                 = name;
        go.transform.SetParent(parent);
        go.transform.position   = pos;
        go.transform.localScale = scale;
        if (mat != null) go.GetComponent<Renderer>().sharedMaterial = mat;

        // Mark as navigation static so NavMesh can be baked
        GameObjectUtility.SetStaticEditorFlags(go,
            StaticEditorFlags.NavigationStatic | StaticEditorFlags.BatchingStatic);

        return go;
    }

    static Transform Child(string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(levelRoot);
        return go.transform;
    }
}
