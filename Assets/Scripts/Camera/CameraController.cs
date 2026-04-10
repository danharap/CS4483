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
    [SerializeField] private Vector3 offset     = new Vector3(0f, 20f, -10f);

    [Header("Zoom")]
    [SerializeField] private float defaultZoom = 1.0f;
    [SerializeField] private float zoomLerpSpeed = 3.5f;

    [Header("Vertical Follow")]
    [SerializeField] private bool followTargetY = false; // keep camera height stable so boss jump doesn't lift the view

    [Header("Smoothing")]
    [SerializeField] private float followSpeed  = 6f;

    // ── State ─────────────────────────────────────────────────────────────
    private Vector3 velocity;
    private Vector3 baseOffset;
    private float zoomCurrent;
    private float zoomTarget;

    void Awake()
    {
        baseOffset = offset;
        zoomCurrent = defaultZoom;
        zoomTarget = defaultZoom;
    }

    void LateUpdate()
    {
        if (target == null)
        {
            // Try to find the player if not set
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null)
            {
                target = p.transform;
            }
            else
            {
                // No player found; keep camera where it is but don't spam logs.
                return;
            }
        }

        // Smooth zoom
        zoomCurrent = Mathf.Lerp(zoomCurrent, zoomTarget, 1f - Mathf.Exp(-zoomLerpSpeed * Time.deltaTime));
        offset = baseOffset * zoomCurrent;

        Vector3 targetPos = target.position;
        if (!followTargetY) targetPos.y = 0f;
        Vector3 desired = targetPos + offset;
        if (!IsFinite(desired)) return;
        if (followSpeed <= 0.01f) followSpeed = 0.01f;
        transform.position = Vector3.SmoothDamp(
            transform.position, desired, ref velocity, 1f / followSpeed);
    }

    static bool IsFinite(Vector3 v)
    {
        return float.IsFinite(v.x) && float.IsFinite(v.y) && float.IsFinite(v.z);
    }

    public void SetZoom(float zoom)
    {
        zoomTarget = Mathf.Clamp(zoom, 0.75f, 1.8f);
    }

    public void ResetZoom()
    {
        zoomTarget = defaultZoom;
    }

    // ── Cinematic pan API ─────────────────────────────────────────────────

    private Coroutine panCoroutine;

    /// <summary>
    /// Smoothly pan the camera to look at <paramref name="worldTarget"/>,
    /// hold for <paramref name="holdTime"/> seconds, then return to the player.
    /// </summary>
    public void PanToAndReturn(Vector3 worldTarget, float travelTime = 1.2f, float holdTime = 2.5f)
    {
        if (panCoroutine != null) StopCoroutine(panCoroutine);
        panCoroutine = StartCoroutine(PanRoutine(worldTarget, travelTime, holdTime));
    }

    private System.Collections.IEnumerator PanRoutine(Vector3 worldTarget, float travelTime, float holdTime)
    {
        // Override the follow target temporarily.
        Transform savedTarget = target;
        target = null;                    // stop normal follow

        Vector3 startPos  = transform.position;
        Vector3 desiredTo = worldTarget + offset * zoomCurrent;

        // Pan to
        float t = 0f;
        while (t < travelTime)
        {
            t += Time.deltaTime;
            float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / travelTime));
            transform.position = Vector3.Lerp(startPos, desiredTo, u);
            yield return null;
        }
        transform.position = desiredTo;

        // Hold
        yield return new WaitForSeconds(holdTime);

        // Return to player
        Vector3 returnStart = transform.position;
        target = savedTarget;

        t = 0f;
        while (t < travelTime)
        {
            t += Time.deltaTime;
            float u = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / travelTime));
            if (target != null)
            {
                Vector3 playerTarget = target.position;
                if (!followTargetY) playerTarget.y = 0f;
                Vector3 desired = playerTarget + offset * zoomCurrent;
                transform.position = Vector3.Lerp(returnStart, desired, u);
            }
            yield return null;
        }

        panCoroutine = null;
    }
}
