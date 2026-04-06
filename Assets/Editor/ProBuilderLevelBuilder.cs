using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
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
    const string TutorialRootName = "=== LEVEL (Tutorial) ===";
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

    /// <summary>
    /// Deterministic tutorial prison (XZ plane): long corridor on +X, narrow in Z. One module = one cell; rows tile on X only.
    /// Floor half-X must cover all gates/triggers/spawn (was mismatched when floor was ±48 but logic used X to -76).
    /// </summary>
    static class TutorialPrisonConstants
    {
        public const float CorridorInnerHalfZ = 6.75f;
        public const float HallwayInnerWidthX = 13.5f;
        public const float OuterWallFaceOffset = 7.25f;
        /// <summary>How far the bar plane (and cell root) moves from the walkway inner face toward corridor center (Z). Snapped.</summary>
        public const float BarProtrusionIntoWalkway = 1.6f;
        public const float GridSnap = 0.05f;
        public const float BarModuleWidth = 0.55f;
        public const int BarsPerCell = 5;
        public const float BarPostThickness = 0.12f;
        public const float BarHeight = 2.8f;
        public const float BarCenterY = 1.4f;
        public const float CellExtentX = 2.4f;
        public const float CellSpacingX = 2.65f;
        public const float CellXMin = -75f;
        public const float CellXMax = 75f;
        public const float GateThicknessZ = 0.35f;
        public const float EndWallThicknessZ = 0.28f;
        public const float BackStripThicknessX = 0.32f;
    }

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
        CreateSatanThroneArea(matStands);   // throne uses Arena 1 stand material
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

        CreateFlameTraps();
        CreateSpawnPoints();
        CreateLighting();
        CreateSatanThroneArea();

        // Ensure NavMeshSurface uses only Arena 2's own children so the player's
        // CharacterController capsule (and any other scene colliders) are excluded from the bake.
        NavMeshSurface nms = root.GetComponent<NavMeshSurface>();
        if (nms == null) nms = root.AddComponent<NavMeshSurface>();
        nms.collectObjects = CollectObjects.Children;
        nms.useGeometry    = NavMeshCollectGeometry.PhysicsColliders;

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

    static void DestroyTutorialIfExists()
    {
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (root.name == TutorialRootName)
            {
                Object.DestroyImmediate(root);
                Debug.Log("[ProBuilderLevelBuilder] Removed existing Tutorial level root.");
            }
        }
    }

    static void EnsureMaterials()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            AssetDatabase.CreateFolder("Assets", "Materials");

        matFloor    = GetOrCreateMat("M_Floor",    new Color(0.60f, 0.60f, 0.60f));
        // ── Stage 1 palette: Roman-colosseum travertine limestone ──────────
        // Warm sandstone for boundary walls (think exposed travertine blocks)
        matWall     = GetOrCreateMat("M_Wall",     new Color(0.68f, 0.60f, 0.48f));
        // Lobby walls use a dedicated material so they are never affected by arena wall changes.
        GetOrCreateMat("M_LobbyWall", new Color(0.62f, 0.62f, 0.62f));
        matHub      = GetOrCreateMat("M_Hub",      new Color(0.85f, 0.85f, 0.85f));
        matBoss     = GetOrCreateMat("M_Boss",     new Color(0.40f, 0.02f, 0.02f));
        matObstacle = GetOrCreateMat("M_Obstacle", new Color(0.45f, 0.40f, 0.35f));
        // Slightly cooler grey stone for stands — distinct from walls but same stone family
        matStands   = GetOrCreateMat("M_Stands",   new Color(0.56f, 0.55f, 0.53f));
        matStandsArena2 = GetOrCreateMat("M_Stands_Arena2", new Color(0.16f, 0.07f, 0.09f)); // nether / darker stone

        // Force an immediate asset save so the updated colour and gloss values are written
        // to disk before the scene geometry references them (avoids stale cached materials).
        AssetDatabase.SaveAssets();
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
            mat.color = color;
        }

        // Clear any albedo texture — if sBg.png or any other sprite was previously dragged
        // onto this material in the Inspector, clear it so walls render as flat colour only.
        mat.SetTexture("_MainTex", null);

        // Fully matte: strip gloss, metallic, specular and env-map reflections.
        // SetFloat alone is not enough for the Standard shader — keywords must also be toggled.
        mat.SetFloat("_Glossiness", 0f);
        mat.SetFloat("_Metallic",   0f);
        mat.SetFloat("_SpecularHighlights", 0f);
        mat.SetFloat("_GlossyReflections",  0f);
        mat.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
        mat.EnableKeyword("_GLOSSYREFLECTIONS_OFF");
        mat.DisableKeyword("_METALLICGLOSSMAP");
        mat.DisableKeyword("_SPECGLOSSMAP");

        EditorUtility.SetDirty(mat);
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

    // Called from SetupAll; builds a dedicated === LEVEL (Tutorial) === jail-cell walkthrough.
    public static void BuildTutorialLevel()
    {
        Scene scene = EditorSceneManager.GetActiveScene();
        Debug.Log("[ProBuilderLevelBuilder] Building Tutorial jail-cell level...");

        EnsureMaterials();
        DestroyTutorialIfExists();

        GameObject root = new GameObject(TutorialRootName);
        root.SetActive(false);
        levelRoot = root.transform;

        CreateTutorialGeometry();
        CreateLighting();

        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("[ProBuilderLevelBuilder] ✓ Tutorial level rebuilt (Tutorial_Prison). Save MainScene. Player spawn: Tutorial_PlayerSpawn / " + TutorialPrisonLayout.PlayerSpawnPosition);
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
        // Depth is 1.05 (5 % overlap) so adjacent rows share edge pixels and the seam disappears.
        const float stepDepth  = 1.05f;
        // Gap must exceed wallThickness/2 (0.25) so step 0 never overlaps the wall outer face.
        // 1.0 puts step-0 centre at radius 39, inner edge at 38.5 — safely clear of the wall.
        const float gapBehindWall = 1.0f;

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

                // No shadow casting: stand rows were casting shadow bands onto the row below,
                // creating the visible seam/tear lines across the stands in Stage 1.
                Renderer sr = step.GetComponent<Renderer>();
                if (sr != null)
                    sr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }
    }

    // ── Satan Throne / Watch Platform (Arena 2 top-middle) ───────────────

    /// <summary>
    /// Builds a ceremonial ruler's box / throne viewing area in the top-middle colosseum stands.
    /// Placed behind Wall_Seg_0 (positive-Z octagon face) at stand height.
    ///
    /// Layout (Z positive = "top" of arena from camera's perspective):
    ///   ArenaRadius = 38  →  stands begin at z ≈ 38.5, step 14 rows outward.
    ///   Throne sits at z ≈ 44–48, elevated ~2 units above the mid-stand height.
    ///   Four columns + canopy frame the throne seat for visual clarity from top-down camera.
    ///
    /// All geometry is decorative — colliders are disabled so they do not affect NavMesh or Satan's hurtbox.
    /// Satan's watch-position inspector field on SatanArenaIntroController should match throneWatchSatanPos.
    /// </summary>
    /// <param name="overrideMaterial">If non-null, used as the main platform stone material (pass matStands for Arena 1).</param>
    static void CreateSatanThroneArea(Material overrideMaterial = null)
    {
        // Arena 1 uses the passed stand material (warm grey stone).
        // Arena 2 falls back to the dark nether-stone default.
        Material matThrone = overrideMaterial ?? GetOrCreateMat("M_Stands_Arena2", new Color(0.16f, 0.07f, 0.09f));
        // Warm ochre gold for columns — works in both colosseum (Stage 1) and hell (Stage 2) themes.
        Material matGold   = GetOrCreateMat("M_Throne_Gold", new Color(0.72f, 0.56f, 0.10f));
        // Neutral dark charcoal for seat/back/arms — not reddish so it reads in Stage 1 too.
        Material matDark   = GetOrCreateMat("M_Throne_Dark", new Color(0.14f, 0.13f, 0.12f));

        GameObject root = new GameObject("Satan_Throne_Area");
        root.transform.SetParent(levelRoot);

        // ── Geometry constants ────────────────────────────────────────────
        // With gapBehindWall=1.0: step i centre z = 39+i.
        // Step 3: z=42, y_top=10.22.  Throne sits just above step 3, closer + lower than before.
        // "FrontStep" piece removed — it clipped into stand row 0 and wasn't visible from camera.
        const float baseTopY  = 10.0f;  // flat top of platform (just above step 3 top at 10.22)
        const float platformH = 1.4f;   // platform slab height
        const float throneZ   = 42.0f;  // z centre — step 3, ~4 units behind the wall (was 43)
        const float boxW      = 10f;    // width (X)
        const float boxD      = 4.5f;   // depth (Z) — narrower so front edge clears stand row 0

        // ── Raised stone platform slab ────────────────────────────────────
        PBCube("Throne_Platform",
            new Vector3(0f, baseTopY - platformH * 0.5f, throneZ),
            new Vector3(boxW, platformH, boxD),
            matThrone, root.transform);

        // ── Throne seat (seat + back + arm rests) ─────────────────────────
        // Seat cushion/base
        PBCube("Throne_Seat",
            new Vector3(0f, baseTopY + 0.35f, throneZ + 0.4f),
            new Vector3(2.8f, 0.55f, 2.6f),
            matDark, root.transform);
        // Back rest
        PBCube("Throne_Back",
            new Vector3(0f, baseTopY + 1.5f, throneZ + 1.6f),
            new Vector3(2.8f, 2.2f, 0.35f),
            matDark, root.transform);
        // Left arm
        PBCube("Throne_ArmLeft",
            new Vector3(-1.35f, baseTopY + 0.8f, throneZ + 0.4f),
            new Vector3(0.35f, 0.65f, 2.6f),
            matDark, root.transform);
        // Right arm
        PBCube("Throne_ArmRight",
            new Vector3( 1.35f, baseTopY + 0.8f, throneZ + 0.4f),
            new Vector3(0.35f, 0.65f, 2.6f),
            matDark, root.transform);

        // ── Open-crown design: tall corner pillars + back monument, NO canopy ──
        // The solid canopy was removed so the top-down camera can see Satan sitting below.
        // Visual interest comes from tall gold obelisk-columns and a grand back monument.

        // Four corner obelisk columns — taller than before so they read clearly without a roof.
        const float colH   = 6.5f;
        const float colR   = 0.42f;
        float colBaseY     = baseTopY + colH * 0.5f;
        float colXOff      = boxW * 0.5f - 0.4f;
        float colZFront    = throneZ - boxD * 0.5f + 0.3f;
        float colZBack     = throneZ + boxD * 0.5f - 0.3f;

        PBCube("Col_FL", new Vector3(-colXOff, colBaseY, colZFront), new Vector3(colR, colH, colR), matGold, root.transform);
        PBCube("Col_FR", new Vector3( colXOff, colBaseY, colZFront), new Vector3(colR, colH, colR), matGold, root.transform);
        PBCube("Col_BL", new Vector3(-colXOff, colBaseY, colZBack),  new Vector3(colR, colH, colR), matGold, root.transform);
        PBCube("Col_BR", new Vector3( colXOff, colBaseY, colZBack),  new Vector3(colR, colH, colR), matGold, root.transform);

        // Flat cap / finial on each column — wide square topper makes columns look architectural.
        float capY = baseTopY + colH + 0.25f;
        PBCube("Cap_FL", new Vector3(-colXOff, capY, colZFront), new Vector3(0.80f, 0.32f, 0.80f), matGold, root.transform);
        PBCube("Cap_FR", new Vector3( colXOff, capY, colZFront), new Vector3(0.80f, 0.32f, 0.80f), matGold, root.transform);
        PBCube("Cap_BL", new Vector3(-colXOff, capY, colZBack),  new Vector3(0.80f, 0.32f, 0.80f), matGold, root.transform);
        PBCube("Cap_BR", new Vector3( colXOff, capY, colZBack),  new Vector3(0.80f, 0.32f, 0.80f), matGold, root.transform);

        // (No railing/cross-bars — removed at user request)

        // ── Grand back monument (behind seat, adds vertical drama from all camera angles) ──
        // Central obelisk — a tall gold monolith rising above the back-rest.
        float obeliskBackZ = throneZ + boxD * 0.5f;
        float obeliskH     = 9.0f;
        float obeliskY     = baseTopY + obeliskH * 0.5f;
        PBCube("Monument_Obelisk",
               new Vector3(0f, obeliskY, obeliskBackZ),
               new Vector3(1.6f, obeliskH, 0.55f), matGold, root.transform);
        // Dark stone cap on the obelisk top
        PBCube("Monument_Cap",
               new Vector3(0f, baseTopY + obeliskH + 0.45f, obeliskBackZ),
               new Vector3(1.0f, 0.65f, 0.45f), matDark, root.transform);

        // Flanking side pillars — shorter stone pillars that frame the central obelisk.
        float sidePillarH  = 5.5f;
        float sidePillarY  = baseTopY + sidePillarH * 0.5f;
        PBCube("Monument_PillarL",
               new Vector3(-1.5f, sidePillarY, obeliskBackZ),
               new Vector3(0.55f, sidePillarH, 0.55f), matThrone, root.transform);
        PBCube("Monument_PillarR",
               new Vector3( 1.5f, sidePillarY, obeliskBackZ),
               new Vector3(0.55f, sidePillarH, 0.55f), matThrone, root.transform);
        // Caps for flanking pillars
        PBCube("Monument_PillarL_Cap",
               new Vector3(-1.5f, baseTopY + sidePillarH + 0.22f, obeliskBackZ),
               new Vector3(0.80f, 0.3f, 0.80f), matGold, root.transform);
        PBCube("Monument_PillarR_Cap",
               new Vector3( 1.5f, baseTopY + sidePillarH + 0.22f, obeliskBackZ),
               new Vector3(0.80f, 0.3f, 0.80f), matGold, root.transform);

        // Lintel connecting the three monument pieces at mid-height (horizontal accent)
        float lintelY = baseTopY + sidePillarH * 0.65f;
        PBCube("Monument_Lintel",
               new Vector3(0f, lintelY, obeliskBackZ),
               new Vector3(3.8f, 0.30f, 0.45f), matDark, root.transform);

        // ── Front presentation step ───────────────────────────────────────────
        PBCube("Throne_FrontStep",
               new Vector3(0f, baseTopY + 0.08f, throneZ - boxD * 0.5f - 0.45f),
               new Vector3(boxW + 1.0f, 0.16f, 0.9f), matThrone, root.transform);

        // ── Disable all colliders — purely decorative ─────────────────────
        foreach (MeshCollider mc in root.GetComponentsInChildren<MeshCollider>())
            mc.enabled = false;

        // ── No shadow casting — throne pieces are decorative, shadows create seams ─
        foreach (Renderer r in root.GetComponentsInChildren<Renderer>())
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        // ── Static "seated Satan" visual ──────────────────────────────────
        // Baked directly into the throne geometry so it is always visible from the moment
        // Arena 2 is loaded (no runtime spawning required).
        // SatanArenaIntroController hides this object when the real boss spawns at wave 10.
        CreateThroneWatcherSprite(root.transform, baseTopY, throneZ);

        // Log the Satan watch position so the designer can copy it into the inspector
        Vector3 satanWatchPos = new Vector3(0f, baseTopY + 0.9f, throneZ - 0.2f);
        Debug.Log($"[ProBuilderLevelBuilder] Satan throne area created. " +
                  $"Set SatanArenaIntroController.thronePosition ≈ {satanWatchPos} " +
                  $"(baseTopY={baseTopY}, throneZ={throneZ})  jumpLandingPosition = (0, 1, 12).");
    }

    /// <summary>
    /// Creates a billboard sprite of Satan sitting on the throne — always visible, purely decorative.
    /// The sprite is the first frame of the awakening animation so it matches what the real Satan
    /// looks like when frozen in ThroneWatch state.
    /// </summary>
    static void CreateThroneWatcherSprite(Transform throneRoot, float baseTopY, float throneZ)
    {
        // Try to load the Satan awakening frame 0 (dormant pose).
        string spritePath = "Assets/Sprites/Final Boss/Satan Awakening/0.png";
        TextureImporter imp = AssetImporter.GetAtPath(spritePath) as TextureImporter;
        if (imp != null && imp.textureType != TextureImporterType.Sprite)
        {
            imp.textureType          = TextureImporterType.Sprite;
            imp.spriteImportMode     = SpriteImportMode.Single;
            imp.filterMode           = FilterMode.Point;
            imp.spritePixelsPerUnit  = 20f;
            imp.mipmapEnabled        = false;
            imp.alphaIsTransparency  = true;
            imp.textureCompression   = TextureImporterCompression.Uncompressed;
            imp.SaveAndReimport();
        }

        UnityEngine.Sprite satanSprite = AssetDatabase.LoadAssetAtPath<UnityEngine.Sprite>(spritePath);
        if (satanSprite == null)
        {
            Debug.LogWarning("[ProBuilderLevelBuilder] Satan Awakening/0.png not found — throne watcher skipped. " +
                             "Run 'CS4483 → 🎨 1. Slice Sprite Sheets' first.");
            return;
        }

        // Place the figure sitting on the throne seat (seat top ≈ baseTopY + 0.625).
        // Raise it by half the desired world height so the bottom of the sprite sits on the seat.
        const float desiredHeight = 5f; // world units — large enough to read at camera distance
        float ppu = satanSprite.pixelsPerUnit;
        float spriteH = satanSprite.rect.height / Mathf.Max(1f, ppu);
        float scale = desiredHeight / Mathf.Max(0.01f, spriteH);

        GameObject watcher = new GameObject("Throne_Satan_Visual");
        watcher.transform.SetParent(throneRoot);
        // Bottom of sprite at throne seat top (baseTopY + 0.625); centre raised by desiredHeight/2.
        watcher.transform.position = new Vector3(0f, baseTopY + 0.625f + desiredHeight * 0.5f, throneZ + 0.3f);
        watcher.transform.localScale = new Vector3(scale, scale, 1f);

        SpriteRenderer sr = watcher.AddComponent<SpriteRenderer>();
        sr.sprite           = satanSprite;
        sr.sortingOrder     = 20; // above throne meshes
        sr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        // Billboard so it always faces the camera.
        watcher.AddComponent<Billboard>();
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

        // Dedicated materials for the lobby — never shared with arena geometry.
        Material matLobbyFloor = GetOrCreateMat("M_LobbyFloor", new Color(0.55f, 0.55f, 0.55f));
        Material matLobbyWall  = GetOrCreateMat("M_LobbyWall",  new Color(0.62f, 0.62f, 0.62f));

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
        wallN.GetComponent<Renderer>().sharedMaterial = matLobbyWall;

        // South wall
        GameObject wallS = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wallS.name = "Lobby_Wall_South";
        wallS.transform.SetParent(lobby.transform);
        wallS.transform.position = new Vector3(0f, wallHeight * 0.5f, lobbyZ - halfSize);
        wallS.transform.localScale = new Vector3(20f, wallHeight, thickness);
        wallS.GetComponent<Renderer>().sharedMaterial = matLobbyWall;

        // East wall
        GameObject wallE = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wallE.name = "Lobby_Wall_East";
        wallE.transform.SetParent(lobby.transform);
        wallE.transform.position = new Vector3(halfSize, wallHeight * 0.5f, lobbyZ);
        wallE.transform.localScale = new Vector3(thickness, wallHeight, 20f);
        wallE.GetComponent<Renderer>().sharedMaterial = matLobbyWall;

        // West wall
        GameObject wallW = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wallW.name = "Lobby_Wall_West";
        wallW.transform.SetParent(lobby.transform);
        wallW.transform.position = new Vector3(-halfSize, wallHeight * 0.5f, lobbyZ);
        wallW.transform.localScale = new Vector3(thickness, wallHeight, 20f);
        wallW.GetComponent<Renderer>().sharedMaterial = matLobbyWall;

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

        // Tutorial guide NPC in the lobby (talking-only; no portal / no tutorial transport).
        GameObject tutorialNpc = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        tutorialNpc.name = "GuideNPC";
        tutorialNpc.transform.SetParent(lobby.transform);
        tutorialNpc.transform.position = new Vector3(8.8f, 1.0f, lobbyZ);
        tutorialNpc.transform.localScale = new Vector3(1.1f, 1.6f, 1.1f);
        var npcR = tutorialNpc.GetComponent<Renderer>();
        if (npcR != null)
            npcR.sharedMaterial = GetOrCreateMat("M_Blue", new Color(0.1f, 0.3f, 1f));

        Object.DestroyImmediate(tutorialNpc.GetComponent<CapsuleCollider>());
        SphereCollider npcTrigger = tutorialNpc.AddComponent<SphereCollider>();
        npcTrigger.isTrigger = true;
        npcTrigger.radius = 2f;

        Rigidbody npcRb = tutorialNpc.AddComponent<Rigidbody>();
        npcRb.isKinematic = true;
        npcRb.useGravity = false;

        var npcDialogue = tutorialNpc.AddComponent<NPCDialogue>();
        var soNpc = new SerializedObject(npcDialogue);
        soNpc.FindProperty("mode").enumValueIndex = 0; // Guide
        soNpc.ApplyModifiedPropertiesWithoutUndo();

        CreateWorldLabel(tutorialNpc.transform, "GuideLabel", "Guide", new Vector3(0f, 2.2f, 0f), Color.cyan);
        CreateWorldLabel(portal.transform, "ArenaLabel", "Arena", new Vector3(0f, 2.2f, 0f), Color.yellow);
    }

    // ── Tutorial prison (rebuilt: single root, grid cells on X, bounds-safe spawn) ──

    static void CreateTutorialGeometry()
    {
        float cz = TutorialPrisonLayout.CorridorCenterZ;
        float hx = TutorialPrisonLayout.FloorHalfX;
        float hzBand = TutorialPrisonLayout.FloorHalfZ;
        float floorNorthZ = cz - hzBand;
        float floorSouthZ = cz + hzBand;
        float wallSpanX = hx * 2f;

        GameObject prison = new GameObject("Tutorial_Prison");
        prison.transform.SetParent(levelRoot);
        prison.transform.localPosition = Vector3.zero;
        prison.transform.localRotation = Quaternion.identity;
        prison.transform.localScale = Vector3.one;

        Material floorMat = GetOrCreateMat("M_TutorialFloor", new Color(0.45f, 0.45f, 0.48f));
        Material wallMat  = GetOrCreateMat("M_TutorialWall",  new Color(0.20f, 0.20f, 0.24f));
        Material barMat   = GetOrCreateMat("M_JailBars",      new Color(0.62f, 0.62f, 0.66f));
        Material voidMat  = GetOrCreateMat("M_VoidBlack",     new Color(0f, 0f, 0f));

        GameObject voidFloor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        voidFloor.name = "Tutorial_VoidBackdrop";
        voidFloor.transform.SetParent(prison.transform);
        voidFloor.transform.position = new Vector3(0f, -2.0f, cz);
        voidFloor.transform.localScale = new Vector3(220f, 1f, 120f);
        voidFloor.GetComponent<Renderer>().sharedMaterial = voidMat;

        CreateTutorialOutsideBlackPlates(prison.transform, voidMat);

        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "Tutorial_Floor";
        floor.transform.SetParent(prison.transform);
        floor.transform.position = new Vector3(0f, -0.2f, cz);
        floor.transform.localScale = new Vector3(wallSpanX, 0.4f, hzBand * 2f);
        floor.GetComponent<Renderer>().sharedMaterial = floorMat;

        float wallT = 0.5f;
        CreateWall(prison.transform, "T_Wall_West",  new Vector3(-hx - wallT * 0.5f, 2f, cz), new Vector3(wallT, 4f, hzBand * 2f), wallMat);
        CreateWall(prison.transform, "T_Wall_East",   new Vector3( hx + wallT * 0.5f, 2f, cz), new Vector3(wallT, 4f, hzBand * 2f), wallMat);
        CreateWall(prison.transform, "T_Wall_North",  new Vector3(0f, 2f, floorNorthZ - wallT * 0.5f), new Vector3(wallSpanX, 4f, wallT), wallMat);
        CreateWall(prison.transform, "T_Wall_South",  new Vector3(0f, 2f, floorSouthZ + wallT * 0.5f), new Vector3(wallSpanX, 4f, wallT), wallMat);

        GameObject topRow = CreateHorizontalPrisonRow(prison.transform, isNorthRow: true, wallMat, barMat, "PrisonCells_Top");
        GameObject bottomRow = CreateHorizontalPrisonRow(prison.transform, isNorthRow: false, wallMat, barMat, "PrisonCells_Bottom");
        LogTutorialPrisonCellLayout(topRow, bottomRow);

        float gateSpanZ = TutorialPrisonConstants.HallwayInnerWidthX;
        float gateThicknessX = TutorialPrisonConstants.GateThicknessZ;
        CreateHallGateAcrossZ(prison.transform, "Tutorial_Gate_1", new Vector3(-70f, 1.5f, cz), barMat, gateSpanZ, gateThicknessX);
        CreateHallGateAcrossZ(prison.transform, "Tutorial_Gate_2", new Vector3(-40f, 1.5f, cz), barMat, gateSpanZ, gateThicknessX);
        CreateHallGateAcrossZ(prison.transform, "Tutorial_Gate_3", new Vector3(-10f, 1.5f, cz), barMat, gateSpanZ, gateThicknessX);

        GameObject playerSpawn = new GameObject("Tutorial_PlayerSpawn");
        playerSpawn.transform.SetParent(prison.transform);
        playerSpawn.transform.position = TutorialPrisonLayout.PlayerSpawnPosition;

        GameObject exitPortal = GameObject.CreatePrimitive(PrimitiveType.Cube);
        exitPortal.name = "TutorialToArena_Portal";
        exitPortal.transform.SetParent(prison.transform);
        exitPortal.transform.position = new Vector3(77f, 1.5f, cz);
        exitPortal.transform.localScale = new Vector3(2f, 3f, 0.3f);
        var exitR = exitPortal.GetComponent<Renderer>();
        if (exitR != null) exitR.sharedMaterial = GetOrCreateMat("M_Boss", new Color(0.40f, 0.02f, 0.02f));
        BoxCollider exitCol = exitPortal.GetComponent<BoxCollider>();
        exitCol.isTrigger = true;
        Rigidbody exitRb = exitPortal.AddComponent<Rigidbody>();
        exitRb.isKinematic = true;
        exitRb.useGravity = false;
        exitPortal.AddComponent<TutorialExitPortal>();
        CreateWorldLabel(exitPortal.transform, "ExitLabel", "Exit to Lobby", new Vector3(0f, 2.2f, 0f), Color.white);

        GameObject enemySpawn = new GameObject("Tutorial_EnemySpawn");
        enemySpawn.transform.SetParent(prison.transform);
        enemySpawn.transform.position = new Vector3(-62f, 1f, cz);

        GameObject firstCheckpoint = new GameObject("Tutorial_FirstCheckpoint");
        firstCheckpoint.transform.SetParent(prison.transform);
        firstCheckpoint.transform.position = new Vector3(-76f, 1f, cz);
        BoxCollider cpCol = firstCheckpoint.AddComponent<BoxCollider>();
        cpCol.isTrigger = true;
        cpCol.size = new Vector3(2f, 2f, 8f);
        var cpTrigger = firstCheckpoint.AddComponent<TutorialHallTrigger>();
        var soCp = new SerializedObject(cpTrigger);
        soCp.FindProperty("action").enumValueIndex = 0; // FirstGateApproach
        soCp.ApplyModifiedPropertiesWithoutUndo();

        // Trigger just beyond gate 1: starts delayed enemy spawn.
        GameObject firstPassed = new GameObject("Tutorial_FirstGatePassed");
        firstPassed.transform.SetParent(prison.transform);
        firstPassed.transform.position = new Vector3(-64f, 1f, cz);
        BoxCollider fpCol = firstPassed.AddComponent<BoxCollider>();
        fpCol.isTrigger = true;
        fpCol.size = new Vector3(2f, 2f, 8f);
        var fpTrigger = firstPassed.AddComponent<TutorialHallTrigger>();
        var soFp = new SerializedObject(fpTrigger);
        soFp.FindProperty("action").enumValueIndex = 1; // FirstGatePassed
        soFp.ApplyModifiedPropertiesWithoutUndo();

        // Trigger just beyond gate 2: starts orb objective.
        GameObject secondPassed = new GameObject("Tutorial_SecondGatePassed");
        secondPassed.transform.SetParent(prison.transform);
        secondPassed.transform.position = new Vector3(-34f, 1f, cz);
        BoxCollider spCol = secondPassed.AddComponent<BoxCollider>();
        spCol.isTrigger = true;
        spCol.size = new Vector3(2f, 2f, 8f);
        var spTrigger = secondPassed.AddComponent<TutorialHallTrigger>();
        var soSp = new SerializedObject(spTrigger);
        soSp.FindProperty("action").enumValueIndex = 2; // SecondGatePassed
        soSp.ApplyModifiedPropertiesWithoutUndo();

        // Trigger entering orb area.
        GameObject upgradeTrigger = new GameObject("Tutorial_UpgradeTrigger");
        upgradeTrigger.transform.SetParent(prison.transform);
        upgradeTrigger.transform.position = new Vector3(-32f, 1f, cz);
        BoxCollider upCol = upgradeTrigger.AddComponent<BoxCollider>();
        upCol.isTrigger = true;
        upCol.size = new Vector3(4f, 2f, 8f);
        var upTrigger = upgradeTrigger.AddComponent<TutorialHallTrigger>();
        var soUp = new SerializedObject(upTrigger);
        soUp.FindProperty("action").enumValueIndex = 3; // OrbAreaStart
        soUp.ApplyModifiedPropertiesWithoutUndo();

        // Orb spawn points (6 total) for the XP collection objective.
        GameObject orbSpawns = new GameObject("Tutorial_OrbSpawns");
        orbSpawns.transform.SetParent(prison.transform);
        Vector3[] orbPositions =
        {
            new Vector3(-28f, 0.5f, cz - 2.5f),
            new Vector3(-28f, 0.5f, cz),
            new Vector3(-28f, 0.5f, cz + 2.5f),
            new Vector3(-22f, 0.5f, cz - 2.5f),
            new Vector3(-22f, 0.5f, cz),
            new Vector3(-22f, 0.5f, cz + 2.5f)
        };
        for (int i = 0; i < orbPositions.Length; i++)
        {
            GameObject orbGo = new GameObject($"OrbSpawn_{i + 1}");
            orbGo.transform.SetParent(orbSpawns.transform);
            orbGo.transform.localPosition = orbPositions[i];
        }

        GameObject npcHintTrigger = new GameObject("Tutorial_NpcHintTrigger");
        npcHintTrigger.transform.SetParent(prison.transform);
        npcHintTrigger.transform.position = new Vector3(-2f, 1f, cz);
        BoxCollider hintCol = npcHintTrigger.AddComponent<BoxCollider>();
        hintCol.isTrigger = true;
        hintCol.size = new Vector3(8f, 2f, 2f);
        var hintTrigger = npcHintTrigger.AddComponent<TutorialHallTrigger>();
        var soHint = new SerializedObject(hintTrigger);
        soHint.FindProperty("action").enumValueIndex = 4; // NpcHint
        soHint.ApplyModifiedPropertiesWithoutUndo();

        Debug.Log($"[TutorialPrison] Rebuild complete. Floor X∈[-{hx:F1},{hx:F1}] Z∈[{floorNorthZ:F1},{floorSouthZ:F1}]. PlayerSpawn={TutorialPrisonLayout.PlayerSpawnPosition}.");
    }

    static void CreateHallGate(Transform parent, string name, Vector3 localPos, Material mat,
        float widthX = TutorialPrisonConstants.HallwayInnerWidthX,
        float depthZ = TutorialPrisonConstants.GateThicknessZ)
    {
        GameObject gate = GameObject.CreatePrimitive(PrimitiveType.Cube);
        gate.name = name;
        gate.transform.SetParent(parent);
        gate.transform.localPosition = localPos;
        gate.transform.localScale = new Vector3(widthX, 3f, depthZ);
        gate.GetComponent<Renderer>().sharedMaterial = mat;

        TutorialGate gateComp = gate.AddComponent<TutorialGate>();
        var so = new SerializedObject(gateComp);
        so.FindProperty("startOpen").boolValue = false;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>Gate blocking movement along X: thin in X, spans inner width across Z.</summary>
    static void CreateHallGateAcrossZ(Transform parent, string name, Vector3 localPos, Material mat,
        float spanZ, float thicknessX)
    {
        GameObject gate = GameObject.CreatePrimitive(PrimitiveType.Cube);
        gate.name = name;
        gate.transform.SetParent(parent);
        gate.transform.localPosition = localPos;
        gate.transform.localScale = new Vector3(thicknessX, 3f, spanZ);
        gate.GetComponent<Renderer>().sharedMaterial = mat;

        TutorialGate gateComp = gate.AddComponent<TutorialGate>();
        var so = new SerializedObject(gateComp);
        so.FindProperty("startOpen").boolValue = false;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>
    /// Floor-level black quads beside the corridor so other level geometry (brown ground) does not show.
    /// Walkway is long in X (~[-48,48]) and narrow in Z (~[-47,-33]) at CorridorCenterZ.
    /// </summary>
    static void CreateTutorialOutsideBlackPlates(Transform tutorialParent, Material voidMat)
    {
        float floorHalfX = TutorialPrisonLayout.FloorHalfX;
        const float plateThickness = 0.25f;
        const float plateY = -0.12f;
        const float plateZExtent = 100f;
        const float plateXExtent = 45f;
        float cz = TutorialPrisonLayout.CorridorCenterZ;

        GameObject west = GameObject.CreatePrimitive(PrimitiveType.Cube);
        west.name = "Tutorial_OutsideBlack_Left";
        west.transform.SetParent(tutorialParent);
        west.transform.position = new Vector3(-floorHalfX - plateXExtent * 0.5f - 0.25f, plateY, cz);
        west.transform.localScale = new Vector3(plateXExtent, plateThickness, plateZExtent);
        west.GetComponent<Renderer>().sharedMaterial = voidMat;

        GameObject east = GameObject.CreatePrimitive(PrimitiveType.Cube);
        east.name = "Tutorial_OutsideBlack_Right";
        east.transform.SetParent(tutorialParent);
        east.transform.position = new Vector3(floorHalfX + plateXExtent * 0.5f + 0.25f, plateY, cz);
        east.transform.localScale = new Vector3(plateXExtent, plateThickness, plateZExtent);
        east.GetComponent<Renderer>().sharedMaterial = voidMat;

        float floorNorthZ = TutorialPrisonLayout.CorridorCenterZ - TutorialPrisonLayout.FloorHalfZ;
        float floorSouthZ = TutorialPrisonLayout.CorridorCenterZ + TutorialPrisonLayout.FloorHalfZ;

        GameObject north = GameObject.CreatePrimitive(PrimitiveType.Cube);
        north.name = "Tutorial_OutsideBlack_North";
        north.transform.SetParent(tutorialParent);
        north.transform.position = new Vector3(0f, plateY, floorNorthZ - plateZExtent * 0.5f - 0.5f);
        north.transform.localScale = new Vector3(plateXExtent * 2f + 20f, plateThickness, plateZExtent);
        north.GetComponent<Renderer>().sharedMaterial = voidMat;

        GameObject south = GameObject.CreatePrimitive(PrimitiveType.Cube);
        south.name = "Tutorial_OutsideBlack_South";
        south.transform.SetParent(tutorialParent);
        south.transform.position = new Vector3(0f, plateY, floorSouthZ + plateZExtent * 0.5f + 0.5f);
        south.transform.localScale = new Vector3(plateXExtent * 2f + 20f, plateThickness, plateZExtent);
        south.GetComponent<Renderer>().sharedMaterial = voidMat;
    }

    static float SnapToTutorialGrid(float v)
    {
        float g = TutorialPrisonConstants.GridSnap;
        return Mathf.Round(v / g) * g;
    }

    /// <summary>Inner face of T_Wall_North (south-facing plane toward cells) — must match CreateTutorialGeometry floorNorthZ.</summary>
    static float GetNorthPerimeterInnerFaceZ()
    {
        return SnapToTutorialGrid(TutorialPrisonLayout.CorridorCenterZ - TutorialPrisonLayout.FloorHalfZ);
    }

    /// <summary>Inner face of T_Wall_South (north-facing plane toward cells) — must match CreateTutorialGeometry floorSouthZ.</summary>
    static float GetSouthPerimeterInnerFaceZ()
    {
        return SnapToTutorialGrid(TutorialPrisonLayout.CorridorCenterZ + TutorialPrisonLayout.FloorHalfZ);
    }

    /// <summary>
    /// One row of cells along +X at fixed Z (north or south of walkway). Each module has a unique X; Y is 0 for all roots.
    /// </summary>
    static GameObject CreateHorizontalPrisonRow(Transform parent, bool isNorthRow, Material wallMat, Material barMat, string rowName)
    {
        GameObject row = new GameObject(rowName);
        row.transform.SetParent(parent);
        row.transform.localPosition = Vector3.zero;
        row.transform.localRotation = Quaternion.identity;
        row.transform.localScale = Vector3.one;

        float halfCell = TutorialPrisonConstants.CellExtentX * 0.5f;
        float xStart = SnapToTutorialGrid(TutorialPrisonConstants.CellXMin + halfCell);
        float xLimit = SnapToTutorialGrid(TutorialPrisonConstants.CellXMax - halfCell);
        float step = SnapToTutorialGrid(TutorialPrisonConstants.CellSpacingX);
        if (step < 0.05f) step = TutorialPrisonConstants.GridSnap;

        int index = 0;
        string prefix = isNorthRow ? "Cell_T" : "Cell_B";
        for (float x = xStart; x <= xLimit + 0.001f; x += step)
        {
            x = SnapToTutorialGrid(x);
            index++;
            CreateOneDiscreteCellNorthSouth(row.transform, isNorthRow, x, wallMat, barMat, $"{prefix}_{index:000}");
        }

        return row;
    }

    /// <summary>
    /// Bars run along local X (bar-module spacing); end caps at ±X; back strip along Z — faces the walkway across Z.
    /// </summary>
    static void CreateOneDiscreteCellNorthSouth(Transform rowParent, bool isNorthRow, float xCenterWorld, Material wallMat, Material barMat, string cellName)
    {
        float cz = TutorialPrisonLayout.CorridorCenterZ;
        float zSign = isNorthRow ? -1f : 1f;

        float innerFaceZ = SnapToTutorialGrid(cz + zSign * TutorialPrisonConstants.CorridorInnerHalfZ);
        float protrusion = SnapToTutorialGrid(TutorialPrisonConstants.BarProtrusionIntoWalkway);
        // North row: +Z toward center; south row: −Z toward center.
        float barsZ = SnapToTutorialGrid(innerFaceZ - zSign * protrusion);

        // CellBack north/south outer edge flush with T_Wall inner face (same Z as floorNorthZ / floorSouthZ).
        float perimeterInnerZ = isNorthRow ? GetNorthPerimeterInnerFaceZ() : GetSouthPerimeterInnerFaceZ();
        float halfBackZ = SnapToTutorialGrid(TutorialPrisonConstants.BackStripThicknessX * 0.5f);
        float backZ = isNorthRow
            ? SnapToTutorialGrid(perimeterInnerZ + halfBackZ)
            : SnapToTutorialGrid(perimeterInnerZ - halfBackZ);

        float depthBarsToOuter = Mathf.Abs(barsZ - perimeterInnerZ);
        if (depthBarsToOuter < 0.08f) depthBarsToOuter = 0.08f;
        float midZ = SnapToTutorialGrid((barsZ + perimeterInnerZ) * 0.5f);
        xCenterWorld = SnapToTutorialGrid(xCenterWorld);

        GameObject cell = new GameObject(cellName);
        cell.transform.SetParent(rowParent);
        cell.transform.position = new Vector3(xCenterWorld, 0f, barsZ);
        cell.transform.localRotation = Quaternion.identity;
        cell.transform.localScale = Vector3.one;

        float halfSpan = (TutorialPrisonConstants.BarsPerCell - 1) * 0.5f * TutorialPrisonConstants.BarModuleWidth;
        halfSpan = SnapToTutorialGrid(halfSpan);
        for (int b = 0; b < TutorialPrisonConstants.BarsPerCell; b++)
        {
            float lx = SnapToTutorialGrid(-halfSpan + b * TutorialPrisonConstants.BarModuleWidth);
            GameObject bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bar.name = $"Bar_{b + 1}";
            bar.transform.SetParent(cell.transform);
            bar.transform.localPosition = new Vector3(lx, TutorialPrisonConstants.BarCenterY, 0f);
            bar.transform.localScale = new Vector3(TutorialPrisonConstants.BarPostThickness, TutorialPrisonConstants.BarHeight, TutorialPrisonConstants.BarPostThickness);
            bar.GetComponent<Renderer>().sharedMaterial = barMat;
        }

        float hx = TutorialPrisonConstants.CellExtentX * 0.5f;
        hx = SnapToTutorialGrid(hx);

        CreateWallWorld(cell.transform, "EndWall_NegX",
            new Vector3(xCenterWorld - hx, 1.5f, midZ),
            new Vector3(TutorialPrisonConstants.EndWallThicknessZ, 3f, depthBarsToOuter),
            wallMat);
        CreateWallWorld(cell.transform, "EndWall_PosX",
            new Vector3(xCenterWorld + hx, 1.5f, midZ),
            new Vector3(TutorialPrisonConstants.EndWallThicknessZ, 3f, depthBarsToOuter),
            wallMat);

        CreateWallWorld(cell.transform, "CellBack",
            new Vector3(xCenterWorld, 1.5f, backZ),
            new Vector3(TutorialPrisonConstants.CellExtentX, 3.5f, TutorialPrisonConstants.BackStripThicknessX),
            wallMat);
    }

    static void LogTutorialPrisonCellLayout(GameObject topRow, GameObject bottomRow)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("[TutorialPrison] Cell root world positions — X must step by CellSpacingX; Z constant per row; Y=0.");
        void AppendRow(string label, GameObject row)
        {
            if (row == null) return;
            sb.AppendLine($"  {label}:");
            foreach (Transform t in row.transform)
            {
                Vector3 p = t.position;
                sb.AppendLine($"    {t.name}: ({p.x:F2}, {p.y:F2}, {p.z:F2})");
            }
        }

        AppendRow("PrisonCells_Top (north)", topRow);
        AppendRow("PrisonCells_Bottom (south)", bottomRow);
        Debug.Log(sb.ToString());
    }

    static void CreateWallWorld(Transform parent, string name, Vector3 worldPosition, Vector3 scale, Material mat)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = name;
        wall.transform.SetParent(parent);
        wall.transform.position = worldPosition;
        wall.transform.localScale = scale;
        wall.GetComponent<Renderer>().sharedMaterial = mat;
    }

    static void CreateWall(Transform parent, string name, Vector3 position, Vector3 scale, Material mat)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = name;
        wall.transform.SetParent(parent);
        wall.transform.localPosition = position;
        wall.transform.localScale = scale;
        wall.GetComponent<Renderer>().sharedMaterial = mat;
    }

    static void CreateWorldLabel(Transform parent, string name, string label, Vector3 localPos, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

        TextMesh tm = go.AddComponent<TextMesh>();
        tm.text = label;
        tm.characterSize = 0.22f;
        tm.fontSize = 64;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = color;
        go.AddComponent<Billboard>();
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

        // Visual (sprite) as a child — Billboard makes it face the camera.
        // The sprite pivot is centred, so without a Y offset the bottom half sits below the floor.
        // Raise by half the desired world height so the base aligns with y=0.
        float ppu = s_obstacleSprite.pixelsPerUnit;
        float px = Mathf.Max(s_obstacleSprite.rect.width, s_obstacleSprite.rect.height);
        float spriteWorldSize = px / ppu;
        float desiredWorldSize = scaleXZ * 1.8f;
        float visualScale = desiredWorldSize / spriteWorldSize;
        GameObject visual = new GameObject("Visual");
        visual.transform.SetParent(go.transform);
        // Raise by half the sprite world size so its bottom edge sits on the ground.
        visual.transform.localPosition = new Vector3(0f, desiredWorldSize * 0.5f, 0f);
        visual.transform.localRotation = Quaternion.identity; // Billboard handles camera-facing
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

    // ── Arena 2 Flame Traps ───────────────────────────────────────────────

    static void CreateFlameTraps()
    {
        GameObject root = new GameObject("Traps");
        root.transform.SetParent(levelRoot);

        // Eight flame traps arranged in two concentric rings around the arena floor.
        // Positions are inside Arena 2's octagon (radius ~37) but far enough from the
        // centre to leave a clear combat lane for the player.
        Vector3[] positions = new[]
        {
            new Vector3(  0f, 0.3f,  22f),   // N
            new Vector3(  0f, 0.3f, -22f),   // S
            new Vector3( 22f, 0.3f,   0f),   // E
            new Vector3(-22f, 0.3f,   0f),   // W
            new Vector3( 16f, 0.3f,  16f),   // NE
            new Vector3(-16f, 0.3f,  16f),   // NW
            new Vector3( 16f, 0.3f, -16f),   // SE
            new Vector3(-16f, 0.3f, -16f),   // SW
        };

        for (int i = 0; i < positions.Length; i++)
        {
            GameObject trap = new GameObject($"FlameTrap_{i + 1}");
            trap.transform.SetParent(root.transform);
            trap.transform.position = positions[i];

            BoxCollider trigger = trap.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.size   = new Vector3(2.5f, 0.5f, 2.5f);
            trigger.center = Vector3.zero;

            trap.AddComponent<FlameTrap>();

            // Visual child – flame animation frames wired by SpriteSetup.
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
