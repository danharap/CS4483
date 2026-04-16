using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Arena flame trap:
/// - Always active visual (continuous hazard)
/// - No warning circle / telegraph
/// - Applies repeating damage while player stays inside the trigger radius
/// </summary>
[RequireComponent(typeof(Collider))]
public class FlameTrap : MonoBehaviour
{
    [Header("Burn Effect")]
    [Tooltip("Damage applied each tick while the player stays inside the active radius.")]
    [SerializeField] private float damagePerTick = 5f;
    [Tooltip("Seconds between damage ticks when player stays inside.")]
    [SerializeField] private float tickInterval  = 0.6f;
    [Tooltip("Radius used for damage checks. Should match warnRadius.")]
    [SerializeField] private float damageRadius  = 1.2f;

    [Header("Flame Animation")]
    [SerializeField] private TrapSpriteAnimator spriteAnimator;

    private readonly Dictionary<int, float> nextDamageAt = new Dictionary<int, float>();

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
        if (spriteAnimator != null)
            spriteAnimator.SetActive(true);
    }

    void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        PlayerHealth ph = other.GetComponent<PlayerHealth>();
        if (ph == null) return;

        int id = other.GetInstanceID();
        if (!nextDamageAt.TryGetValue(id, out float t))
            t = 0f;
        if (Time.time < t) return;

        ph.TakeDamage(damagePerTick);
        nextDamageAt[id] = Time.time + tickInterval;
    }

    void OnTriggerExit(Collider other)
    {
        nextDamageAt.Remove(other.GetInstanceID());
    }

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
            go.transform.localPosition = new Vector3(0f, 0.58f, 0f);
            visual = go.transform;
        }
        else
        {
            visual.localPosition = new Vector3(visual.localPosition.x, 0.58f, visual.localPosition.z);
        }

        TrapSpriteAnimator anim = visual.gameObject.AddComponent<TrapSpriteAnimator>();
        anim.frameRate     = 15f;
        anim.sortingOrder  = 25;
        anim.playOnAwake   = true;
        anim.enableGlow    = true;
        anim.spriteScale   = new Vector3(1.8f, 1.8f, 1f);
        anim.glowColor     = new Color(1f, 0.45f, 0.05f);
        anim.glowIntensity = 2.0f;
        anim.glowRange     = 3.0f;

        Sprite[] frames = LoadFramesFromResources("Traps/FlameTrap");
        if (frames.Length > 0)
            anim.SetFrames(frames);
        else
            Debug.LogWarning($"[FlameTrap] No sprites at Resources/Traps/FlameTrap — run menu: CS4483 → Fix Resources trap textures (Sprite import). Object: {name}", this);

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
