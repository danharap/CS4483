using System.Collections;
using UnityEngine;

/// <summary>
/// Manages Satan's presence in Stage 2 from arrival to boss combat.
///
/// Flow:
///   1. When Arena 2 becomes active this component's Start() runs.
///   2. Satan is instantiated at the throne position and frozen on awakening frame 0
///      (ThroneWatch state) — visible to the player during waves 6–9.
///   3. At wave <satanWaveTrigger> (default 9 = displayed as wave 10), the awakening
///      animation continues, then Satan arcs down into the arena and enters combat.
///   4. EnemySpawner.SpawnBoss() detects Satan is already in the scene and skips
///      spawning a duplicate — so the BossWave flow still waits for his defeat normally.
///
/// Attach this component to any persistent object inside the Arena 2 root so it only
/// activates once Arena 2 is enabled.  Wire satanPrefab, thronePosition, and landingPosition
/// in the Inspector.  The WaveManager reference is auto-resolved via FindFirstObjectByType
/// if left unassigned.
/// </summary>
public class SatanArenaIntroController : MonoBehaviour
{
    // ── Inspector Fields ──────────────────────────────────────────────────

    [Header("Satan Setup")]
    [Tooltip("The Satan boss prefab to instantiate. Must have SatanBossController.")]
    [SerializeField] private GameObject satanPrefab;

    [Header("Positions")]
    [Tooltip("World position where Satan sits on the throne (matches throne geometry). " +
             "Logged to console by ProBuilderLevelBuilder after building Arena 2.")]
    [SerializeField] private Vector3 thronePosition  = new Vector3(0f, 13.9f, 45.3f);
    [Tooltip("Where Satan lands on the arena floor after the jump-down intro.")]
    [SerializeField] private Vector3 landingPosition = new Vector3(0f, 1.1f, 12f);

    [Header("Wave Trigger")]
    [Tooltip("0-based wave index that triggers the boss intro. Default 9 = displayed as Wave 10.")]
    [SerializeField] private int satanWaveTrigger = 9;
    [Tooltip("Seconds to wait after wave start before triggering the awakening.")]
    [SerializeField] private float introDelay = 0.8f;

    [Header("References")]
    [Tooltip("Leave null to auto-find at runtime.")]
    [SerializeField] private WaveManager waveManager;

    // ── Runtime ───────────────────────────────────────────────────────────

    private SatanBossController satanInstance;
    private bool introTriggered;

    // ─────────────────────────────────────────────────────────────────────

    private void Start()
    {
        // Resolve WaveManager
        if (waveManager == null)
            waveManager = FindFirstObjectByType<WaveManager>();

        if (waveManager == null)
        {
            Debug.LogError("[SatanArenaIntro] WaveManager not found — Satan intro will not trigger!");
            return;
        }

        waveManager.OnWaveStart += OnWaveStart;

        // Spawn Satan on the throne immediately when Arena 2 becomes active
        SpawnSatanOnThrone();
    }

    private void OnDestroy()
    {
        if (waveManager != null)
            waveManager.OnWaveStart -= OnWaveStart;
    }

    // ─────────────────────────────────────────────────────────────────────
    #region Throne Spawn

    private void SpawnSatanOnThrone()
    {
        // Hide the static "seated" sprite baked into the throne geometry —
        // the live Satan instance (which supports awakening + combat) replaces it.
        HideThroneStaticVisual();

        if (satanPrefab == null)
        {
            Debug.LogError("[SatanArenaIntro] satanPrefab is not assigned! " +
                           "Assign the Enemy_Satan prefab in the SatanArenaIntroController inspector.");
            return;
        }

        // Instantiate at the throne position; Awake() runs immediately.
        GameObject go = Instantiate(satanPrefab, thronePosition, Quaternion.identity);
        go.name = "Satan_Boss";

        satanInstance = go.GetComponent<SatanBossController>();
        if (satanInstance == null)
        {
            Debug.LogError("[SatanArenaIntro] Satan prefab is missing SatanBossController!");
            return;
        }

        // EnableThroneMode must be called BEFORE Start() runs on the new object.
        // In Unity, Instantiate triggers Awake() synchronously, then this line runs,
        // then Start() fires next frame — so the flag is set in time.
        satanInstance.EnableThroneMode(thronePosition, landingPosition);

        Debug.Log($"[SatanArenaIntro] Satan placed on throne at {thronePosition}. " +
                  $"Watching until wave {satanWaveTrigger + 1}.");
    }

    /// <summary>
    /// Finds and hides the static "Throne_Satan_Visual" sprite baked into the level geometry
    /// by ProBuilderLevelBuilder. The live Satan prefab takes over from here.
    /// </summary>
    private void HideThroneStaticVisual()
    {
        // Walk up to the arena root then search downward for the throne area child.
        Transform arenaRoot = transform.root;
        Transform throneArea = arenaRoot.Find("Satan_Throne_Area");
        if (throneArea != null)
        {
            Transform watcher = throneArea.Find("Throne_Satan_Visual");
            if (watcher != null)
            {
                watcher.gameObject.SetActive(false);
                Debug.Log("[SatanArenaIntro] Throne static visual hidden — live Satan now active.");
            }
        }
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────
    #region Wave 10 Trigger

    private void OnWaveStart(int waveIndex)
    {
        if (introTriggered) return;
        if (waveIndex != satanWaveTrigger) return;

        introTriggered = true;
        Debug.Log($"[SatanArenaIntro] Wave {waveIndex + 1} started — triggering Satan boss intro!");
        StartCoroutine(TriggerIntroAfterDelay());
    }

    private IEnumerator TriggerIntroAfterDelay()
    {
        yield return new WaitForSeconds(introDelay);

        if (satanInstance == null)
        {
            Debug.LogError("[SatanArenaIntro] Satan instance is null when trying to trigger intro!");
            yield break;
        }

        satanInstance.BeginAwakenFromThrone();
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────
    #region Editor Gizmos

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // Throne position
        Gizmos.color = new Color(1f, 0.6f, 0f, 0.4f);
        Gizmos.DrawSphere(thronePosition, 0.8f);
        Gizmos.color = new Color(1f, 0.6f, 0f, 1f);
        Gizmos.DrawWireSphere(thronePosition, 0.8f);
        UnityEditor.Handles.Label(thronePosition + Vector3.up * 1.5f, "Satan Throne Watch Pos");

        // Landing position
        Gizmos.color = new Color(1f, 0.1f, 0.1f, 0.4f);
        Gizmos.DrawSphere(landingPosition, 0.8f);
        Gizmos.color = new Color(1f, 0.1f, 0.1f, 1f);
        Gizmos.DrawWireSphere(landingPosition, 0.8f);
        UnityEditor.Handles.Label(landingPosition + Vector3.up * 1.5f, "Satan Landing Pos");

        // Jump arc preview (25 steps)
        Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.7f);
        Vector3 prev = thronePosition;
        const int steps = 25;
        for (int i = 1; i <= steps; i++)
        {
            float u     = (float)i / steps;
            float arcY  = 9f * 4f * u * (1f - u); // matches default jumpArcHeight
            Vector3 cur = Vector3.Lerp(thronePosition, landingPosition, u);
            cur.y += arcY;
            Gizmos.DrawLine(prev, cur);
            prev = cur;
        }
    }
#endif

    #endregion
}
