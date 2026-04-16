using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spike trap behavior:
/// - Idles on frame 1 (first sprite frame)
/// - When player enters radius, spikes animate up once, deal damage, and stun for 1 second
/// - Then returns to idle frame until next trigger
/// </summary>
[RequireComponent(typeof(Collider))]
public class SpikeTrap : MonoBehaviour
{
    [Header("Spike Stats")]
    [SerializeField] private float damage = 15f;
    [SerializeField] private float stunDuration = 1f;
    [SerializeField] private float triggerCooldown = 1.0f;
    [SerializeField] private float activeDuration = 0.35f;
    [SerializeField] private float damageRadius = 1.2f;

    [Header("Spike Visuals")]
    [SerializeField] private TrapSpriteAnimator spriteAnimator;

    private bool isTriggering;
    private float cooldownTimer;

    void Awake()
    {
        if (spriteAnimator == null)
            spriteAnimator = EnsureAnimator();

        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
        if (col is SphereCollider sc) sc.radius = damageRadius;
    }

    void Start()
    {
        // Idle pose/frame (frame 0) until player steps in.
        if (spriteAnimator != null) spriteAnimator.SetActive(false);
    }

    void Update()
    {
        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;
    }

    void OnTriggerEnter(Collider other)
    {
        if (isTriggering || cooldownTimer > 0f) return;
        if (!other.CompareTag("Player")) return;

        PlayerHealth ph = other.GetComponent<PlayerHealth>();
        PlayerController pc = other.GetComponent<PlayerController>();
        if (ph == null) return;

        StartCoroutine(TriggerOnce(ph, pc));
    }

    private IEnumerator TriggerOnce(PlayerHealth ph, PlayerController pc)
    {
        isTriggering = true;
        cooldownTimer = triggerCooldown;

        if (spriteAnimator != null) spriteAnimator.SetActive(true);

        ph.TakeDamage(damage);
        if (pc != null) pc.StunFor(stunDuration);

        yield return new WaitForSeconds(activeDuration);

        // Return to frame 1 idle state.
        if (spriteAnimator != null) spriteAnimator.SetActive(false);
        isTriggering = false;
    }

    private TrapSpriteAnimator EnsureAnimator()
    {
        TrapSpriteAnimator existing = GetComponentInChildren<TrapSpriteAnimator>(true);
        if (existing != null)
        {
            existing.sortingOrder = Mathf.Max(existing.sortingOrder, 26);
            return existing;
        }

        Transform visual = transform.Find("TrapVisual");
        if (visual == null)
        {
            GameObject go = new GameObject("TrapVisual");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, 0.58f, 0f);
            visual = go.transform;
        }
        else
        {
            visual.localPosition = new Vector3(visual.localPosition.x, 0.58f, visual.localPosition.z);
        }

        TrapSpriteAnimator anim = visual.gameObject.AddComponent<TrapSpriteAnimator>();
        anim.frameRate = 16f;
        anim.sortingOrder = 26;
        anim.playOnAwake = false;
        anim.enableGlow = false;
        anim.spriteScale = new Vector3(1.8f, 1.8f, 1f);

        Sprite[] frames = LoadFramesFromResources("Traps/SpikeTrap");
        if (frames.Length > 0)
            anim.SetFrames(frames);
        else
            Debug.LogWarning($"[SpikeTrap] No sprites at Resources/Traps/SpikeTrap. Object: {name}", this);

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
