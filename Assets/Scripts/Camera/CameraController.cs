using UnityEngine;

/// <summary>
/// Smooth top-down camera follow. Use this instead of Cinemachine for simplicity,
/// or replace with a Cinemachine FreeLook if preferred.
/// Attach to the Camera GameObject. Set target to the Player transform.
/// </summary>
public class CameraController : MonoBehaviour
{
    // ── Tunables ──────────────────────────────────────────────────────────
    [Header("Follow Target")]
    [SerializeField] private Transform target;

    [Header("Camera Offset (world-space)")]
    [SerializeField] private Vector3 offset     = new Vector3(0f, 16f, -9f);

    [Header("Smoothing")]
    [SerializeField] private float followSpeed  = 6f;

    // ── State ─────────────────────────────────────────────────────────────
    private Vector3 velocity;

    void LateUpdate()
    {
        if (target == null)
        {
            // Try to find the player if not set
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) target = p.transform;
            return;
        }

        Vector3 desired = target.position + offset;
        transform.position = Vector3.SmoothDamp(
            transform.position, desired, ref velocity, 1f / followSpeed);
    }
}
