using UnityEngine;
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

    [Header("Spawns")]
    [SerializeField] private Vector3 tutorialSpawnPosition = new Vector3(0f, 1.1f, -84f);
    [SerializeField] private Vector3 lobbyReturnSpawnPosition = new Vector3(0f, 1.1f, 0f);

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void EnterTutorialFromLobby()
    {
        if (lobbyRoot == null) lobbyRoot = FindSceneRoot("=== LEVEL (Lobby) ===");
        if (tutorialRoot == null) tutorialRoot = FindSceneRoot("=== LEVEL (Tutorial) ===");
        if (arena1Root == null) arena1Root = FindSceneRoot("=== LEVEL (ProBuilder) ===");
        if (arena2Root == null) arena2Root = FindSceneRoot("=== LEVEL (ProBuilder) Arena2 ===");

        EnsureRuntimeTutorialIfMissing();
        if (tutorialEnemySpawnPoint == null)
        {
            GameObject spawnGO = FindAnyByName("Tutorial_EnemySpawn");
            if (spawnGO != null) tutorialEnemySpawnPoint = spawnGO.transform;
        }

        if (lobbyRoot != null)
            lobbyRoot.SetActive(false);
        if (tutorialRoot != null)
            tutorialRoot.SetActive(true);
        if (arena1Root != null)
            arena1Root.SetActive(false);
        if (arena2Root != null)
            arena2Root.SetActive(false);

        ClearLiveEnemiesAndProjectiles();
        GameManager.Instance?.WaveManager?.ResetToFirstWave();

        // Tutorial requires shooting for the combat lesson.
        GameManager.Instance?.PlayerWeapon?.SetShootingEnabled(true);

        TeleportPlayer(tutorialSpawnPosition);
        // Use explicit Unity null check (not ?.) to catch destroyed-but-not-C#-null instances
        if (TutorialManager.Instance != null)
            TutorialManager.Instance.BeginTutorialRoom(tutorialEnemyPrefab, tutorialEnemySpawnPoint, tutorialXpOrbPrefab);
        else
            Debug.LogWarning("[TutorialRoomManager] TutorialManager.Instance is null — tutorial intro will not play.");
    }

    public void ExitTutorialToLobby()
    {
        if (tutorialRoot == null) tutorialRoot = FindSceneRoot("=== LEVEL (Tutorial) ===");
        if (lobbyRoot == null) lobbyRoot = FindSceneRoot("=== LEVEL (Lobby) ===");

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
        TutorialManager.Instance?.EndTutorialRoom();
    }

    private void EnsureRuntimeTutorialIfMissing()
    {
        if (tutorialRoot != null &&
            FindAnyByName("Tutorial_Floor") != null &&
            FindAnyByName("Tutorial_Gate_3") != null &&
            FindAnyByName("Tutorial_OrbSpawns") != null &&
            FindAnyByName("Tutorial_FirstCheckpoint") != null)
            return;
        if (tutorialRoot != null) Destroy(tutorialRoot);

        tutorialRoot = new GameObject("=== LEVEL (Tutorial) ===");
        tutorialRoot.SetActive(false);

        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "Tutorial_Floor";
        floor.transform.SetParent(tutorialRoot.transform);
        floor.transform.position = new Vector3(0f, -0.2f, -40f);
        floor.transform.localScale = new Vector3(12f, 0.4f, 96f);

        CreateRuntimeWall("L", new Vector3(-6f, 2f, -40f), new Vector3(0.5f, 4f, 96f));
        CreateRuntimeWall("R", new Vector3(6f, 2f, -40f), new Vector3(0.5f, 4f, 96f));
        CreateRuntimeWall("Start", new Vector3(0f, 2f, -88f), new Vector3(12f, 4f, 0.5f));
        CreateRuntimeWall("End", new Vector3(0f, 2f, 8f), new Vector3(12f, 4f, 0.5f));

        GameObject enemySpawn = new GameObject("Tutorial_EnemySpawn");
        enemySpawn.transform.SetParent(tutorialRoot.transform);
        enemySpawn.transform.position = new Vector3(0f, 1f, -62f);
        tutorialEnemySpawnPoint = enemySpawn.transform;

        CreateRuntimeGate("Tutorial_Gate_1", new Vector3(0f, 1.5f, -70f));
        CreateRuntimeGate("Tutorial_Gate_2", new Vector3(0f, 1.5f, -40f));
        CreateRuntimeGate("Tutorial_Gate_3", new Vector3(0f, 1.5f, -10f));

        // First checkpoint trigger
        CreateRuntimeTrigger("Tutorial_FirstCheckpoint", new Vector3(0f, 1f, -76f), new Vector3(6f, 2f, 2f), 0);
        CreateRuntimeTrigger("Tutorial_FirstGatePassed", new Vector3(0f, 1f, -64f), new Vector3(6f, 2f, 2f), 1);
        CreateRuntimeTrigger("Tutorial_SecondGatePassed", new Vector3(0f, 1f, -34f), new Vector3(6f, 2f, 2f), 2);
        CreateRuntimeTrigger("Tutorial_UpgradeTrigger", new Vector3(0f, 1f, -32f), new Vector3(8f, 2f, 4f), 3);
        CreateRuntimeTrigger("Tutorial_NpcHintTrigger", new Vector3(0f, 1f, -2f), new Vector3(6f, 2f, 2f), 4);

        // Orb spawn points
        GameObject orbSpawns = new GameObject("Tutorial_OrbSpawns");
        orbSpawns.transform.SetParent(tutorialRoot.transform);
        Vector3[] orbPositions =
        {
            new Vector3(-2.5f, 0.5f, -28f),
            new Vector3( 0.0f, 0.5f, -28f),
            new Vector3( 2.5f, 0.5f, -28f),
            new Vector3(-2.5f, 0.5f, -22f),
            new Vector3( 0.0f, 0.5f, -22f),
            new Vector3( 2.5f, 0.5f, -22f),
        };
        for (int i = 0; i < orbPositions.Length; i++)
        {
            GameObject p = new GameObject($"OrbSpawn_{i + 1}");
            p.transform.SetParent(orbSpawns.transform);
            p.transform.localPosition = orbPositions[i];
        }

        // NOTE: Removed the tutorial guide NPC from inside the prison tutorial area.

        // Exit portal back to lobby
        GameObject exitPortal = GameObject.CreatePrimitive(PrimitiveType.Cube);
        exitPortal.name = "TutorialToArena_Portal";
        exitPortal.transform.SetParent(tutorialRoot.transform);
        exitPortal.transform.position = new Vector3(0f, 1.5f, 7.3f);
        exitPortal.transform.localScale = new Vector3(2f, 3f, 0.3f);
        BoxCollider exitCol = exitPortal.GetComponent<BoxCollider>();
        exitCol.isTrigger = true;
        Rigidbody exitRb = exitPortal.AddComponent<Rigidbody>();
        exitRb.isKinematic = true;
        exitRb.useGravity = false;
        exitPortal.AddComponent<TutorialExitPortal>();
    }

    private static GameObject FindSceneRoot(string name)
    {
        foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            if (root.name == name) return root;
        return null;
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

    private void CreateRuntimeWall(string suffix, Vector3 position, Vector3 scale)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = $"Tutorial_Wall_{suffix}";
        wall.transform.SetParent(tutorialRoot.transform);
        wall.transform.position = position;
        wall.transform.localScale = scale;
    }

    private void CreateRuntimeGate(string gateName, Vector3 position)
    {
        GameObject gate = GameObject.CreatePrimitive(PrimitiveType.Cube);
        gate.name = gateName;
        gate.transform.SetParent(tutorialRoot.transform);
        gate.transform.position = position;
        gate.transform.localScale = new Vector3(11f, 3f, 0.4f);
        gate.AddComponent<TutorialGate>();
    }

    private void CreateRuntimeTrigger(string name, Vector3 position, Vector3 size, int actionIndex)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(tutorialRoot.transform);
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

