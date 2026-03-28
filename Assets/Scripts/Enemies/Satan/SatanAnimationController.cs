using System.Collections;
using UnityEngine;

/// <summary>
/// Handles all sprite animation for Satan:
///   - Freezing on the first awakening frame (statue)
///   - Playing the rest of the awakening sequence on demand
///   - Swapping animation sets per attack type
///   - Death animation + red flash feedback
///
/// Uses manual sprite-swap driven by coroutines so attack timing in
/// SatanAttacks is never desynced from visuals.
/// Attach to the Satan prefab root. Wires itself to a child SpriteRenderer.
/// </summary>
public class SatanAnimationController : MonoBehaviour
{
    // ── Sprite Sets (assign in Inspector) ────────────────────────────────

    [Header("Awakening")]
    [Tooltip("All frames of the Awakening animation, in order. Frame 0 = statue.")]
    [SerializeField] private Sprite[] awakeningFrames;
    [SerializeField] private float    awakeningFPS = 8f;

    [Header("Idle / Combat")]
    [SerializeField] private Sprite[] idleFrames;
    [SerializeField] private float    idleFPS = 6f;

    [Header("Direct Attack")]
    [SerializeField] private Sprite[] directAttackFrames;
    [SerializeField] private float    directAttackFPS = 10f;

    [Header("Down Beam Attack")]
    [SerializeField] private Sprite[] downBeamFrames;
    [SerializeField] private float    downBeamFPS = 10f;

    [Header("Fan Attack")]
    [SerializeField] private Sprite[] fanAttackFrames;
    [SerializeField] private float    fanAttackFPS = 10f;

    [Header("Dual Hand Beam Attack")]
    [SerializeField] private Sprite[] dualHandBeamFrames;
    [SerializeField] private float    dualHandBeamFPS = 10f;

    [Header("Death")]
    [SerializeField] private Sprite[] deathFrames;
    [SerializeField] private float    deathFPS = 7f;

    [Header("Visual Size")]
    [Tooltip("Uniform scale applied to Satan's sprite. 2.4 = 3/5 of the original 4x size.")]
    [SerializeField] private float spriteScale = 2.4f;

    [Header("Flash")]
    [SerializeField] private float hitFlashDuration = 0.12f;

    // ── Runtime ───────────────────────────────────────────────────────────

    private SpriteRenderer sr;
    private Coroutine loopCoroutine;
    private bool flashing;

    private void Awake()
    {
        // Expect a SpriteRenderer on this GameObject or as a direct child
        sr = GetComponent<SpriteRenderer>();
        if (sr == null)
            sr = GetComponentInChildren<SpriteRenderer>();
        if (sr == null)
        {
            GameObject child = new GameObject("Satan_Sprite");
            child.transform.SetParent(transform, false);
            child.transform.localPosition = Vector3.zero;
            child.AddComponent<Billboard>();
            sr = child.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 25;
        }

        // Apply uniform visual scale so Satan appears large on screen
        sr.transform.localScale = new Vector3(spriteScale, spriteScale, 1f);
    }

    // ─────────────────────────────────────────────────────────────────────
    #region Public Interface

    /// <summary>Show the very first awakening frame (statue state). Blocks all loops.</summary>
    public void SetStatueFrame()
    {
        StopLoop();
        if (awakeningFrames != null && awakeningFrames.Length > 0)
            sr.sprite = awakeningFrames[0];
    }

    /// <summary>
    /// Play the full awakening animation from frame 1 onward (skips statue frame 0).
    /// Awaitable so SatanBossController can yield on it.
    /// </summary>
    public IEnumerator PlayAwakening()
    {
        StopLoop();
        if (awakeningFrames == null || awakeningFrames.Length <= 1) yield break;

        float step = 1f / Mathf.Max(1f, awakeningFPS);
        for (int i = 1; i < awakeningFrames.Length; i++) // start from frame 1
        {
            sr.sprite = awakeningFrames[i];
            yield return new WaitForSeconds(step);
        }
    }

    /// <summary>Start looping idle combat animation.</summary>
    public void PlayIdleLoop()
    {
        StopLoop();
        loopCoroutine = StartCoroutine(LoopFrames(idleFrames, idleFPS));
    }

    /// <summary>Play a one-shot attack animation (non-looping). Returns when finished.</summary>
    public IEnumerator PlayAttackAnim(AttackAnimType type)
    {
        StopLoop();

        Sprite[] frames;
        float    fps;
        switch (type)
        {
            case AttackAnimType.Direct:       frames = directAttackFrames;   fps = directAttackFPS;   break;
            case AttackAnimType.DownBeam:     frames = downBeamFrames;       fps = downBeamFPS;       break;
            case AttackAnimType.Fan:          frames = fanAttackFrames;      fps = fanAttackFPS;      break;
            case AttackAnimType.DualHandBeam: frames = dualHandBeamFrames;   fps = dualHandBeamFPS;   break;
            default:                          yield break;
        }

        if (frames == null || frames.Length == 0) yield break;

        float step = 1f / Mathf.Max(1f, fps);
        foreach (Sprite s in frames)
        {
            sr.sprite = s;
            yield return new WaitForSeconds(step);
        }
    }

    /// <summary>Play death animation once, then yield.</summary>
    public IEnumerator PlayDeath()
    {
        StopLoop();
        if (deathFrames == null || deathFrames.Length == 0) yield break;

        float step = 1f / Mathf.Max(1f, deathFPS);
        foreach (Sprite s in deathFrames)
        {
            sr.sprite = s;
            yield return new WaitForSeconds(step);
        }
    }

    public void HideAll()
    {
        StopLoop();
        sr.enabled = false;
    }

    /// <summary>Brief red tint hit-confirm. Non-blocking.</summary>
    public void FlashRed()
    {
        if (!flashing)
            StartCoroutine(FlashCoroutine());
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────
    #region Helpers

    private IEnumerator LoopFrames(Sprite[] frames, float fps)
    {
        if (frames == null || frames.Length == 0) yield break;
        float step = 1f / Mathf.Max(1f, fps);
        int   i    = 0;
        while (true)
        {
            sr.sprite = frames[i];
            i = (i + 1) % frames.Length;
            yield return new WaitForSeconds(step);
        }
    }

    private void StopLoop()
    {
        if (loopCoroutine != null)
        {
            StopCoroutine(loopCoroutine);
            loopCoroutine = null;
        }
    }

    private IEnumerator FlashCoroutine()
    {
        flashing = true;
        Color original = sr.color;
        sr.color = Color.red;
        yield return new WaitForSeconds(hitFlashDuration);
        sr.color = original;
        flashing = false;
    }

    #endregion
}

/// <summary>Attack animation variant identifiers.</summary>
public enum AttackAnimType
{
    Direct,
    DownBeam,
    Fan,
    DualHandBeam
}
