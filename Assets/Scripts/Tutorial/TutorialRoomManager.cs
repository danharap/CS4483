using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// Handles transitions:
/// Lobby -> Tutorial room (jail-cell walkthrough) -> Lobby.
/// </summary>
public class TutorialRoomManager : MonoBehaviour
{
    public static TutorialRoomManager Instance { get; private set; }

    [Header("Roots")]
    [SerializeField] private GameObject lobbyRoot;
    [SerializeField] private GameObject tutorialRoot;
    [SerializeField] private GameObject arena1Root;
    [SerializeField] private GameObject arena2Root;

    [Header("Tutorial Enemy")]
    [SerializeField] private GameObject tutorialEnemyPrefab;
    [SerializeField] private Transform tutorialEnemySpawnPoint;
    [SerializeField] private GameObject tutorialXpOrbPrefab;
    [SerializeField] private GameObject tutorialHealthPackPrefab;

    [Header("Spawns")]
    [Tooltip("Fallback if Tutorial_PlayerSpawn is missing from the scene.")]
    [SerializeField] private Vector3 tutorialSpawnPosition = new Vector3(-74f, 1.1f, -40f);
    [SerializeField] private Vector3 lobbyReturnSpawnPosition = new Vector3(0f, 1.1f, 0f);

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void EnterTutorialFromLobby()
    {
        // Always resolve roots from the scene — serialized references go stale after Setup All / rebuilds.
        lobbyRoot = FindSceneRoot("=== LEVEL (Lobby) ===");
        tutorialRoot = FindSceneRoot("=== LEVEL (Tutorial) ===");
        arena1Root = FindSceneRoot("=== LEVEL (ProBuilder) ===");
        arena2Root = FindSceneRoot("=== LEVEL (ProBuilder) Arena2 ===");

        EnsureRuntimeTutorialIfMissing();
        tutorialRoot = FindSceneRoot("=== LEVEL (Tutorial) ===");

        if (tutorialEnemySpawnPoint == null)
        {
            GameObject spawnGO = FindAnyByName("Tutorial_EnemySpawn");
            if (spawnGO != null) tutorialEnemySpawnPoint = spawnGO.transform;
        }

        if (lobbyRoot != null)
            lobbyRoot.SetActive(false);
        if (tutorialRoot != null)
            tutorialRoot.SetActive(true);
        // Disable arenas by scene lookup too — if arena1Root was null, brown arena geometry could stay visible.
        if (arena1Root != null)
            arena1Root.SetActive(false);
        else
            SetRootActiveByName("=== LEVEL (ProBuilder) ===", false);
        if (arena2Root != null)
            arena2Root.SetActive(false);
        else
            SetRootActiveByName("=== LEVEL (ProBuilder) Arena2 ===", false);

        ClearLiveEnemiesAndProjectiles();
        GameManager.Instance?.WaveManager?.ResetToFirstWave();

        // Tutorial requires shooting for the combat lesson.
        GameManager.Instance?.PlayerWeapon?.SetShootingEnabled(true);

        TeleportPlayer(ResolveTutorialPlayerSpawn());
        // Use explicit Unity null check (not ?.) to catch destroyed-but-not-C#-null instances
        if (TutorialManager.Instance != null)
        {
            // Pass the health pack prefab so the medkit tutorial step can spawn it.
            if (tutorialHealthPackPrefab != null)
                TutorialManager.Instance.healthPackPrefab = tutorialHealthPackPrefab;

            TutorialManager.Instance.BeginTutorialRoom(tutorialEnemyPrefab, tutorialEnemySpawnPoint, tutorialXpOrbPrefab);
        }
        else
            Debug.LogWarning("[TutorialRoomManager] TutorialManager.Instance is null — tutorial intro will not play.");
    }

    public void ExitTutorialToLobby()
    {
        tutorialRoot = FindSceneRoot("=== LEVEL (Tutorial) ===");
        lobbyRoot = FindSceneRoot("=== LEVEL (Lobby) ===");
        arena1Root = FindSceneRoot("=== LEVEL (ProBuilder) ===");
        arena2Root = FindSceneRoot("=== LEVEL (ProBuilder) Arena2 ===");

        if (tutorialRoot != null)
            tutorialRoot.SetActive(false);
        if (lobbyRoot != null)
            lobbyRoot.SetActive(true);
        if (arena1Root != null)
            arena1Root.SetActive(false);
        if (arena2Root != null)
            arena2Root.SetActive(false);

        ClearLiveEnemiesAndProjectiles();
        GameManager.Instance?.WaveManager?.ResetToFirstWave();

        // Back in lobby: lock shooting until arena.
        GameManager.Instance?.PlayerWeapon?.SetShootingEnabled(false);

        TeleportPlayer(lobbyReturnSpawnPosition);

        // TransitionToLobby shows the "speak with the Guide" message briefly instead
        // of just silently hiding the tutorial UI.
        if (TutorialManager.Instance != null)
            TutorialManager.Instance.TransitionToLobby();
        else
            Debug.LogWarning("[TutorialRoomManager] TutorialManager.Instance is null on exit.");

        GameplayMusicController.Instance?.PlayLobby();
    }

    private void EnsureRuntimeTutorialIfMissing()
    {
        tutorialRoot = FindSceneRoot("=== LEVEL (Tutorial) ===");

        // Full prison: must have bar rows (Setup All / ProBuilder). Never strip that.
        if (FindAnyByName("PrisonCells_Top") != null || FindAnyByName("PrisonCells_Bottom") != null)
            return;
        if (FindAnyByName("PrisonCells_Left") != null || FindAnyByName("PrisonCells_Right") != null)
            return;
        if (FindAnyByName("CellRow_Left") != null || FindAnyByName("CellRow_Right") != null)
            return;

        if (FindAnyByName("Tutorial_Prison") != null || FindAnyByName("TutorialArea") != null)
        {
            if (FindAnyByName("PrisonCells_Top") == null)
                Debug.LogWarning("[TutorialRoomManager] Tutorial_Prison/TutorialArea exists but PrisonCells_Top is missing. Run CS4483 → SETUP EVERYTHING to rebuild prison geometry.");
            return;
        }

        // Runtime fallback corridor (no editor-built Tutorial_Prison): keep if complete.
        if (tutorialRoot != null &&
            FindAnyByName("Tutorial_Floor") != null &&
            FindAnyByName("Tutorial_Gate_3") != null &&
            FindAnyByName("Tutorial_OrbSpawns") != null &&
            FindAnyByName("Tutorial_FirstCheckpoint") != null)
            return;

        if (tutorialRoot != null)
            Destroy(tutorialRoot);

        tutorialRoot = new GameObject("=== LEVEL (Tutorial) ===");
        tutorialRoot.SetActive(false);

        GameObject prison = new GameObject("Tutorial_Prison");
        prison.transform.SetParent(tutorialRoot.transform);
        prison.transform.localPosition = Vector3.zero;

        float cz = TutorialPrisonLayout.CorridorCenterZ;
        float hx = TutorialPrisonLayout.FloorHalfX;
        float hz = TutorialPrisonLayout.FloorHalfZ;
        float spanX = hx * 2f;

        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "Tutorial_Floor";
        floor.transform.SetParent(prison.transform);
        floor.transform.position = new Vector3(0f, -0.2f, cz);
        floor.transform.localScale = new Vector3(spanX, 0.4f, hz * 2f);

        GameObject floorVis = GameObject.CreatePrimitive(PrimitiveType.Quad);
        floorVis.name = "Tutorial_FloorVisual";
        floorVis.transform.SetParent(prison.transform, false);
        floorVis.transform.position = new Vector3(0f, 0.03f, cz);
        floorVis.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        floorVis.transform.localScale = new Vector3(spanX, hz * 2f, 1f);
        Object.Destroy(floorVis.GetComponent<Collider>());
        Renderer fr = floorVis.GetComponent<Renderer>();
        Material fm = new Material(Shader.Find("Standard"));
        fm.color = new Color(0.45f, 0.45f, 0.48f);
        fr.material = fm;
        fr.shadowCastingMode = ShadowCastingMode.Off;

        Transform pt = prison.transform;
        CreateRuntimeWall("West", new Vector3(-hx - 0.25f, 2f, cz), new Vector3(0.5f, 4f, hz * 2f), pt);
        CreateRuntimeWall("East", new Vector3(hx + 0.25f, 2f, cz), new Vector3(0.5f, 4f, hz * 2f), pt);
        CreateRuntimeWall("North", new Vector3(0f, 2f, cz - hz - 0.25f), new Vector3(spanX, 4f, 0.5f), pt);
        CreateRuntimeWall("South", new Vector3(0f, 2f, cz + hz + 0.25f), new Vector3(spanX, 4f, 0.5f), pt);

        GameObject enemySpawn = new GameObject("Tutorial_EnemySpawn");
        enemySpawn.transform.SetParent(prison.transform);
        enemySpawn.transform.position = new Vector3(-62f, 1f, cz);
        tutorialEnemySpawnPoint = enemySpawn.transform;

        GameObject medkitSpawn = new GameObject("Tutorial_MedkitSpawn");
        medkitSpawn.transform.SetParent(prison.transform);
        medkitSpawn.transform.position = new Vector3(-16f, 0.5f, cz);

        GameObject spawnMarker = new GameObject("Tutorial_PlayerSpawn");
        spawnMarker.transform.SetParent(prison.transform);
        spawnMarker.transform.position = TutorialPrisonLayout.PlayerSpawnPosition;

        CreateRuntimeGate("Tutorial_Gate_1", new Vector3(-70f, 1.5f, cz), pt);
        CreateRuntimeGate("Tutorial_Gate_2", new Vector3(-40f, 1.5f, cz), pt);
        CreateRuntimeGate("Tutorial_Gate_3", new Vector3(-10f, 1.5f, cz), pt);

        CreateRuntimeTrigger("Tutorial_FirstCheckpoint", new Vector3(-76f, 1f, cz), new Vector3(2f, 2f, 8f), 0, pt);
        CreateRuntimeTrigger("Tutorial_FirstGatePassed", new Vector3(-64f, 1f, cz), new Vector3(2f, 2f, 8f), 1, pt);
        CreateRuntimeTrigger("Tutorial_SecondGatePassed", new Vector3(-34f, 1f, cz), new Vector3(2f, 2f, 8f), 2, pt);
        CreateRuntimeTrigger("Tutorial_UpgradeTrigger", new Vector3(-32f, 1f, cz), new Vector3(4f, 2f, 8f), 3, pt);
        CreateRuntimeTrigger("Tutorial_NpcHintTrigger", new Vector3(-2f, 1f, cz), new Vector3(8f, 2f, 2f), 4, pt);

        // Orb spawn points
        GameObject orbSpawns = new GameObject("Tutorial_OrbSpawns");
        orbSpawns.transform.SetParent(prison.transform);
        Vector3[] orbPositions =
        {
            new Vector3(-28f, 0.5f, cz - 2.5f),
            new Vector3(-28f, 0.5f, cz),
            new Vector3(-28f, 0.5f, cz + 2.5f),
            new Vector3(-22f, 0.5f, cz - 2.5f),
            new Vector3(-22f, 0.5f, cz),
            new Vector3(-22f, 0.5f, cz + 2.5f),
        };
        for (int i = 0; i < orbPositions.Length; i++)
        {
            GameObject p = new GameObject($"OrbSpawn_{i + 1}");
            p.transform.SetParent(orbSpawns.transform);
            p.transform.localPosition = orbPositions[i];
        }

        // NOTE: Removed the tutorial guide NPC from inside the prison tutorial area.

        // Exit portal back to lobby — placed close to gate 3 so the post-upgrade hallway is short.
        GameObject exitPortal = GameObject.CreatePrimitive(PrimitiveType.Cube);
        exitPortal.name = "TutorialToArena_Portal";
        exitPortal.transform.SetParent(prison.transform);
        exitPortal.transform.position = new Vector3(TutorialPrisonLayout.TutorialExitPortalCenterX, 1.5f, TutorialPrisonLayout.TutorialExitPortalCenterZ);
        exitPortal.transform.localScale = new Vector3(2f, 3f, 0.3f);
        BoxCollider exitCol = exitPortal.GetComponent<BoxCollider>();
        exitCol.isTrigger = true;
        Rigidbody exitRb = exitPortal.AddComponent<Rigidbody>();
        exitRb.isKinematic = true;
        exitRb.useGravity = false;
        exitPortal.AddComponent<TutorialExitPortal>();

        Sprite stonePortal = Resources.Load<Sprite>("portals/stone_portal_gate");
        LobbyPortalVisual.ApplyIfNeeded(exitPortal, stonePortal);
    }

    private static GameObject FindSceneRoot(string name)
    {
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            if (root.name == name) return root;
        return null;
    }

    static void SetRootActiveByName(string name, bool active)
    {
        GameObject go = FindSceneRoot(name);
        if (go != null) go.SetActive(active);
    }

    private static GameObject FindAnyByName(string name)
    {
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            Transform t = root.transform.Find(name);
            if (t != null) return t.gameObject;
            foreach (var child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == name) return child.gameObject;
            }
        }
        return null;
    }

    private void TeleportPlayer(Vector3 pos)
    {
        CharacterController cc = FindFirstObjectByType<CharacterController>();
        if (cc == null) return;
        cc.enabled = false;
        cc.transform.position = pos;
        cc.enabled = true;
    }

    Vector3 ResolveTutorialPlayerSpawn()
    {
        GameObject sp = FindAnyByName("Tutorial_PlayerSpawn");
        if (sp != null) return sp.transform.position;
        return tutorialSpawnPosition;
    }

    private void CreateRuntimeWall(string suffix, Vector3 position, Vector3 scale, Transform parent)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = $"Tutorial_Wall_{suffix}";
        wall.transform.SetParent(parent);
        wall.transform.position = position;
        wall.transform.localScale = scale;
    }

    private void CreateRuntimeGate(string gateName, Vector3 position, Transform parent)
    {
        GameObject gate = GameObject.CreatePrimitive(PrimitiveType.Cube);
        gate.name = gateName;
        gate.transform.SetParent(parent);
        gate.transform.position = position;
        gate.transform.localScale = new Vector3(0.4f, 3f, 13.5f);
        gate.AddComponent<TutorialGate>();
    }

    private void CreateRuntimeTrigger(string name, Vector3 position, Vector3 size, int actionIndex, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.position = position;
        BoxCollider col = go.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.size = size;
        TutorialHallTrigger trigger = go.AddComponent<TutorialHallTrigger>();
        trigger.SetActionByIndex(actionIndex);
    }

    private void ClearLiveEnemiesAndProjectiles()
    {
        foreach (EnemyBase enemy in FindObjectsByType<EnemyBase>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            Destroy(enemy.gameObject);

        foreach (Projectile proj in FindObjectsByType<Projectile>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            Destroy(proj.gameObject);

        EnemyRegistry.Clear();
    }
}

