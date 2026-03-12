using UnityEngine;
using System.Collections;

/// <summary>
/// Handles visuals for the HeavyEnemy:
/// - Keeps a separate head sprite anchored on top of the body
/// - Chooses head sprite + flip based on movement direction
/// - Exposes configurable offsets for each direction in the Inspector
/// Body animation is driven by an Animator that can optionally use a direction parameter.
/// </summary>
public class HeavyEnemyVisualController : MonoBehaviour
{
    [Header("References")]
    public SpriteRenderer bodyRenderer;
    public SpriteRenderer headRenderer;

    [Header("Body Animation")]
    public Sprite[] bodyUpDownFrames;
    public Sprite[] bodyRightFrames;
    public float bodyFrameRate = 6f;

    [Header("Head Sprites")]
    public Sprite headUpDownSprite;   // Moving toward player up/down
    public Sprite headRightSprite;     // Moving toward player right (or left with flipX)

    [Header("Head Offsets (local)")]
    public Vector3 headOffsetUpDown = new Vector3(0f, 0.48f, 0f);
    public Vector3 headOffsetRight  = new Vector3(0.08f, 0.48f, 0f);
    public Vector3 headOffsetLeft   = new Vector3(-0.08f, 0.48f, 0f);

    [Header("Death")]
    public Sprite deathSprite;

    private Vector3 lastPosition;
    private int     lastDirIndex; // 0=Up/Down, 1=Right (toward player), 2=Left (toward player)
    private float   bodyFrameTimer;
    private int     bodyFrameIndex;
    private Sprite[] activeBodyFrames;

    void Awake()
    {
        lastPosition = transform.position;

        // Try to auto-find references if they weren't wired
        if (bodyRenderer == null)
            bodyRenderer = GetComponentInChildren<SpriteRenderer>();

        if (headRenderer == null && bodyRenderer != null && bodyRenderer.transform != transform)
        {
            foreach (Transform child in bodyRenderer.transform)
            {
                var sr = child.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    headRenderer = sr;
                    break;
                }
            }
        }

        // Default active frames
        activeBodyFrames = (bodyUpDownFrames != null && bodyUpDownFrames.Length > 0) ? bodyUpDownFrames : bodyRightFrames;
    }

    void LateUpdate()
    {
        if (headRenderer == null)
            return;

        // Determine movement direction from position delta on XZ plane
        Vector3 pos = transform.position;
        Vector3 delta = pos - lastPosition;
        delta.y = 0f;

        // If we're barely moving, keep previous direction
        if (delta.sqrMagnitude > 0.0001f)
        {
            float absX = Mathf.Abs(delta.x);
            float absZ = Mathf.Abs(delta.z);

            if (absX >= absZ)
            {
                // Horizontal
                if (delta.x > 0f)
                    lastDirIndex = 1; // Right
                else
                    lastDirIndex = 2; // Left
            }
            else
            {
                // Vertical (up/down share same visuals)
                lastDirIndex = 0;
            }
        }

        // Apply visual state based on lastDirIndex
        // Also choose body animation frames based on direction toward player.
        switch (lastDirIndex)
        {
            case 1: // Right
                ApplyHeadState(headRightSprite != null ? headRightSprite : headUpDownSprite,
                    headOffsetRight, false);
                activeBodyFrames = (bodyRightFrames != null && bodyRightFrames.Length > 0) ? bodyRightFrames : bodyUpDownFrames;
                break;
            case 2: // Left
                ApplyHeadState(headRightSprite != null ? headRightSprite : headUpDownSprite,
                    headOffsetLeft, true);
                activeBodyFrames = (bodyRightFrames != null && bodyRightFrames.Length > 0) ? bodyRightFrames : bodyUpDownFrames;
                break;
            default: // Up/Down
                ApplyHeadState(headUpDownSprite != null ? headUpDownSprite : headRightSprite,
                    headOffsetUpDown, false);
                activeBodyFrames = (bodyUpDownFrames != null && bodyUpDownFrames.Length > 0) ? bodyUpDownFrames : bodyRightFrames;
                break;
        }

        // Animate body (cycle through active frames) and flip body when moving left
        if (bodyRenderer != null && activeBodyFrames != null && activeBodyFrames.Length > 0)
        {
            bodyFrameTimer += Time.deltaTime;
            if (bodyFrameTimer >= 1f / bodyFrameRate)
            {
                bodyFrameTimer = 0f;
                bodyFrameIndex = (bodyFrameIndex + 1) % activeBodyFrames.Length;
                bodyRenderer.sprite = activeBodyFrames[bodyFrameIndex];
            }
            bodyRenderer.flipX = (lastDirIndex == 2); // Face left when moving left toward player
        }
        else if (bodyRenderer != null && bodyRenderer.sprite == null)
        {
            // Fallback so body is never invisible if frames weren't assigned yet
            bodyRenderer.sprite = headUpDownSprite != null ? headUpDownSprite : headRightSprite;
            bodyRenderer.flipX = (lastDirIndex == 2);
        }

        lastPosition = pos;
    }

    private void ApplyHeadState(Sprite sprite, Vector3 localOffset, bool flipX)
    {
        if (sprite != null)
            headRenderer.sprite = sprite;

        headRenderer.flipX = flipX;
        headRenderer.transform.localPosition = localOffset;
    }

    /// <summary>Hit feedback: flash body and head red.</summary>
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

    /// <summary>Death: show death sprite on head and fade out.</summary>
    public void PlayDeathAnimation()
    {
        StartCoroutine(DeathFadeCoroutine());
    }

    private IEnumerator DeathFadeCoroutine()
    {
        if (headRenderer != null && deathSprite != null)
            headRenderer.sprite = deathSprite;
        float t = 0f;
        float duration = 0.4f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float a = 1f - t / duration;
            if (bodyRenderer != null) bodyRenderer.color = new Color(1f, 1f, 1f, a);
            if (headRenderer != null) headRenderer.color = new Color(1f, 1f, 1f, a);
            yield return null;
        }
    }
}

