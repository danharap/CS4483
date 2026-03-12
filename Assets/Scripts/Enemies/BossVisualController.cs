using System.Collections;
using UnityEngine;

/// <summary>
/// Heavy-style visuals for the boss:
/// - Body uses walking frames (up/down vs right; left is flipped right)
/// - Head animates while moving; switches to head attack frames while attacking
/// - Attack frames can be set explicitly by the BossEnemy slam routine
/// </summary>
public class BossVisualController : MonoBehaviour
{
    [Header("References")]
    public SpriteRenderer bodyRenderer;
    public SpriteRenderer headRenderer;

    [Header("Body Walking Frames")]
    public Sprite[] bodyWalkUpDown;
    public Sprite[] bodyWalkRight;
    public float bodyWalkFrameRate = 6f;

    [Header("Body Attack Frames (Jump Slam)")]
    public Sprite[] bodyAttackFrames; // expected 3 frames: charge, descent, impact

    [Header("Head Frames")]
    public Sprite[] headWalkFrames;
    public Sprite[] headAttackFrames;
    public float headFrameRate = 7f;

    [Header("Head Offset (local)")]
    public Vector3 headOffset = new Vector3(0f, 0.58f, 0f);

    [Header("Direction Smoothing")]
    [Tooltip("Minimum movement (squared) required to update facing direction.")]
    public float minMoveSqr = 0.0025f;
    [Tooltip("If true, vertical wins ties (so primarily up/down uses walking sprite).")]
    public bool verticalWinsTies = true;

    [Header("Death")]
    public Sprite deathSprite;

    private Vector3 lastPos;
    private int dirIndex; // 0=Up/Down, 1=Right, 2=Left
    private float bodyTimer, headTimer;
    private int bodyIndex, headIndex;
    private bool isAttacking;
    private int forcedAttackFrame = -1;
    private Sprite[] activeBodyWalk;

    void Awake()
    {
        lastPos = transform.position;
        if (bodyRenderer == null) bodyRenderer = GetComponentInChildren<SpriteRenderer>();
        if (headRenderer == null && bodyRenderer != null)
        {
            Transform headT = bodyRenderer.transform.Find("Head");
            if (headT != null) headRenderer = headT.GetComponent<SpriteRenderer>();
        }
        activeBodyWalk = (bodyWalkUpDown != null && bodyWalkUpDown.Length > 0) ? bodyWalkUpDown : bodyWalkRight;
    }

    void LateUpdate()
    {
        if (bodyRenderer == null || headRenderer == null) return;

        // keep head aligned
        headRenderer.transform.localPosition = headOffset;

        Vector3 pos = transform.position;
        Vector3 delta = pos - lastPos;
        delta.y = 0f;

        // Determine direction only while moving and not attacking
        if (!isAttacking)
        {
            if (delta.sqrMagnitude >= minMoveSqr)
            {
                float ax = Mathf.Abs(delta.x);
                float az = Mathf.Abs(delta.z);

                // Primarily vertical => walking (up/down). Primarily horizontal => walking right (flip for left).
                if (verticalWinsTies ? (az >= ax) : (az > ax))
                    dirIndex = 0;
                else
                    dirIndex = delta.x > 0f ? 1 : 2;
            }
        }

        // Body render
        if (isAttacking && bodyAttackFrames != null && bodyAttackFrames.Length > 0)
        {
            int idx = Mathf.Clamp(forcedAttackFrame, 0, bodyAttackFrames.Length - 1);
            bodyRenderer.sprite = bodyAttackFrames[idx];
        }
        else
        {
            // Choose walking frames based on direction
            activeBodyWalk = (dirIndex == 0)
                ? (bodyWalkUpDown != null && bodyWalkUpDown.Length > 0 ? bodyWalkUpDown : bodyWalkRight)
                : (bodyWalkRight != null && bodyWalkRight.Length > 0 ? bodyWalkRight : bodyWalkUpDown);

            if (activeBodyWalk != null && activeBodyWalk.Length > 0)
            {
                bodyTimer += Time.deltaTime;
                if (bodyTimer >= 1f / bodyWalkFrameRate)
                {
                    bodyTimer = 0f;
                    bodyIndex = (bodyIndex + 1) % activeBodyWalk.Length;
                    bodyRenderer.sprite = activeBodyWalk[bodyIndex];
                }
            }
        }

        // Flip body for left while walking (and also for attack if currently facing left)
        bodyRenderer.flipX = (dirIndex == 2);

        // Head render: while moving = headWalkFrames, while attacking = headAttackFrames
        Sprite[] headFrames = isAttacking
            ? headAttackFrames
            : headWalkFrames;

        if (headFrames != null && headFrames.Length > 0)
        {
            headTimer += Time.deltaTime;
            if (headTimer >= 1f / headFrameRate)
            {
                headTimer = 0f;
                headIndex = (headIndex + 1) % headFrames.Length;
                headRenderer.sprite = headFrames[headIndex];
            }
        }

        // Head flip matches body direction (right/left)
        headRenderer.flipX = (dirIndex == 2);

        lastPos = pos;
    }

    public void SetAttacking(bool attacking)
    {
        if (isAttacking == attacking) return;
        isAttacking = attacking;
        headIndex = 0; bodyIndex = 0;
        headTimer = 0f; bodyTimer = 0f;
        forcedAttackFrame = -1;
    }

    /// <summary>Force a specific attack frame index on the body (0..N-1).</summary>
    public void SetBodyAttackFrame(int index)
    {
        forcedAttackFrame = index;
    }

    public void FlashRed(float duration)
    {
        StartCoroutine(FlashCoroutine(duration));
    }

    private IEnumerator FlashCoroutine(float duration)
    {
        if (bodyRenderer != null) bodyRenderer.color = Color.red;
        if (headRenderer != null) headRenderer.color = Color.red;
        yield return new WaitForSeconds(duration);
        if (bodyRenderer != null) bodyRenderer.color = Color.white;
        if (headRenderer != null) headRenderer.color = Color.white;
    }

    public void PlayDeathAnimation()
    {
        StartCoroutine(DeathFade());
    }

    private IEnumerator DeathFade()
    {
        if (deathSprite != null)
        {
            if (headRenderer != null) headRenderer.sprite = deathSprite;
            if (bodyRenderer != null) bodyRenderer.sprite = null;
        }

        float t = 0f;
        float dur = 0.45f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float a = 1f - t / dur;
            if (bodyRenderer != null) bodyRenderer.color = new Color(1f, 1f, 1f, a);
            if (headRenderer != null) headRenderer.color = new Color(1f, 1f, 1f, a);
            yield return null;
        }
    }
}

