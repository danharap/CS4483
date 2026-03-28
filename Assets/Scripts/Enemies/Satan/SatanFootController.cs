using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Per-foot behavior for Satan's Phase 2 stomp attack.
///
/// Lifecycle per foot:
///   1. Warning  – telegraph circle pulses on the ground. Foot visual is HIDDEN.
///   2. Descend  – foot drops from above (fully off-screen) into view, base-first.
///   3. Grounded – foot lands with BASE touching the floor. Hurtbox enabled.
///   4. Retract  – foot rises back off-screen. Hurtbox disabled.
///   5. Done     – OnDone callback, GameObject destroyed.
///
/// Pivot / alignment:
///   The sprite pivot is at center (0.5, 0.5).  With Billboard facing the camera,
///   moving the visual child +Y in LOCAL space shifts the sprite CENTER upward in camera view,
///   placing the BOTTOM of the sprite at the transform's world position (= warning circle center).
///   footPivotLift = sprite.bounds.size.y * footScale * 0.5f  (positive, shifts center UP).
///   footSpriteOffset is an additional manual tweak exposed on SatanFootPhaseController.
/// </summary>
public class SatanFootController : MonoBehaviour
{
    public enum FootState { Idle, Warning, Descending, Grounded, Retracting, Done }

    public FootState State { get; private set; } = FootState.Idle;

    public System.Action<SatanFootController> OnDone;

    // ── Config (set by Setup) ─────────────────────────────────────────────

    private Vector3  targetPos;
    private SatanBossController boss;
    private Transform player;

    private Sprite  footSprite;
    private float   footScale;
    private float   descentHeight;
    private float   descentSpeed;
    private float   retractSpeed;
    private float   warningDuration;
    private float   groundedDuration;
    private float   stompDamage;
    private float   stompRadius;
    private Color   warningColor;
    private float   warningLineWidth;
    private Vector3 spriteOffset;
    private float   shakeIntensity;
    private float   shakeDuration;

    // ── Components ────────────────────────────────────────────────────────

    private GameObject      footVisualGO;
    private SpriteRenderer  footSR;
    private LineRenderer    warnCircle;
    private CapsuleCollider hurtbox;

    private readonly HashSet<Projectile> hitProjectiles = new HashSet<Projectile>();
    private bool flashing;

    // World Y when the foot is considered "grounded".
    // The visual base (not center) sits at the arena floor at this height.
    private const float GroundedY = 0.12f;

    // ─────────────────────────────────────────────────────────────────────
    #region Setup

    public void Setup(
        Vector3 targetPos,
        SatanBossController boss,
        Transform player,
        Sprite  footSprite,
        float   footScale,
        float   descentHeight,
        float   descentSpeed,
        float   retractSpeed,
        float   warningDuration,
        float   groundedDuration,
        float   stompDamage,
        float   stompRadius,
        Color   warningColor,
        float   warningLineWidth,
        Vector3 spriteOffset,
        float   shakeIntensity,
        float   shakeDuration)
    {
        this.targetPos        = targetPos;
        this.boss             = boss;
        this.player           = player;
        this.footSprite       = footSprite;
        this.footScale        = footScale;
        this.descentHeight    = descentHeight;
        this.descentSpeed     = descentSpeed;
        this.retractSpeed     = retractSpeed;
        this.warningDuration  = warningDuration;
        this.groundedDuration = groundedDuration;
        this.stompDamage      = stompDamage;
        this.stompRadius      = stompRadius;
        this.warningColor     = warningColor;
        this.warningLineWidth = warningLineWidth;
        this.spriteOffset     = spriteOffset;
        this.shakeIntensity   = shakeIntensity;
        this.shakeDuration    = shakeDuration;

        // Position transform high above the landing point.
        // The foot visual will be hidden until descent begins.
        transform.position = new Vector3(targetPos.x, descentHeight, targetPos.z);

        BuildWarningCircle();
        BuildFootVisual();
        BuildHurtbox();

        StartCoroutine(StompSequence());
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────
    #region Build Helpers

    private void BuildWarningCircle()
    {
        GameObject go = new GameObject("FootWarning");
        go.transform.SetParent(null);
        warnCircle = go.AddComponent<LineRenderer>();
        warnCircle.useWorldSpace = true;
        warnCircle.loop          = true;
        warnCircle.positionCount = 48;
        warnCircle.startWidth    = warningLineWidth;
        warnCircle.endWidth      = warningLineWidth;
        warnCircle.material      = new Material(Shader.Find("Sprites/Default"));
        warnCircle.startColor    = warningColor;
        warnCircle.endColor      = warningColor;
        warnCircle.sortingOrder  = 30;
        warnCircle.enabled       = false;
    }

    private void BuildFootVisual()
    {
        footVisualGO = new GameObject("FootVisual");
        footVisualGO.transform.SetParent(transform, false);
        footVisualGO.transform.localScale = new Vector3(footScale, footScale, 1f);

        // Pivot compensation:
        // Sprite pivot is at (0.5, 0.5) = center. Billboard makes the sprite face the camera.
        // Moving the child +Y in local space lifts the sprite CENTER upward in camera view,
        // so the BOTTOM edge of the sprite aligns with the transform's world position (floor hit point).
        // This ensures the base of the hoof visually contacts the floor at the warning circle.
        float pivotLift = (footSprite != null)
            ? footSprite.bounds.size.y * footScale * 0.5f   // positive = lift center up
            : 0f;

        footVisualGO.transform.localPosition = new Vector3(
            spriteOffset.x,
            pivotLift + spriteOffset.y,
            spriteOffset.z);

        footVisualGO.AddComponent<Billboard>();

        footSR = footVisualGO.AddComponent<SpriteRenderer>();
        footSR.sprite       = footSprite;
        footSR.sortingOrder = 20;

        // Fallback disc visual when no sprite is assigned.
        if (footSprite == null)
        {
            GameObject disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disc.name = "FootFallback";
            disc.transform.SetParent(footVisualGO.transform, false);
            disc.transform.localPosition = Vector3.zero;
            disc.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            disc.transform.localScale    = new Vector3(1.4f, 0.15f, 1.4f);
            Destroy(disc.GetComponent<Collider>());
            MeshRenderer mr = disc.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                Material mat = new Material(Shader.Find("Standard"));
                mat.color = new Color(0.65f, 0.08f, 0.08f);
                mr.material = mat;
            }
        }

        // ── KEY FIX: Hide the foot during the warning phase. ─────────────
        // It starts at descentHeight which is still visible through the camera.
        // The visual is revealed only when the actual stomp descent begins.
        footVisualGO.SetActive(false);
    }

    private void BuildHurtbox()
    {
        hurtbox = gameObject.AddComponent<CapsuleCollider>();
        hurtbox.isTrigger = true;
        hurtbox.radius    = stompRadius * 0.8f;
        hurtbox.height    = 2f;
        hurtbox.center    = Vector3.zero;
        hurtbox.enabled   = false;

        int bl = LayerMask.NameToLayer("Boss");
        if (bl >= 0) gameObject.layer = bl;
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────
    #region Stomp Sequence

    private IEnumerator StompSequence()
    {
        // ── 1. Warning ────────────────────────────────────────────────────
        State = FootState.Warning;
        warnCircle.enabled = true;
        // footVisualGO is HIDDEN here (set inactive in BuildFootVisual)

        float t = 0f;
        while (t < warningDuration)
        {
            t += Time.deltaTime;
            float pulse = 0.5f + 0.5f * Mathf.Sin(t * Mathf.PI * 6f);
            UpdateWarningCircle(pulse);
            yield return null;
        }
        warnCircle.enabled = false;

        // ── 2. Descent ───────────────────────────────────────────────────
        State = FootState.Descending;
        // Snap to descent start position (in case Satan moved during warning)
        transform.position = new Vector3(targetPos.x, descentHeight, targetPos.z);

        // Reveal the foot NOW — it drops into view from above.
        footVisualGO.SetActive(true);

        while (transform.position.y > GroundedY)
        {
            transform.position -= new Vector3(0f, descentSpeed * Time.deltaTime, 0f);
            yield return null;
        }
        transform.position = new Vector3(targetPos.x, GroundedY, targetPos.z);

        // ── 3. Grounded (damage window) ───────────────────────────────────
        State = FootState.Grounded;
        hurtbox.enabled = true;
        hitProjectiles.Clear();

        DamagePlayerIfInRange();
        StartCoroutine(ScreenShake());

        yield return new WaitForSeconds(groundedDuration);

        hurtbox.enabled = false;

        // ── 4. Retract ────────────────────────────────────────────────────
        State = FootState.Retracting;
        while (transform.position.y < descentHeight)
        {
            transform.position += new Vector3(0f, retractSpeed * Time.deltaTime, 0f);
            yield return null;
        }

        // ── 5. Done ───────────────────────────────────────────────────────
        State = FootState.Done;
        Cleanup();
        OnDone?.Invoke(this);
        Destroy(gameObject, 0.05f);
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────
    #region Damage

    private void DamagePlayerIfInRange()
    {
        if (player == null) return;
        float dist = Vector3.Distance(
            new Vector3(player.position.x, 0f, player.position.z),
            new Vector3(targetPos.x, 0f, targetPos.z));
        if (dist <= stompRadius)
            player.GetComponent<PlayerHealth>()?.TakeDamage(stompDamage);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (State != FootState.Grounded) return;

        Projectile proj = other.GetComponent<Projectile>();
        if (proj == null || hitProjectiles.Contains(proj)) return;

        hitProjectiles.Add(proj);
        float dmg = proj.DamageAmount;
        proj.NotifyHit();
        boss?.TakeDamagePhase2(dmg);
        FlashRed();
    }

    private void FlashRed()
    {
        if (!flashing && footSR != null)
            StartCoroutine(HitFlashCoroutine());
    }

    private IEnumerator HitFlashCoroutine()
    {
        flashing = true;
        Color original = footSR.color;
        footSR.color = Color.red;
        yield return new WaitForSeconds(0.12f);
        if (footSR != null) footSR.color = original;
        flashing = false;
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────
    #region Visual Helpers

    private void UpdateWarningCircle(float alphaMult)
    {
        if (warnCircle == null) return;
        Color c = warningColor;
        c.a = Mathf.Clamp01(warningColor.a * alphaMult);
        warnCircle.startColor = c;
        warnCircle.endColor   = c;

        int count = warnCircle.positionCount;
        for (int i = 0; i < count; i++)
        {
            float a = (i / (float)count) * Mathf.PI * 2f;
            warnCircle.SetPosition(i, new Vector3(
                targetPos.x + Mathf.Cos(a) * stompRadius,
                0.08f,
                targetPos.z + Mathf.Sin(a) * stompRadius));
        }
    }

    private IEnumerator ScreenShake()
    {
        Camera cam = Camera.main;
        if (cam == null) yield break;
        Vector3 orig    = cam.transform.position;
        float   elapsed = 0f;
        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;
            float frac = 1f - elapsed / shakeDuration;
            cam.transform.position = orig + new Vector3(
                Random.Range(-1f, 1f) * shakeIntensity * frac,
                0f,
                Random.Range(-1f, 1f) * shakeIntensity * frac);
            yield return null;
        }
        cam.transform.position = orig;
    }

    private void Cleanup()
    {
        if (warnCircle != null) Destroy(warnCircle.gameObject);
    }

    #endregion
}
