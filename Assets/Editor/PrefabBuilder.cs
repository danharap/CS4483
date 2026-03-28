using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using TMPro;

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

        EnsureTagExists("XPOrb");

        CreateProjectilePrefab();
        CreateXPOrbPrefab();
        CreateHealthPackPrefab();
        CreateChaserPrefab();
        CreateFastEnemyPrefab();
        CreateBigBatPrefab();
        CreateBossPrefab();
        CreateSatanBulletPrefab();
        CreateSatanBossPrefab();
        CreateHeavyEnemyPrefab();
        CreateDamageNumberPrefab();

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

        // Remove mesh collider, add sphere trigger (radius in local space; scale 0.25 → world radius ~0.5 so fast enemies don't tunnel through)
        Object.DestroyImmediate(go.GetComponent<SphereCollider>());
        SphereCollider col = go.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius    = 2f;

        // Rigidbody: kinematic + continuous speculative so trigger overlap is reliable vs fast-moving enemies
        Rigidbody rb = go.AddComponent<Rigidbody>();
        rb.useGravity  = false;
        rb.isKinematic = true;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        go.AddComponent<Projectile>();

        SavePrefab(go, "Projectile");
        Object.DestroyImmediate(go);
    }

    // ── XP Orb ────────────────────────────────────────────────────────────

    static void CreateXPOrbPrefab()
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "XPOrb";
        go.tag = "XPOrb";
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

    /// <summary>Ensures a tag exists in ProjectSettings so assigning gameObject.tag does not throw.</summary>
    static void EnsureTagExists(string tagName)
    {
        Object tagManagerAsset = AssetDatabase.LoadAssetAtPath<Object>("ProjectSettings/TagManager.asset");
        if (tagManagerAsset == null) return;
        SerializedObject so = new SerializedObject(tagManagerAsset);
        SerializedProperty tagsProp = so.FindProperty("tags");
        if (tagsProp == null) return;
        for (int i = 0; i < tagsProp.arraySize; i++)
            if (tagsProp.GetArrayElementAtIndex(i).stringValue == tagName)
                return;
        tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
        tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = tagName;
        so.ApplyModifiedPropertiesWithoutUndo();
        Debug.Log($"[PrefabBuilder] Added tag: {tagName}");
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
        mat.color = new Color(0.2f, 1f, 0.2f); // Bright green
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", new Color(0.2f, 1f, 0.2f) * 1.8f); // Strong green glow
        AssetDatabase.CreateAsset(mat, matPath);

        // Create 3D plus sign (+) using 3 cubes
        // Vertical bar (scaled almost to zero so medkit sprite is the only visible element)
        GameObject vertical = GameObject.CreatePrimitive(PrimitiveType.Cube);
        vertical.name = "Vertical";
        vertical.transform.SetParent(go.transform);
        vertical.transform.localPosition = Vector3.zero;
        vertical.transform.localScale = new Vector3(0.001f, 0.001f, 0.001f);
        vertical.GetComponent<Renderer>().sharedMaterial = mat;
        Object.DestroyImmediate(vertical.GetComponent<BoxCollider>());

        // Horizontal bar (also scaled almost to zero)
        GameObject horizontal = GameObject.CreatePrimitive(PrimitiveType.Cube);
        horizontal.name = "Horizontal";
        horizontal.transform.SetParent(go.transform);
        horizontal.transform.localPosition = Vector3.zero;
        horizontal.transform.localScale = new Vector3(0.001f, 0.001f, 0.001f);
        horizontal.GetComponent<Renderer>().sharedMaterial = mat;
        Object.DestroyImmediate(horizontal.GetComponent<BoxCollider>());

        // Add trigger collider to parent
        BoxCollider col = go.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.size = new Vector3(0.6f, 0.6f, 0.2f);

        // Add soft glow light above the plus sign
        GameObject glow = new GameObject("GlowLight");
        glow.transform.SetParent(go.transform);
        glow.transform.localPosition = new Vector3(0f, 0.8f, 0f);
        Light light = glow.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(0.4f, 1f, 0.4f);
        light.range = 6f;
        light.intensity = 4.0f;

        go.AddComponent<HealthPack>();
        go.AddComponent<HealthPackGlow>();

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

        CapsuleCollider cap = go.GetComponent<CapsuleCollider>();
        if (cap != null) { cap.radius = 0.7f; cap.height = 2.4f; }
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

        CapsuleCollider cap = go.GetComponent<CapsuleCollider>();
        if (cap != null) { cap.radius = 0.6f; cap.height = 2.2f; }
        SetupEnemyPhysics(go);
        TryAddNavMeshAgent(go, 6f);
        FastEnemy e = go.AddComponent<FastEnemy>();
        e.maxHP    = 22f;
        e.moveSpeed = 6f;
        e.xpDrop   = 8f;

        SavePrefab(go, "Enemy_Fast");
        Object.DestroyImmediate(go);
    }

    // ── Big Bat Enemy (fast + higher HP) ──────────────────────────────────

    static void CreateBigBatPrefab()
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = "Enemy_BigBat";
        go.transform.localScale = new Vector3(0.9f, 0.9f, 0.9f);

        // Hide 3D mesh; BatEnemyVisualController will display sprites
        Renderer r = go.GetComponent<Renderer>();
        if (r != null) r.enabled = false;

        CapsuleCollider cap = go.GetComponent<CapsuleCollider>();
        if (cap != null) { cap.radius = 0.75f; cap.height = 2.4f; }
        SetupEnemyPhysics(go);
        TryAddNavMeshAgent(go, 6.2f);

        BigBatEnemy e = go.AddComponent<BigBatEnemy>();
        e.maxHP = 65f;
        e.moveSpeed = 6.2f;
        e.xpDrop = 14f;

        SavePrefab(go, "Enemy_BigBat");
        Object.DestroyImmediate(go);
    }

    // ── Boss Enemy ────────────────────────────────────────────────────────

    static void CreateBossPrefab()
    {
        GameObject root = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        root.name = "Enemy_Boss";
        // IMPORTANT: keep physics at scale 1 (so hitbox/pathing isn't gigantic).
        // Scale only the visuals via VisualRoot.
        root.transform.localScale = Vector3.one;

        // Hide 3D mesh; we render with sprites like Heavy
        Renderer r = root.GetComponent<Renderer>();
        if (r != null) r.enabled = false;

        CapsuleCollider cap = root.GetComponent<CapsuleCollider>();
        // Smaller hitbox (about half of previous)
        if (cap != null) { cap.radius = 0.45f; cap.height = 1.6f; }
        SetupEnemyPhysics(root);
        TryAddNavMeshAgent(root, 2.5f);
        // Boss agent should be bigger than default enemies
        {
            var a = root.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (a != null)
            {
                a.radius = 0.45f;
                a.height = 1.6f;
                a.stoppingDistance = 1.0f;
                a.acceleration = 30f;
                a.angularSpeed = 720f;
            }
        }

        BossEnemy boss = root.AddComponent<BossEnemy>();
        boss.maxHP    = 500f;
        boss.moveSpeed = 2.5f;
        boss.xpDrop   = 100f;

        // Visual root so we can "jump" visuals without moving colliders (prevents pushing player upward)
        GameObject visualRoot = new GameObject("VisualRoot");
        visualRoot.transform.SetParent(root.transform);
        visualRoot.transform.localPosition = Vector3.zero;
        visualRoot.transform.localScale = new Vector3(3.5f, 3.5f, 3.5f);

        // Body
        GameObject body = new GameObject("Body");
        body.transform.SetParent(visualRoot.transform);
        body.transform.localPosition = new Vector3(0f, 1.6f, 0f);
        body.transform.localScale = Vector3.one;
        SpriteRenderer bodySr = body.AddComponent<SpriteRenderer>();
        bodySr.sortingOrder = 20;
        body.AddComponent<Billboard>();

        // Head (renders above body)
        GameObject head = new GameObject("Head");
        head.transform.SetParent(body.transform);
        head.transform.localPosition = new Vector3(0f, 0.62f, 0f);
        head.transform.localScale = Vector3.one;
        SpriteRenderer headSr = head.AddComponent<SpriteRenderer>();
        headSr.sortingOrder = 21;
        head.AddComponent<Billboard>();

        BossVisualController vis = root.AddComponent<BossVisualController>();
        vis.bodyRenderer = bodySr;
        vis.headRenderer = headSr;

        SavePrefab(root, "Enemy_Boss");
        Object.DestroyImmediate(root);
    }

    // ── Satan Boss (F10 debug) ────────────────────────────────────────────

    static void CreateSatanBulletPrefab()
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "SatanBullet";
        go.transform.localScale = Vector3.one * 0.35f;

        Renderer r = go.GetComponent<Renderer>();
        if (r != null) r.enabled = false;

        Object.DestroyImmediate(go.GetComponent<SphereCollider>());
        SphereCollider col = go.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius    = 1f;

        Rigidbody rb = go.AddComponent<Rigidbody>();
        rb.useGravity  = false;
        rb.isKinematic = true;
        rb.constraints = RigidbodyConstraints.FreezeRotation;

        go.AddComponent<SatanBullet>();

        SavePrefab(go, "SatanBullet");
        Object.DestroyImmediate(go);
    }

    static void CreateSatanBossPrefab()
    {
        GameObject bulletAsset = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabDir}/SatanBullet.prefab");
        if (bulletAsset == null)
        {
            Debug.LogWarning("[PrefabBuilder] SatanBullet.prefab missing; run Create Prefabs again.");
            return;
        }

        GameObject root = new GameObject("Enemy_Satan");
        root.AddComponent<SatanBossController>();
        root.AddComponent<SatanAnimationController>();
        SatanFootPhase foot = root.AddComponent<SatanFootPhase>();
        SatanAttacks attacks = root.AddComponent<SatanAttacks>();

        SerializedObject soBoss = new SerializedObject(root.GetComponent<SatanBossController>());
        soBoss.FindProperty("footPhase").objectReferenceValue = foot;
        soBoss.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject soAtk = new SerializedObject(attacks);
        soAtk.FindProperty("bulletPrefab").objectReferenceValue = bulletAsset;
        soAtk.ApplyModifiedPropertiesWithoutUndo();

        SavePrefab(root, "Enemy_Satan");
        Object.DestroyImmediate(root);
    }

    // ── Heavy Enemy (tank) ────────────────────────────────────────────────
    // Body (animated) + Head (direction: up/down = sHeavyHead, right = sHeavyHead_Right, left = same sprite flipped).
    // Apply Sprites to Prefabs uses ApplyHeavyEnemySprites to assign body strip + head sprites.

    static void CreateHeavyEnemyPrefab()
    {
        GameObject root = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        root.name = "Enemy_Heavy";
        // 1.5x larger than prior (1.2 → 1.8)
        root.transform.localScale = new Vector3(1.8f, 1.8f, 1.8f);

        Renderer r = root.GetComponent<Renderer>();
        if (r != null) r.enabled = false;

        CapsuleCollider cap = root.GetComponent<CapsuleCollider>();
        if (cap != null) { cap.radius = 1.0f; cap.height = 3.0f; }
        SetupEnemyPhysics(root);
        TryAddNavMeshAgent(root, 2.0f);

        HeavyEnemy heavy = root.AddComponent<HeavyEnemy>();
        heavy.maxHP     = 180f;
        heavy.moveSpeed = 2.0f;
        heavy.xpDrop    = 18f;

        // Body: animated sprite below the head (frames driven by HeavyEnemyVisualController)
        GameObject body = new GameObject("Body");
        body.transform.SetParent(root.transform);
        body.transform.localPosition = new Vector3(0f, 1.15f, 0f);
        body.transform.localScale = Vector3.one;
        SpriteRenderer bodySr = body.AddComponent<SpriteRenderer>();
        bodySr.sortingOrder = 12;
        body.AddComponent<Billboard>();

        // Head: direction-based (heavy head / heavy right, flip when moving left)
        GameObject head = new GameObject("Head");
        head.transform.SetParent(body.transform);
        head.transform.localPosition = new Vector3(0f, 0.48f, 0f);
        head.transform.localScale = Vector3.one;
        SpriteRenderer headSr = head.AddComponent<SpriteRenderer>();
        headSr.sortingOrder = 11;
        head.AddComponent<Billboard>();

        HeavyEnemyVisualController vis = root.AddComponent<HeavyEnemyVisualController>();
        vis.bodyRenderer = bodySr;
        vis.headRenderer = headSr;

        SavePrefab(root, "Enemy_Heavy");
        Object.DestroyImmediate(root);
    }

    static void CreateDamageNumberPrefab()
    {
        GameObject go = new GameObject("DamageNumber");
        TextMeshPro tmp = go.AddComponent<TextMeshPro>();
        tmp.text = "10";
        tmp.fontSize = 4f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(1f, 0.85f, 0.25f);

        go.AddComponent<Billboard>();
        go.AddComponent<DamageNumber>();

        SavePrefab(go, "DamageNumber");
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
