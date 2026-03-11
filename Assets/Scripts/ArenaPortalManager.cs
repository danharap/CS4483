using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.AI.Navigation;

/// <summary>
/// After wave 5 is cleared, spawns a portal. When the player enters it, transitions to Arena 2
/// (different arena root with different obstacles and background). Also supports debug: spawn portal on F5.
/// </summary>
public class ArenaPortalManager : MonoBehaviour
{
    public static ArenaPortalManager Instance { get; private set; }

    [Header("Arena Roots")]
    [SerializeField] private GameObject arena1Root;
    [SerializeField] private GameObject arena2Root;

    [Header("Portal")]
    [SerializeField] private Vector3 portalSpawnPosition = new Vector3(0f, 1f, -10f);
    [SerializeField] private Vector3 portalScale = new Vector3(3f, 3f, 0.5f);
    [SerializeField] private Color portalColor = new Color(0.4f, 0.2f, 0.8f, 0.9f);

    [Header("Arena 2 Player Spawn")]
    [SerializeField] private Vector3 arena2PlayerSpawnPosition = new Vector3(0f, 1.1f, 0f);

    [Header("References")]
    [SerializeField] private EnemySpawner spawner;

    private GameObject portalInstance;
    private const string Arena1Name = "=== LEVEL (ProBuilder) ===";
    private const string Arena2Name = "=== LEVEL (ProBuilder) Arena2 ===";
    private const int PortalAfterWaveIndex = 4; // 0-based: wave 5 cleared

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (arena1Root == null) arena1Root = FindRootByName(Arena1Name);
        if (arena2Root == null) arena2Root = FindRootByName(Arena2Name);
        if (spawner == null) spawner = FindFirstObjectByType<EnemySpawner>();

        WaveManager wm = GameManager.Instance?.WaveManager;
        if (wm != null)
            wm.OnWaveCleared += OnWaveCleared;
    }

    /// <summary>
    /// Find a root GameObject by name. Works for inactive objects (unlike GameObject.Find).
    /// </summary>
    private static GameObject FindRootByName(string name)
    {
        Scene scene = SceneManager.GetActiveScene();
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.name == name) return root;
        }
        return null;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        var wm = GameManager.Instance?.WaveManager;
        if (wm != null)
            wm.OnWaveCleared -= OnWaveCleared;
    }

    void Update()
    {
        // Debug: F5 = spawn portal to test without playing 5 waves
        if (Input.GetKeyDown(KeyCode.F5))
            SpawnPortalForDebug();
    }

    private void OnWaveCleared(int waveIndex)
    {
        if (waveIndex == PortalAfterWaveIndex)
            SpawnPortal();
    }

    /// <summary>
    /// Call from menu or F5 to test the portal without clearing 5 waves.
    /// </summary>
    public void SpawnPortalForDebug()
    {
        SpawnPortal();
    }

    private void SpawnPortal()
    {
        if (portalInstance != null) return;

        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "Portal";
        go.transform.position = portalSpawnPosition;
        go.transform.localScale = portalScale;

        Collider c = go.GetComponent<Collider>();
        if (c != null) c.isTrigger = true;

        // Trigger events need a Rigidbody on at least one side; player has CharacterController, so add kinematic Rigidbody to portal
        Rigidbody rb = go.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        Renderer r = go.GetComponent<Renderer>();
        if (r != null)
        {
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = portalColor;
            r.material = mat;
        }

        go.AddComponent<Portal>();
        portalInstance = go;
    }

    /// <summary>
    /// Called by Portal when the player enters. Switches to Arena 2 and teleports the player.
    /// </summary>
    public void TransitionToArena2()
    {
        if (arena2Root == null)
        {
            arena2Root = FindRootByName(Arena2Name);
            if (arena2Root == null)
            {
                Debug.LogWarning("[ArenaPortalManager] Arena 2 not found. Build it via CS4483 → 2 - Build Arena 2, then run Setup Everything.");
                return;
            }
        }

        if (arena1Root == null) arena1Root = FindRootByName(Arena1Name);

        // Disable Arena 1, enable Arena 2
        if (arena1Root != null) arena1Root.SetActive(false);
        arena2Root.SetActive(true);

        // Bake NavMesh for Arena 2 so enemies can path (once at first transition)
        NavMeshSurface surface = arena2Root.GetComponent<NavMeshSurface>();
        if (surface != null)
            surface.BuildNavMesh();

        // Teleport player to Arena 2 spawn
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            player.transform.position = arena2PlayerSpawnPosition;

        // Switch spawner to Arena 2 spawn points
        if (spawner != null)
            spawner.UseArena2Spawns();

        // Set wave index so next wave is 6 (break will increment to 5, then wave 6 runs)
        WaveManager wm = GameManager.Instance?.WaveManager;
        if (wm != null)
        {
            wm.SetWaveIndex(4);
            GameManager.Instance?.HUD?.UpdateWaveNumber(5);
        }

        // Remove portal (one-time transition)
        if (portalInstance != null)
        {
            Destroy(portalInstance);
            portalInstance = null;
        }

        GameManager.Instance?.HUD?.ShowTransition("Arena 2! Survive...");
    }
}
