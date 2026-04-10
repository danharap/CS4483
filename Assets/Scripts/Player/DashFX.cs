using System.Collections;
using UnityEngine;

/// <summary>
/// Spawns a procedural dust burst at the player's feet at the start of each dash.
/// Attach to the same GameObject as PlayerController.
///
/// No art assets required — particles are created at runtime with a grey/white colour.
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class DashFX : MonoBehaviour
{
    [Header("Dust Particles")]
    [SerializeField] private int   particleCount  = 18;
    [SerializeField] private float speed          = 5.5f;
    [SerializeField] private float lifetime       = 0.38f;
    [SerializeField] private float startSize      = 0.28f;
    [SerializeField] private Color dustColor      = new Color(0.85f, 0.82f, 0.75f, 0.75f);

    [Header("Screen Shake (optional)")]
    [SerializeField] private bool  enableShake    = true;
    [SerializeField] private float shakeMagnitude = 0.08f;
    [SerializeField] private float shakeDuration  = 0.12f;

    private PlayerController pc;
    private ParticleSystem   ps;
    private Coroutine        shakeCoroutine;

    void Awake()
    {
        pc = GetComponent<PlayerController>();
    }

    void Start()
    {
        pc.OnDashEnd += OnDashEnd; // fires at end of dash frame — we intercept start via Update
        BuildParticleSystem();
    }

    void OnDestroy()
    {
        if (pc != null) pc.OnDashEnd -= OnDashEnd;
    }

    // Track dash start ourselves (OnDashEnd fires at finish; we want start).
    private bool wasDashing;

    void Update()
    {
        bool dashing = pc.IsDashing;
        if (dashing && !wasDashing)
            EmitDust();
        wasDashing = dashing;
    }

    private void OnDashEnd() { /* reserved for future trail effects */ }

    private void EmitDust()
    {
        if (ps == null) return;

        ps.transform.position = transform.position + Vector3.down * 0.1f;

        var burst = new ParticleSystem.Burst(0f, particleCount);
        ps.emission.SetBurst(0, burst);
        ps.Play();

        if (enableShake)
        {
            if (shakeCoroutine != null) StopCoroutine(shakeCoroutine);
            shakeCoroutine = StartCoroutine(ShakeCamera());
        }
    }

    private void BuildParticleSystem()
    {
        GameObject psGo = new GameObject("DashDust");
        psGo.transform.SetParent(transform, false);
        ps = psGo.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.loop             = false;
        main.playOnAwake      = false;
        main.startLifetime    = lifetime;
        main.startSpeed       = new ParticleSystem.MinMaxCurve(speed * 0.6f, speed);
        main.startSize        = startSize;
        main.startColor       = new ParticleSystem.MinMaxGradient(
            new Color(dustColor.r, dustColor.g, dustColor.b, 0.35f),
            dustColor);
        main.gravityModifier  = 0.4f;
        main.simulationSpace  = ParticleSystemSimulationSpace.World;
        main.maxParticles     = 64;

        var emission = ps.emission;
        emission.enabled = true;
        emission.SetBurst(0, new ParticleSystem.Burst(0f, particleCount));

        var shape = ps.shape;
        shape.enabled     = true;
        shape.shapeType   = ParticleSystemShapeType.Circle;
        shape.radius      = 0.35f;
        shape.arc         = 360f;

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(g);

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sc = new AnimationCurve();
        sc.AddKey(0f, 1f);
        sc.AddKey(1f, 0f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sc);

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.material = new Material(Shader.Find("Sprites/Default"));
        renderer.sortingOrder = 15;
    }

    private IEnumerator ShakeCamera()
    {
        Camera cam = Camera.main;
        if (cam == null) yield break;

        Vector3 originPos = cam.transform.localPosition;
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;
            float dampen = 1f - (elapsed / shakeDuration);
            cam.transform.localPosition = originPos +
                (Vector3)UnityEngine.Random.insideUnitCircle * shakeMagnitude * dampen;
            yield return null;
        }
        cam.transform.localPosition = originPos;
    }
}
