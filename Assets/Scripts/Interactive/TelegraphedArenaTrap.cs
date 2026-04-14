using System.Collections;
using UnityEngine;

/// <summary>
/// Base for arena traps that run a timed warn → active → cooldown loop.
/// Subclasses override OnWarnStart / OnWarnEnd / OnActivate / OnDeactivate.
/// </summary>
public abstract class TelegraphedArenaTrap : MonoBehaviour
{
    [Header("Trap Timing")]
    [Tooltip("Seconds from warn start to trap activation.")]
    [SerializeField] protected float warnDuration   = 0.85f;
    [Tooltip("Seconds the trap stays active and damaging.")]
    [SerializeField] protected float activeDuration = 0.75f;
    [Tooltip("Seconds before the trap cycles again after retracting.")]
    [SerializeField] protected float cooldownDuration = 3.5f;
    [Tooltip("Seconds between trap cycles before the first warn (initial random offset).")]
    [SerializeField] protected float initialDelay   = 0f;

    [Header("Warning Indicator")]
    [SerializeField] protected Color warnColor      = new Color(1f, 0.2f, 0.1f, 0.55f);
    [SerializeField] protected float warnRadius     = 1.2f;
    [SerializeField] protected bool  pulseWarn      = true;

    protected bool isActive;
    private   LineRenderer warnRing;

    protected virtual void Start()
    {
        StartCoroutine(TrapCycle());
    }

    /// <summary>Called from tutorial via SendMessage before Start() for a fast demo cycle.</summary>
    public void SetDemoCycleTimings()
    {
        initialDelay     = Random.Range(0.3f, 0.8f);
        cooldownDuration = 1.4f;
    }

    private IEnumerator TrapCycle()
    {
        if (initialDelay > 0f)
            yield return new WaitForSeconds(initialDelay);

        while (true)
        {
            EnsureWarnRing();
            OnWarnStart();
            float t = 0f;
            while (t < warnDuration)
            {
                t += Time.deltaTime;
                float pulse = pulseWarn ? 0.5f + 0.5f * Mathf.Sin(t * Mathf.PI * 4f) : 1f;
                SetWarnRingAlpha(warnColor.a * pulse);
                yield return null;
            }
            HideWarnRing();
            OnWarnEnd();

            isActive = true;
            OnActivate();
            yield return new WaitForSeconds(activeDuration);
            isActive = false;
            OnDeactivate();

            yield return new WaitForSeconds(cooldownDuration);
        }
    }

    private void EnsureWarnRing()
    {
        if (warnRing != null) { warnRing.enabled = true; return; }
        GameObject go = new GameObject("WarnRing");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = Vector3.zero;
        warnRing = go.AddComponent<LineRenderer>();
        warnRing.useWorldSpace   = true;
        warnRing.loop            = true;
        warnRing.positionCount   = 40;
        warnRing.startWidth      = 0.06f;
        warnRing.endWidth        = 0.06f;
        Material m = new Material(Shader.Find("Sprites/Default"));
        warnRing.material        = m;
        warnRing.sortingOrder    = 20;
        UpdateWarnRingPositions();
    }

    private void UpdateWarnRingPositions()
    {
        if (warnRing == null) return;
        Vector3 c = transform.position;
        int n = warnRing.positionCount;
        for (int i = 0; i < n; i++)
        {
            float a = (i / (float)n) * Mathf.PI * 2f;
            warnRing.SetPosition(i, new Vector3(
                c.x + Mathf.Cos(a) * warnRadius,
                0.06f,
                c.z + Mathf.Sin(a) * warnRadius));
        }
    }

    private void SetWarnRingAlpha(float alpha)
    {
        if (warnRing == null) return;
        Color c = warnColor; c.a = alpha;
        warnRing.startColor = c;
        warnRing.endColor   = c;
    }

    private void HideWarnRing()
    {
        if (warnRing != null) warnRing.enabled = false;
    }

    protected virtual void OnWarnStart()  { }
    protected virtual void OnWarnEnd()    { }
    protected virtual void OnActivate()   { }
    protected virtual void OnDeactivate() { }
}
