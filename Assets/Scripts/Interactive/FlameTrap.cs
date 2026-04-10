using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Arena 2 flame trap. When the player steps on it, they receive burn damage
/// as repeating ticks for a short duration. Unlike the spike trap there is no
/// stun. The flame animation plays continuously (fire is always lit).
/// Auto-initializes its TrapSpriteAnimator from Resources if one isn't wired in the Inspector.
/// </summary>
[RequireComponent(typeof(Collider))]
public class FlameTrap : MonoBehaviour
{
    [Header("Burn Effect")]
    [Tooltip("Damage applied each burn tick.")]
    [SerializeField] private float damagePerTick  = 5f;
    [Tooltip("Seconds between each damage tick.  Should be >= PlayerHealth.iFramesDuration.")]
    [SerializeField] private float tickInterval   = 0.6f;
    [Tooltip("Total seconds the player burns after stepping on the trap.")]
    [SerializeField] private float burnDuration   = 3f;
    [Tooltip("How long before this trap can burn the same player again.")]
    [SerializeField] private float triggerCooldown = 4f;

    [Header("Flame Animation")]
    [SerializeField] public TrapSpriteAnimator spriteAnimator;

    private float cooldownTimer;
    private Coroutine burnRoutine;

    void Awake()
    {
        // Self-initialize if the spriteAnimator wasn't wired via the Inspector / SpriteSetup.
        if (spriteAnimator == null)
            spriteAnimator = EnsureAnimator();
    }

    void Start()
    {
        // Fire is always burning — start the animation immediately.
        if (spriteAnimator != null)
            spriteAnimator.SetActive(true);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (cooldownTimer > 0f) return;

        PlayerHealth health = other.GetComponent<PlayerHealth>();
        if (health == null) return;

        if (burnRoutine != null) StopCoroutine(burnRoutine);
        burnRoutine = StartCoroutine(BurnPlayer(health));
        cooldownTimer = triggerCooldown;
    }

    private IEnumerator BurnPlayer(PlayerHealth health)
    {
        float elapsed = 0f;
        while (elapsed < burnDuration)
        {
            if (health == null) yield break;
            health.TakeDamage(damagePerTick);
            elapsed += tickInterval;
            yield return new WaitForSeconds(tickInterval);
        }
    }

    void Update()
    {
        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;
    }

    // ── Visual self-init ───────────────────────────────────────────────────

    private TrapSpriteAnimator EnsureAnimator()
    {
        TrapSpriteAnimator existing = GetComponentInChildren<TrapSpriteAnimator>(true);
        if (existing != null)
            return existing;

        Transform visual = transform.Find("TrapVisual");
        if (visual == null)
        {
            GameObject go = new GameObject("TrapVisual");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Vector3.zero;
            visual = go.transform;
        }

        TrapSpriteAnimator anim = visual.gameObject.AddComponent<TrapSpriteAnimator>();
        anim.frameRate     = 15f;
        anim.sortingOrder  = 2;
        anim.playOnAwake   = true;
        anim.enableGlow    = true;
        anim.glowColor     = new Color(1f, 0.45f, 0.05f);
        anim.glowIntensity = 2.0f;
        anim.glowRange     = 3.0f;

        Sprite[] frames = LoadFramesFromResources("Traps/FlameTrap");
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
