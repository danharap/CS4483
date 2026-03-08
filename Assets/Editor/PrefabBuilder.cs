using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Editor tool: CS4483 → 3 - Create Prefabs
/// Generates Player, Chaser, FastEnemy, Boss, Projectile, and XPOrb prefabs
/// and saves them to Assets/Prefabs/. Run after Setup Scene.
/// </summary>
public static class PrefabBuilder
{
    private const string PrefabDir = "Assets/Prefabs";

    [MenuItem("CS4483/3 - Create Prefabs")]
    public static void CreateAllPrefabs()
    {
        if (!AssetDatabase.IsValidFolder(PrefabDir))
            AssetDatabase.CreateFolder("Assets", "Prefabs");

        CreateProjectilePrefab();
        CreateXPOrbPrefab();
        CreateHealthPackPrefab();
        CreateChaserPrefab();
        CreateFastEnemyPrefab();
        CreateBossPrefab();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[PrefabBuilder] ✓ Prefabs created in Assets/Prefabs/. Assign them in EnemySpawner + PlayerWeapon.");
    }

    // ── Projectile ────────────────────────────────────────────────────────

    static void CreateProjectilePrefab()
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "Projectile";
        go.transform.localScale = Vector3.one * 0.25f;
        
        // Make 3D mesh invisible (used only for collision)
        Renderer renderer = go.GetComponent<Renderer>();
        if (renderer != null)
            renderer.enabled = false;

        // Remove mesh collider, add sphere trigger
        Object.DestroyImmediate(go.GetComponent<SphereCollider>());
        SphereCollider col = go.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius    = 0.15f;

        // Rigidbody (kinematic, gravity off – movement done in script)
        Rigidbody rb = go.AddComponent<Rigidbody>();
        rb.useGravity  = false;
        rb.isKinematic = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        go.AddComponent<Projectile>();

        SavePrefab(go, "Projectile");
        Object.DestroyImmediate(go);
    }

    // ── XP Orb ────────────────────────────────────────────────────────────

    static void CreateXPOrbPrefab()
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "XPOrb";
        go.transform.localScale = Vector3.one * 0.3f;
        
        // Create and save cyan material (delete first if exists)
        string matPath = "Assets/Materials/M_XPOrb.mat";
        AssetDatabase.DeleteAsset(matPath);
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = new Color(0.0f, 0.9f, 0.9f);
        AssetDatabase.CreateAsset(mat, matPath);
        go.GetComponent<Renderer>().sharedMaterial = mat;

        Object.DestroyImmediate(go.GetComponent<SphereCollider>());
        SphereCollider col = go.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius    = 0.2f;

        go.AddComponent<XPOrb>();

        SavePrefab(go, "XPOrb");
        Object.DestroyImmediate(go);
    }

    // ── Health Pack ───────────────────────────────────────────────────────

    static void CreateHealthPackPrefab()
    {
        // Create parent object
        GameObject go = new GameObject("HealthPack");
        
        // Create and save green material (delete first if exists)
        string matPath = "Assets/Materials/M_HealthPack.mat";
        AssetDatabase.DeleteAsset(matPath);
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = new Color(0.1f, 1f, 0.1f); // Bright green
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", new Color(0.1f, 0.6f, 0.1f));
        AssetDatabase.CreateAsset(mat, matPath);

        // Create 3D plus sign (+) using 3 cubes
        // Vertical bar
        GameObject vertical = GameObject.CreatePrimitive(PrimitiveType.Cube);
        vertical.name = "Vertical";
        vertical.transform.SetParent(go.transform);
        vertical.transform.localPosition = Vector3.zero;
        vertical.transform.localScale = new Vector3(0.15f, 0.5f, 0.15f);
        vertical.GetComponent<Renderer>().sharedMaterial = mat;
        Object.DestroyImmediate(vertical.GetComponent<BoxCollider>());

        // Horizontal bar
        GameObject horizontal = GameObject.CreatePrimitive(PrimitiveType.Cube);
        horizontal.name = "Horizontal";
        horizontal.transform.SetParent(go.transform);
        horizontal.transform.localPosition = Vector3.zero;
        horizontal.transform.localScale = new Vector3(0.5f, 0.15f, 0.15f);
        horizontal.GetComponent<Renderer>().sharedMaterial = mat;
        Object.DestroyImmediate(horizontal.GetComponent<BoxCollider>());

        // Add trigger collider to parent
        BoxCollider col = go.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.size = new Vector3(0.6f, 0.6f, 0.2f);

        go.AddComponent<HealthPack>();

        SavePrefab(go, "HealthPack");
        Object.DestroyImmediate(go);
    }

    // ── Chaser Enemy ──────────────────────────────────────────────────────

    static void CreateChaserPrefab()
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = "Enemy_Chaser";
        go.transform.localScale = new Vector3(0.9f, 0.9f, 0.9f);
        
        // Bright red material for basic enemy (delete first if exists)
        string matPath = "Assets/Materials/M_Enemy_Chaser.mat";
        AssetDatabase.DeleteAsset(matPath);
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = new Color(1f, 0.1f, 0.1f);
        AssetDatabase.CreateAsset(mat, matPath);
        go.GetComponent<Renderer>().sharedMaterial = mat;

        SetupEnemyPhysics(go);
        TryAddNavMeshAgent(go, 3.5f);
        ChaserEnemy e = go.AddComponent<ChaserEnemy>();
        e.maxHP    = 60f;
        e.moveSpeed = 3.5f;
        e.xpDrop   = 10f;

        SavePrefab(go, "Enemy_Chaser");
        Object.DestroyImmediate(go);
    }

    // ── Fast Enemy ────────────────────────────────────────────────────────

    static void CreateFastEnemyPrefab()
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = "Enemy_Fast";
        go.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);
        
        // Bright orange material for fast enemy (delete first if exists)
        string matPath = "Assets/Materials/M_Enemy_Fast.mat";
        AssetDatabase.DeleteAsset(matPath);
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = new Color(1f, 0.6f, 0f);
        AssetDatabase.CreateAsset(mat, matPath);
        go.GetComponent<Renderer>().sharedMaterial = mat;

        SetupEnemyPhysics(go);
        TryAddNavMeshAgent(go, 6f);
        FastEnemy e = go.AddComponent<FastEnemy>();
        e.maxHP    = 30f;
        e.moveSpeed = 6f;
        e.xpDrop   = 8f;

        SavePrefab(go, "Enemy_Fast");
        Object.DestroyImmediate(go);
    }

    // ── Boss Enemy ────────────────────────────────────────────────────────

    static void CreateBossPrefab()
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = "Enemy_Boss";
        go.transform.localScale = new Vector3(3.5f, 3.5f, 3.5f);
        
        // Dark purple material for boss (delete first if exists)
        string matPath = "Assets/Materials/M_Enemy_Boss.mat";
        AssetDatabase.DeleteAsset(matPath);
        Material mat = new Material(Shader.Find("Standard"));
        mat.color = new Color(0.6f, 0f, 0.7f);
        AssetDatabase.CreateAsset(mat, matPath);
        go.GetComponent<Renderer>().sharedMaterial = mat;

        SetupEnemyPhysics(go);
        TryAddNavMeshAgent(go, 2.5f);
        BossEnemy boss = go.AddComponent<BossEnemy>();
        boss.maxHP    = 500f;
        boss.moveSpeed = 2.5f;
        boss.xpDrop   = 100f;

        // Visual indicator: large red sphere on top
        GameObject top = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        top.name = "BossCrown";
        top.transform.SetParent(go.transform);
        top.transform.localPosition = new Vector3(0f, 0.6f, 0f);
        top.transform.localScale    = new Vector3(0.4f, 0.4f, 0.4f);
        string crownMatPath = "Assets/Materials/M_BossCrown.mat";
        AssetDatabase.DeleteAsset(crownMatPath);
        Material crownMat = new Material(Shader.Find("Standard"));
        crownMat.color = new Color(1f, 0f, 0f);
        crownMat.EnableKeyword("_EMISSION");
        crownMat.SetColor("_EmissionColor", new Color(1f, 0f, 0f) * 0.5f);
        AssetDatabase.CreateAsset(crownMat, crownMatPath);
        top.GetComponent<Renderer>().sharedMaterial = crownMat;
        Object.DestroyImmediate(top.GetComponent<SphereCollider>());

        SavePrefab(go, "Enemy_Boss");
        Object.DestroyImmediate(go);
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    static void SetupEnemyPhysics(GameObject go)
    {
        Rigidbody rb = go.AddComponent<Rigidbody>();
        rb.useGravity  = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
    }

    static void TryAddNavMeshAgent(GameObject go, float speed)
    {
        NavMeshAgent agent = go.AddComponent<NavMeshAgent>();
        agent.speed       = speed;
        agent.angularSpeed = 360f;
        agent.acceleration = 20f;
        agent.stoppingDistance = 0.5f;
        agent.radius      = 0.4f;
        agent.height      = 2f;
    }

    static void SetColor(GameObject go, Color color)
    {
        Renderer r = go.GetComponent<Renderer>();
        if (r != null) r.material = new Material(Shader.Find("Standard")) { color = color };
    }

    static void SavePrefab(GameObject go, string name)
    {
        string path = $"{PrefabDir}/{name}.prefab";
        PrefabUtility.SaveAsPrefabAsset(go, path);
        Debug.Log($"[PrefabBuilder] Saved: {path}");
    }
}
