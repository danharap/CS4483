using UnityEngine;

/// <summary>
/// Trigger zone: when the player enters, notifies ArenaPortalManager to transition to Arena 2.
/// Used after wave 5 is cleared.
/// </summary>
[RequireComponent(typeof(Collider))]
public class Portal : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        ArenaPortalManager manager = ArenaPortalManager.Instance;
        if (manager != null)
        {
            manager.TransitionToArena2();
        }
        else
        {
            Debug.LogWarning("[Portal] ArenaPortalManager.Instance is null.");
        }
    }
}
