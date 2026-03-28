using UnityEngine;

/// <summary>
/// Portal in the lobby that teleports the player into the main arena.
/// </summary>
[RequireComponent(typeof(Collider))]
public class LobbyPortal : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        LobbyPortalManager mgr = LobbyPortalManager.Instance;
        if (mgr != null)
            mgr.EnterArenaFromLobby();
        else
            Debug.LogWarning("[LobbyPortal] LobbyPortalManager.Instance is null.");
    }
}

