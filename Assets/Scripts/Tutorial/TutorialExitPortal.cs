using UnityEngine;

/// <summary>
/// Portal at the end of the tutorial room that sends player into Arena 1.
/// </summary>
[RequireComponent(typeof(Collider))]
public class TutorialExitPortal : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        TutorialRoomManager.Instance?.ExitTutorialToLobby();
    }
}

