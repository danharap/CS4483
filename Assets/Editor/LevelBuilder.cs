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
        CreateNorthPocket();
        CreateEastPocket();
        CreateWestPocket();
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
        // Raised platform
        Box("Hub_Floor",  new Vector3(0, 0.5f, 0), new Vector3(9, 1f, 9), matHub, p);
        // Landmark pillar (BLUE = main wayfinding beacon)
        GameObject pillar = Box("Hub_Pillar", new Vector3(0, 4f, 0), new Vector3(1, 8, 1), null, p);
        pillar.GetComponent<Renderer>().material = GetOrCreateMat("M_Blue", new Color(0.1f, 0.3f, 1f));
        // Connector corridors (flush with main floor)
        Box("Corridor_N", new Vector3( 0,  0, 7),   new Vector3(4, 0.4f,  6), matFloor, p);
        Box("Corridor_E", new Vector3( 7,  0, 0),   new Vector3(6, 0.4f,  4), matFloor, p);
        Box("Corridor_W", new Vector3(-7,  0, 0),   new Vector3(6, 0.4f,  4), matFloor, p);
        Box("Corridor_S", new Vector3( 0,  0, -7),  new Vector3(5, 0.4f,  6), matFloor, p);

        // Corridor walls (choke points)
        Box("ChokeWall_N_L",  new Vector3(-2.5f, 2, 7),   new Vector3(1, 4, 6), matWall, p);
        Box("ChokeWall_N_R",  new Vector3( 2.5f, 2, 7),   new Vector3(1, 4, 6), matWall, p);
        Box("ChokeWall_E_L",  new Vector3( 7, 2, -2.5f),  new Vector3(6, 4, 1), matWall, p);
        Box("ChokeWall_E_R",  new Vector3( 7, 2,  2.5f),  new Vector3(6, 4, 1), matWall, p);
        Box("ChokeWall_W_L",  new Vector3(-7, 2, -2.5f),  new Vector3(6, 4, 1), matWall, p);
        Box("ChokeWall_W_R",  new Vector3(-7, 2,  2.5f),  new Vector3(6, 4, 1), matWall, p);
    }

    // ── North Pocket (GREEN – Healing Shrine) ─────────────────────────────

    static void CreateNorthPocket()
    {
        Transform p = Child("Pocket_North_Green");
        Box("Pocket_N_Floor",  new Vector3(0, 0, 15),   new Vector3(12, 0.4f, 9),  matGreen, p);
        // Pocket walls
        Box("Pocket_N_WallL",  new Vector3(-6,  2, 15), new Vector3(1, 4, 10), matWall, p);
        Box("Pocket_N_WallR",  new Vector3( 6,  2, 15), new Vector3(1, 4, 10), matWall, p);
        Box("Pocket_N_WallB",  new Vector3( 0,  2, 20), new Vector3(13, 4, 1), matWall, p);
        // Landmark (GREEN pillar)
        GameObject pillar = Box("Green_Pillar", new Vector3(0, 3, 17), new Vector3(0.6f, 6, 0.6f), matGreen, p);
        // Healing shrine marker (will have HealingShrine component added manually)
        GameObject shrine = Box("HealingShrine", new Vector3(-3, 0.6f, 16), new Vector3(1.5f, 1.2f, 1.5f), matGreen, p);
        shrine.tag = "Untagged";
        // Prompt text anchor (empty GO for UI attachment)
        GameObject prompt = new GameObject("ShrinePrompt_Anchor");
        prompt.transform.SetParent(shrine.transform);
        prompt.transform.localPosition = Vector3.up * 1.5f;
    }

    // ── East Pocket (YELLOW – Upgrade Area) ──────────────────────────────

    static void CreateEastPocket()
    {
        Transform p = Child("Pocket_East_Yellow");
        Box("Pocket_E_Floor",  new Vector3(16, 0, 0),  new Vector3(9, 0.4f, 12), matYellow, p);
        Box("Pocket_E_WallN",  new Vector3(16, 2, 6),  new Vector3(10, 4, 1),    matWall,   p);
        Box("Pocket_E_WallS",  new Vector3(16, 2, -6), new Vector3(10, 4, 1),    matWall,   p);
        Box("Pocket_E_WallE",  new Vector3(21, 2, 0),  new Vector3(1, 4, 13),    matWall,   p);
        Box("Yellow_Pillar",   new Vector3(18, 3, 0),  new Vector3(0.6f, 6, 0.6f), matYellow, p);
    }

    // ── West Pocket (RED – pre-Boss area) ────────────────────────────────

    static void CreateWestPocket()
    {
        Transform p = Child("Pocket_West_Red");
        Box("Pocket_W_Floor",  new Vector3(-16, 0, 0),  new Vector3(9, 0.4f, 12), matRed, p);
        Box("Pocket_W_WallN",  new Vector3(-16, 2, 6),  new Vector3(10, 4, 1),    matWall, p);
        Box("Pocket_W_WallS",  new Vector3(-16, 2, -6), new Vector3(10, 4, 1),    matWall, p);
        Box("Pocket_W_WallW",  new Vector3(-21, 2, 0),  new Vector3(1, 4, 13),    matWall, p);
        Box("Red_Pillar",      new Vector3(-18, 3, 0),  new Vector3(0.6f, 6, 0.6f), matRed, p);
    }

    // ── Boss Arena ────────────────────────────────────────────────────────

    static void CreateBossArena()
    {
        Transform p = Child("BossArena");
        Box("Boss_Floor",    new Vector3(0, 0, -17),   new Vector3(14, 0.4f, 8), matBoss, p);
        Box("Boss_WallL",    new Vector3(-7, 2, -17),  new Vector3(1, 4, 9),     matWall, p);
        Box("Boss_WallR",    new Vector3( 7, 2, -17),  new Vector3(1, 4, 9),     matWall, p);
        Box("Boss_WallB",    new Vector3( 0, 2, -21),  new Vector3(15, 4, 1),    matWall, p);
        Box("Boss_WallBoss", new Vector3(0, 4, -13),   new Vector3(15, 0.3f, 1), matWall, p); // top lintel
        // Boss landmark (Red pillar)
        Box("Boss_Pillar",   new Vector3(0, 3, -19),   new Vector3(1, 6, 1), matBoss, p);
        // Gate door (GateDoor component added by SceneSetup)
        GameObject gate = Box("GateDoor", new Vector3(0, 2, -12.8f), new Vector3(5, 4, 0.4f), matRed, p);
        gate.name = "GateDoor";   // SceneSetup finds this by name
    }

    // ── Raised Platforms + Ramps ──────────────────────────────────────────

    static void CreateRaisedPlatforms()
    {
        Transform p = Child("RaisedPlatforms");

        // Platform 1 (North-East corner)
        Box("Platform1",  new Vector3(17, 1.75f, 10),  new Vector3(7, 0.5f, 6),  matHub, p);
        CreateRamp(p, "Ramp1",
            new Vector3(14, 0.4f, 10),  // foot (on main floor)
            new Vector3(14, 2.0f, 10),  // head (at platform edge)
            4f);                        // width
        // Parapet walls on platform 1
        Box("P1_WallN", new Vector3(17, 2.75f, 13),  new Vector3(7, 1.5f, 0.4f), matWall, p);
        Box("P1_WallE", new Vector3(20.5f, 2.75f, 10), new Vector3(0.4f, 1.5f, 7), matWall, p);

        // Platform 2 (South-West corner)
        Box("Platform2",  new Vector3(-17, 1.75f, -10), new Vector3(7, 0.5f, 6),  matHub, p);
        CreateRamp(p, "Ramp2",
            new Vector3(-14, 0.4f, -10), // foot
            new Vector3(-14, 2.0f, -10), // head
            4f);
        Box("P2_WallS", new Vector3(-17, 2.75f, -13), new Vector3(7, 1.5f, 0.4f), matWall, p);
        Box("P2_WallW", new Vector3(-20.5f, 2.75f, -10), new Vector3(0.4f, 1.5f, 7), matWall, p);
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

    // ── Occlusion Walls ───────────────────────────────────────────────────

    static void CreateOcclusionWalls()
    {
        Transform p = Child("OcclusionWalls");
        // Scattered blocky pillars/walls that break sightlines across the arena
        Box("Occ_A", new Vector3( 4,  2,  3),   new Vector3(1, 4, 4),   matWall, p);
        Box("Occ_B", new Vector3(-4,  2, -3),   new Vector3(4, 4, 1),   matWall, p);
        Box("Occ_C", new Vector3( 9,  2,  9),   new Vector3(1, 4, 3),   matWall, p);
        Box("Occ_D", new Vector3(-9,  2, -9),   new Vector3(3, 4, 1),   matWall, p);
        Box("Occ_E", new Vector3( 2,  2, -5),   new Vector3(1, 4, 5),   matWall, p);
        Box("Occ_F", new Vector3(-2,  2,  8),   new Vector3(5, 4, 1),   matWall, p);
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

    // ── Lighting (wayfinding) ─────────────────────────────────────────────

    static void CreateLighting()
    {
        Transform p = Child("Lighting");
        AddPointLight(p, "Light_Hub",    new Vector3(0, 5, 0),     Color.white,  2.5f, 20f);
        AddPointLight(p, "Light_North",  new Vector3(0, 4, 15),    Color.green,  2f,   15f);
        AddPointLight(p, "Light_East",   new Vector3(16, 4, 0),    Color.yellow, 2f,   12f);
        AddPointLight(p, "Light_West",   new Vector3(-16, 4, 0),   Color.red,    2f,   12f);
        AddPointLight(p, "Light_Boss",   new Vector3(0, 4, -17),   new Color(0.9f,0.1f,0.1f), 3f, 14f);
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
