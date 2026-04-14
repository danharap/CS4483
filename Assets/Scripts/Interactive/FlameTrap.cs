using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Arena flame trap. Warn ring → active window with repeating fire damage inside radius → cooldown.
/// Fire visuals run only during the active phase (warning shows the orange ring only).
/// Auto-initializes its TrapSpriteAnimator from Resources if one isn't wired in the Inspector.
/// </summary>
[RequireComponent(typeof(Collider))]
public class FlameTrap : TelegraphedArenaTrap
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

    private Coroutine damageLoop;

    void Awake()
    {
        if (spriteAnimator == null)
            spriteAnimator = EnsureAnimator();
    }

    protected override void Start()
    {
        initialDelay = Random.Range(0f, cooldownDuration);
        warnRadius   = damageRadius;
        warnColor    = new Color(1f, 0.45f, 0.05f, 0.55f);
        base.Start();
    }

    protected override void OnWarnStart()
    {
        if (spriteAnimator != null)
            spriteAnimator.SetActive(false);
    }

    protected override void OnActivate()
    {
        if (spriteAnimator != null)
            spriteAnimator.SetActive(true);

        if (damageLoop != null) StopCoroutine(damageLoop);
        damageLoop = StartCoroutine(DamageTick());
    }

    protected override void OnDeactivate()
    {
        if (spriteAnimator != null)
            spriteAnimator.SetActive(false);
        if (damageLoop != null) { StopCoroutine(damageLoop); damageLoop = null; }
    }

    private IEnumerator DamageTick()
    {
        while (isActive)
        {
            DamagePlayersInRadius();
            yield return new WaitForSeconds(tickInterval);
        }
    }

    private void DamagePlayersInRadius()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, damageRadius);
        foreach (Collider c in hits)
        {
            if (!c.CompareTag("Player")) continue;
            PlayerHealth ph = c.GetComponent<PlayerHealth>();
            if (ph != null) ph.TakeDamage(damagePerTick);
        }
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
            go.transform.localPosition = Vector3.zero;
            visual = go.transform;
        }

        TrapSpriteAnimator anim = visual.gameObject.AddComponent<TrapSpriteAnimator>();
        anim.frameRate     = 15f;
        anim.sortingOrder  = 25;
        anim.playOnAwake   = true;
        anim.enableGlow    = true;
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
