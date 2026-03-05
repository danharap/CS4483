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
        SetColor(go, new Color(1f, 0.85f, 0.1f));

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
        SetColor(go, new Color(0.0f, 0.9f, 0.9f));

        Object.DestroyImmediate(go.GetComponent<SphereCollider>());
        SphereCollider col = go.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius    = 0.2f;

        go.AddComponent<XPOrb>();

        SavePrefab(go, "XPOrb");
        Object.DestroyImmediate(go);
    }

    // ── Chaser Enemy ──────────────────────────────────────────────────────

    static void CreateChaserPrefab()
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = "Enemy_Chaser";
        go.transform.localScale = new Vector3(0.9f, 0.9f, 0.9f);
        SetColor(go, new Color(0.75f, 0.15f, 0.15f));

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
        SetColor(go, new Color(0.95f, 0.55f, 0.05f));

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
        go.transform.localScale = new Vector3(2.5f, 2.5f, 2.5f);
        SetColor(go, new Color(0.5f, 0.0f, 0.5f));

        SetupEnemyPhysics(go);
        TryAddNavMeshAgent(go, 2.5f);
        BossEnemy boss = go.AddComponent<BossEnemy>();
        boss.maxHP    = 500f;
        boss.moveSpeed = 2.5f;
        boss.xpDrop   = 100f;

        // Visual indicator: child sphere on top
        GameObject top = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        top.name = "BossCrown";
        top.transform.SetParent(go.transform);
        top.transform.localPosition = new Vector3(0f, 0.6f, 0f);
        top.transform.localScale    = new Vector3(0.3f, 0.3f, 0.3f);
        SetColor(top, Color.yellow);
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
