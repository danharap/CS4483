using UnityEngine;

/// <summary>
/// Manages transitions from the Lobby level into Arena 1.
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
        if (lobbyRoot == null)
            lobbyRoot = GameObject.Find("=== LEVEL (Lobby) ===");
        if (arena1Root == null)
            arena1Root = GameObject.Find("=== LEVEL (ProBuilder) ===");

        if (lobbyRoot != null) lobbyRoot.SetActive(false);
        if (arena1Root != null) arena1Root.SetActive(true);

        var cc = FindObjectOfType<CharacterController>();
        if (cc != null)
        {
            cc.enabled = false;
            cc.transform.position = arenaSpawnPosition;
            cc.enabled = true;
        }

        GameManager.Instance?.WaveManager?.BeginWaves();
    }
}

