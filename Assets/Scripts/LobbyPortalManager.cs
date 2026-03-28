using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages transitions from the Lobby level into Arena 1.
///
/// NavMesh note:
///   The Arena 1 NavMesh is baked during editor setup with CollectObjects.All, which captures
///   EVERY physics collider in the scene — including Lobby walls.  After the Lobby is hidden,
///   those baked impassable areas remain unless the NavMesh is explicitly rebuilt.
///   EnterArenaFromLobby() disables the Lobby, then rebakes so enemies can navigate freely.
/// </summary>
public class LobbyPortalManager : MonoBehaviour
{
    public static LobbyPortalManager Instance { get; private set; }

    [Header("Roots")]
    [SerializeField] private GameObject lobbyRoot;
    [SerializeField] private GameObject arena1Root;

    [Header("Arena 1 Player Spawn")]
    [SerializeField] private Vector3 arenaSpawnPosition = new Vector3(0f, 1.1f, 0f);

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void EnterArenaFromLobby()
    {
        // Resolve refs (handles inactive objects that GameObject.Find would miss)
        if (lobbyRoot  == null) lobbyRoot  = FindSceneRoot("=== LEVEL (Lobby) ===");
        if (arena1Root == null) arena1Root = FindSceneRoot("=== LEVEL (ProBuilder) ===");

        // ── Hide the Lobby completely ─────────────────────────────────────
        if (lobbyRoot != null)
        {
            // Disable every Lobby collider (do NOT Destroy them).
            // LobbyPortal uses [RequireComponent(typeof(Collider))] — destroying that BoxCollider
            // would fail with "Can't remove BoxCollider because LobbyPortal depends on it".
            // Disabled colliders block nothing in physics and are typically ignored by NavMesh bakes.
            foreach (Collider col in lobbyRoot.GetComponentsInChildren<Collider>(true))
                col.enabled = false;

            lobbyRoot.SetActive(false);
            Debug.Log("[LobbyPortalManager] Lobby hidden — all lobby colliders disabled.");
        }

        // ── Show Arena 1 ──────────────────────────────────────────────────
        if (arena1Root != null) arena1Root.SetActive(true);

        // ── Move player ───────────────────────────────────────────────────
        var cc = FindFirstObjectByType<CharacterController>();
        if (cc != null)
        {
            cc.enabled = false;
            cc.transform.position = arenaSpawnPosition;
            cc.enabled = true;
        }

        // ── Rebake Arena 1 NavMesh WITHOUT lobby walls ────────────────────
        // The bake runs synchronously here (brief stutter on transition is acceptable
        // since the screen transition hides it).  After rebuilding, the space formerly
        // occupied by the Lobby walls becomes walkable so enemies can navigate freely.
        RebakeArena1NavMesh();

        // ── Start waves ───────────────────────────────────────────────────
        GameManager.Instance?.WaveManager?.BeginWaves();
    }

    /// <summary>
    /// Re-enable all lobby colliders that were disabled when entering Arena 1.
    /// Must be called every time the player respawns to lobby so the LobbyPortal trigger works again.
    /// </summary>
    public void ResetForRespawn()
    {
        if (lobbyRoot == null) lobbyRoot = FindSceneRoot("=== LEVEL (Lobby) ===");
        if (lobbyRoot == null) return;

        foreach (Collider col in lobbyRoot.GetComponentsInChildren<Collider>(true))
            col.enabled = true;

        Debug.Log("[LobbyPortalManager] Lobby colliders re-enabled for respawn.");
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    static GameObject FindSceneRoot(string name)
    {
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
            if (root.name == name) return root;
        return null;
    }

    static void RebakeArena1NavMesh()
    {
        // Prefer a NavMeshSurface component on the Arena 1 root (added by SetupAll).
        // Fall back to searching the whole scene if the component is on a child.
        NavMeshSurface surface = null;

        GameObject arena1 = FindSceneRoot("=== LEVEL (ProBuilder) ===");
        if (arena1 != null)
            surface = arena1.GetComponent<NavMeshSurface>();

        if (surface == null)
            surface = FindFirstObjectByType<NavMeshSurface>();

        if (surface != null)
        {
            surface.BuildNavMesh();
            Debug.Log("[LobbyPortalManager] Arena 1 NavMesh rebuilt — lobby walls no longer block enemy paths.");
        }
        else
        {
            Debug.LogWarning("[LobbyPortalManager] No NavMeshSurface found — enemies may still be blocked " +
                             "by old lobby NavMesh data. Run CS4483 → SETUP EVERYTHING to bake NavMesh.");
        }
    }
}
