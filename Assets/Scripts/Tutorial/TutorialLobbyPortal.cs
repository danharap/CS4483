using UnityEngine;

/// <summary>
/// Portal in the lobby that enters the tutorial jail-cell room.
/// </summary>
[RequireComponent(typeof(Collider))]
public class TutorialLobbyPortal : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        // Disabled: tutorial entry is not portal-driven.
    }
}

