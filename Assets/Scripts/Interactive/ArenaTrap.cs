using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Arena trap: when the player steps on it (trigger enter), deals damage and stuns for a short duration.
/// Requires a Collider with Is Trigger = true. Player must be tagged "Player".
/// Auto-initializes its TrapSpriteAnimator from Resources if one isn't wired in the Inspector.
/// </summary>
[RequireComponent(typeof(Collider))]
public class ArenaTrap : MonoBehaviour
{
    [Header("Trap Effect")]
    [SerializeField] private float damage = 10f;
    [SerializeField] private float stunDuration = 1.5f;
    [Tooltip("Seconds before this trap can trigger again (avoids repeated triggers while standing).")]
    [SerializeField] private float cooldownAfterTrigger = 2f;

    [Header("Optional")]
    [SerializeField] private string playerTag = "Player";

    [Header("Spike Trap Animation (Optional)")]
    [SerializeField] private TrapSpriteAnimator spriteAnimator;

    private float cooldownTimer;

    void Awake()
    {
        // If the scene has traps that were placed without running SpriteSetup, self-initialize.
        if (spriteAnimator == null)
            spriteAnimator = EnsureAnimator("Traps/SpikeTrap", playOnAwake: true,
                                            frameRate: 18f, glowColor: new Color(0.3f, 0.9f, 1f),
                                            glowIntensity: 1.4f, glowRange: 2.2f);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        if (cooldownTimer > 0f) return;

        PlayerHealth health = other.GetComponent<PlayerHealth>();
        PlayerController controller = other.GetComponent<PlayerController>();

        if (health != null)
            health.TakeDamage(damage);
        if (controller != null)
            controller.StunFor(stunDuration);

        cooldownTimer = cooldownAfterTrigger;

        if (spriteAnimator != null)
            spriteAnimator.SetActive(true);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        if (spriteAnimator != null)
            spriteAnimator.SetActive(false);
    }

    void Update()
    {
        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;
    }

    // ── Shared helper ──────────────────────────────────────────────────────

    /// <summary>
    /// Finds or creates a TrapSpriteAnimator on the TrapVisual child (or this GameObject),
    /// loads sprite frames from a Resources subfolder, and returns it.
    /// </summary>
    protected TrapSpriteAnimator EnsureAnimator(string resourceFolder, bool playOnAwake,
                                                float frameRate, Color glowColor,
                                                float glowIntensity, float glowRange)
    {
        // Prefer an existing TrapSpriteAnimator in children.
        TrapSpriteAnimator existing = GetComponentInChildren<TrapSpriteAnimator>(true);
        if (existing != null)
            return existing;

        // Find or create the TrapVisual child.
        Transform visual = transform.Find("TrapVisual");
        if (visual == null)
        {
            GameObject go = new GameObject("TrapVisual");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Vector3.zero;
            visual = go.transform;
        }

        TrapSpriteAnimator anim = visual.gameObject.AddComponent<TrapSpriteAnimator>();
        anim.frameRate     = frameRate;
        anim.sortingOrder  = 2;
        anim.playOnAwake   = playOnAwake;
        anim.enableGlow    = true;
        anim.glowColor     = glowColor;
        anim.glowIntensity = glowIntensity;
        anim.glowRange     = glowRange;

        Sprite[] frames = LoadFramesFromResources(resourceFolder);
        if (frames.Length > 0)
            anim.SetFrames(frames);

        return anim;
    }

    private static Sprite[] LoadFramesFromResources(string folder)
    {
        var list = new List<Sprite>();
        for (int i = 0; i < 32; i++)
        {
            Sprite s = Resources.Load<Sprite>($"{folder}/{i}");
            if (s == null) break;
            list.Add(s);
        }
        return list.ToArray();
    }
}
