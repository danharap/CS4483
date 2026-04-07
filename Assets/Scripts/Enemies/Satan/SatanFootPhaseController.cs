using System.Collections;
using UnityEngine;

/// <summary>
/// Manages Satan's Phase 2 stomp cycle.
///
/// Rules:
///   • At most <see cref="maxActiveFeet"/> feet (default 2) can be on the ground at once.
///   • Each foot is an independent SatanFootController instance (created at runtime).
///   • Stomps are aimed near the player with light scatter so the attack is readable but not pixel-perfect.
///   • After each wave of stomps, the controller rests briefly before the next wave.
///
/// Attach to the Satan root and assign in the Inspector of SatanBossController.
/// </summary>
public class SatanFootPhaseController : MonoBehaviour
{
    // ── Stomp Pattern ─────────────────────────────────────────────────────

    [Header("Stomp Pattern")]
    [Tooltip("Maximum feet on the ground at once. 2 = alternating / overlapping left-right feel.")]
    [SerializeField] private int maxActiveFeet = 2;
    [Tooltip("Stomps launched per wave before the brief rest.")]
    [SerializeField] private int stompsPerWave = 4;
    [Tooltip("Delay between launching consecutive stomps within a wave.")]
    [SerializeField] private float delayBetweenStomps = 1.8f;
    [Tooltip("Rest time between waves (all active feet must retract first).")]
    [SerializeField] private float restBetweenWaves = 3.5f;

    // ── Foot Timing ───────────────────────────────────────────────────────

    [Header("Foot Timing")]
    [SerializeField] private float warningDuration  = 1.2f;
    [Tooltip("How long the foot stays on the ground — the player's damage window.")]
    [SerializeField] private float groundedDuration = 1.6f;
    [SerializeField] private float descentSpeed     = 28f;
    [SerializeField] private float retractSpeed     = 22f;
    [SerializeField] private float descentHeight    = 20f;

    // ── Foot Sprite ───────────────────────────────────────────────────────

    [Header("Foot Sprite")]
    [SerializeField] private Sprite footSprite;
    [SerializeField] private float  footScale = 3f;

    [Header("Alignment")]
    [Tooltip("Applied on top of the auto pivot-compensation. Tune in Play mode. " +
             "Positive Y moves the visual down in screen space (toward impact point).")]
    [SerializeField] private Vector3 footSpriteOffset = Vector3.zero;

    // ── Stomp Damage ──────────────────────────────────────────────────────

    [Header("Stomp Impact Damage")]
    [SerializeField] private float stompDamage = 30f;
    [SerializeField] private float stompRadius = 2.5f;

    // ── Targeting ─────────────────────────────────────────────────────────

    [Header("Targeting")]
    [Tooltip("Max random XZ scatter applied on top of the predicted landing spot.")]
    [SerializeField] private float aimScatter = 2.0f;
    [Tooltip("Z scatter multiplier (keeps stomps readable in top-down view).")]
    [SerializeField] private float aimScatterZMultiplier = 0.5f;

    [Header("Prediction")]
    [Tooltip("How many seconds ahead to predict the player's position (0 = aim at current pos).")]
    [SerializeField] private float predictionTime = 0.55f;
    [Tooltip("Blend 0-1 between current position (0) and fully predicted position (1). " +
             "Keeps the attack fair by not being perfectly accurate.")]
    [SerializeField] [Range(0f, 1f)] private float predictionStrength = 0.65f;
    [Tooltip("Max world-units the prediction is allowed to offset the target. Prevents wild leads.")]
    [SerializeField] private float predictionMaxLead = 5f;

    // ── Telegraph ─────────────────────────────────────────────────────────

    [Header("Telegraph Circle")]
    [SerializeField] private Color warningColor     = new Color(1f, 0.25f, 0.05f, 0.9f);
    [SerializeField] private float warningLineWidth = 0.12f;

    // ── Screen Shake ──────────────────────────────────────────────────────

    [Header("Screen Shake")]
    [SerializeField] private float shakeIntensity = 0.35f;
    [SerializeField] private float shakeDuration  = 0.28f;

    // ── Runtime ───────────────────────────────────────────────────────────

    private SatanBossController boss;
    private Transform           player;
    private bool                active;
    private int                 activeFeetCount;

    // Velocity tracking for prediction
    private Vector3 playerVelocity;
    private Vector3 lastPlayerPos;

    // ─────────────────────────────────────────────────────────────────────
    #region Public API

    /// <summary>Called by SatanBossController when Phase 2 begins.</summary>
    public void Begin(SatanBossController controller)
    {
        boss            = controller;
        player          = controller.Player;
        active          = true;
        activeFeetCount = 0;

        if (player != null)
            lastPlayerPos = player.position;

        Debug.Log("[SatanFootPhase] Phase 2 stomp cycle started.");
        StartCoroutine(StompCycle());
    }

    /// <summary>Called when the boss is truly defeated (or on cleanup).</summary>
    public void Stop()
    {
        active = false;
        StopAllCoroutines();
        Debug.Log("[SatanFootPhase] Stomp cycle stopped.");
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────
    #region Velocity Tracking

    private void Update()
    {
        if (!active || player == null) return;

        // Guard against Time.deltaTime == 0 (e.g. timeScale = 0 during upgrade pause).
        // Division by zero produces NaN which then propagates into targetPos and breaks
        // the stomp sequence with "transform.position is not valid" errors.
        if (Time.deltaTime <= 0f) return;

        Vector3 rawVel = (player.position - lastPlayerPos) / Time.deltaTime;
        rawVel.y = 0f;

        // Sanitize in case player was teleported across a large distance (produces huge vel).
        if (float.IsNaN(rawVel.x) || float.IsNaN(rawVel.z) ||
            float.IsInfinity(rawVel.x) || float.IsInfinity(rawVel.z))
        {
            lastPlayerPos = player.position;
            return;
        }

        playerVelocity = Vector3.Lerp(playerVelocity, rawVel, Time.deltaTime * 8f);

        // Safety net: reset if playerVelocity somehow still ends up non-finite.
        if (float.IsNaN(playerVelocity.x) || float.IsNaN(playerVelocity.z))
            playerVelocity = Vector3.zero;

        lastPlayerPos = player.position;
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────
    #region Stomp Cycle

    private IEnumerator StompCycle()
    {
        while (active && boss != null && boss.Phase2Active)
        {
            // ── Wave ─────────────────────────────────────────────────────
            for (int i = 0; i < stompsPerWave; i++)
            {
                if (!active || !boss.Phase2Active) yield break;

                // Block until there is room for another foot.
                while (activeFeetCount >= maxActiveFeet)
                    yield return null;

                if (!active || !boss.Phase2Active) yield break;

                Vector3 target = ChooseTarget();
                LaunchFoot(target);
                Debug.Log($"[SatanFootPhase] Launched foot {i + 1}/{stompsPerWave}  " +
                          $"target={target}  activeFeet={activeFeetCount}");

                yield return new WaitForSeconds(delayBetweenStomps);
            }

            // ── Rest (wait for feet to clear) ────────────────────────────
            while (activeFeetCount > 0)
                yield return null;

            yield return new WaitForSeconds(restBetweenWaves);
        }
    }

    private Vector3 ChooseTarget()
    {
        if (player == null) return Vector3.zero;

        Vector3 currentPos = new Vector3(player.position.x, 0f, player.position.z);

        // If velocity is somehow non-finite, fall back to current position (no prediction).
        Vector3 safeVel = (float.IsNaN(playerVelocity.x) || float.IsNaN(playerVelocity.z) ||
                           float.IsInfinity(playerVelocity.x) || float.IsInfinity(playerVelocity.z))
                          ? Vector3.zero : playerVelocity;

        Vector3 lead = safeVel * predictionTime;
        lead.y = 0f;
        if (lead.sqrMagnitude > predictionMaxLead * predictionMaxLead)
            lead = lead.normalized * predictionMaxLead;

        Vector3 baseTarget = currentPos + lead * predictionStrength;

        Vector3 result = new Vector3(
            baseTarget.x + Random.Range(-aimScatter, aimScatter),
            0f,
            baseTarget.z + Random.Range(-aimScatter * aimScatterZMultiplier,
                                         aimScatter * aimScatterZMultiplier));

        // Final sanity check — should never be needed after the guards above.
        if (float.IsNaN(result.x) || float.IsNaN(result.z))
            return currentPos;

        return result;
    }

    private void LaunchFoot(Vector3 targetPos)
    {
        activeFeetCount++;

        GameObject footGO = new GameObject("SatanFoot");
        SatanFootController fc = footGO.AddComponent<SatanFootController>();
        fc.OnDone += OnFootRetracted;

        fc.Setup(
            targetPos:        targetPos,
            boss:             boss,
            player:           player,
            footSprite:       footSprite,
            footScale:        footScale,
            descentHeight:    descentHeight,
            descentSpeed:     descentSpeed,
            retractSpeed:     retractSpeed,
            warningDuration:  warningDuration,
            groundedDuration: groundedDuration,
            stompDamage:      stompDamage,
            stompRadius:      stompRadius,
            warningColor:     warningColor,
            warningLineWidth: warningLineWidth,
            spriteOffset:     footSpriteOffset,
            shakeIntensity:   shakeIntensity,
            shakeDuration:    shakeDuration);
    }

    private void OnFootRetracted(SatanFootController foot)
    {
        activeFeetCount = Mathf.Max(0, activeFeetCount - 1);
        Debug.Log($"[SatanFootPhase] Foot retracted. Active feet remaining: {activeFeetCount}");
    }

    #endregion
}
