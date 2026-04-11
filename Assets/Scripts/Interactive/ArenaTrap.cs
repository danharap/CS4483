using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spike trap: stepping on it deals damage and stuns briefly. Sprite stays visible on
/// frame 0 until the player enters the trigger, then animates while they stand on it.
/// If TrapSpriteAnimator is not wired in the scene, frames are loaded from
/// Resources/Traps/SpikeTrap (must be imported as Sprite — run CS4483 menu fix once).
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
        if (spriteAnimator == null)
            spriteAnimator = EnsureSpikeAnimator();
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

    TrapSpriteAnimator EnsureSpikeAnimator()
    {
        TrapSpriteAnimator existing = GetComponentInChildren<TrapSpriteAnimator>(true);
        if (existing != null)
            return existing;

        Transform visual = transform.Find("TrapVisual");
        if (visual == null)
        {
            var go = new GameObject("TrapVisual");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Vector3.zero;
            visual = go.transform;
        }

        var anim = visual.gameObject.AddComponent<TrapSpriteAnimator>();
        anim.frameRate     = 18f;
        anim.sortingOrder  = 25;
        anim.playOnAwake   = false;
        anim.enableGlow    = true;
        anim.glowColor     = new Color(0.3f, 0.9f, 1f);
        anim.glowIntensity = 1.4f;
        anim.glowRange     = 2.2f;

        Sprite[] frames = LoadFramesFromResources("Traps/SpikeTrap");
        if (frames.Length > 0)
            anim.SetFrames(frames);
        else
            Debug.LogWarning($"[ArenaTrap] No sprites at Resources/Traps/SpikeTrap — run menu: CS4483 → Fix Resources trap textures (Sprite import). Object: {name}", this);

        return anim;
    }

    static Sprite[] LoadFramesFromResources(string folder)
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
