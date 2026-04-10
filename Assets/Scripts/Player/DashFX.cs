using System.Collections;
using UnityEngine;

/// <summary>
/// Spawns a procedural dust burst at the player's feet at the start of each dash.
/// Uses small SpriteRenderer quads that fade and shrink — no Particle System package required.
/// Auto-attaches to the Player at runtime; also wired by SetupAll in the editor.
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class DashFX : MonoBehaviour
{
    // Automatically adds this component to the player when the game starts if it is missing.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoAttach()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null && player.GetComponent<DashFX>() == null)
            player.AddComponent<DashFX>();
    }
    [Header("Dust Puffs")]
    [SerializeField] private int   puffCount     = 10;
    [SerializeField] private float puffSpeed     = 4.5f;
    [SerializeField] private float puffLifetime  = 0.32f;
    [SerializeField] private float puffStartSize = 0.30f;
    [SerializeField] private Color puffColor     = new Color(0.88f, 0.84f, 0.76f, 0.70f);

    [Header("Screen Shake (optional)")]
    [SerializeField] private bool  enableShake    = true;
    [SerializeField] private float shakeMagnitude = 0.08f;
    [SerializeField] private float shakeDuration  = 0.12f;

    private PlayerController pc;
    private bool             wasDashing;
    private Material         puffMaterial;
    private Coroutine        shakeCoroutine;

    void Awake()
    {
        pc = GetComponent<PlayerController>();
        puffMaterial = new Material(Shader.Find("Sprites/Default"));
    }

    void Start()
    {
        pc.OnDashEnd += OnDashEnd;
    }

    void OnDestroy()
    {
        if (pc != null) pc.OnDashEnd -= OnDashEnd;
        if (puffMaterial != null) Destroy(puffMaterial);
    }

    void Update()
    {
        bool dashing = pc.IsDashing;
        if (dashing && !wasDashing)
            EmitPuffs();
        wasDashing = dashing;
    }

    private void OnDashEnd() { /* reserved */ }

    // ── Puff burst ────────────────────────────────────────────────────────

    private void EmitPuffs()
    {
        Vector3 origin = transform.position + Vector3.down * 0.05f;

        for (int i = 0; i < puffCount; i++)
        {
            // Random direction on the XZ plane, slight upward bias.
            Vector2 flat = Random.insideUnitCircle.normalized;
            Vector3 dir  = new Vector3(flat.x, Random.Range(0f, 0.4f), flat.y).normalized;
            float   spd  = puffSpeed * Random.Range(0.5f, 1.0f);

            StartCoroutine(AnimatePuff(origin, dir, spd));
        }

        if (enableShake)
        {
            if (shakeCoroutine != null) StopCoroutine(shakeCoroutine);
            shakeCoroutine = StartCoroutine(ShakeCamera());
        }
    }

    private IEnumerator AnimatePuff(Vector3 startPos, Vector3 dir, float speed)
    {
        // Create a tiny quad with a SpriteRenderer.
        GameObject go = new GameObject("DashPuff");
        go.transform.position = startPos;

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite       = BuildDotSprite();
        sr.material     = puffMaterial;
        sr.color        = puffColor;
        sr.sortingOrder = 20;

        float elapsed = 0f;
        float size    = puffStartSize;
        Color c       = puffColor;

        while (elapsed < puffLifetime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / puffLifetime;

            // Move outward, decelerating.
            go.transform.position += dir * speed * (1f - t) * Time.deltaTime;

            // Shrink and fade.
            float s = Mathf.Lerp(size, 0f, t);
            go.transform.localScale = new Vector3(s, s, s);
            c.a = Mathf.Lerp(puffColor.a, 0f, t);
            sr.color = c;

            yield return null;
        }

        Destroy(go);
    }

    // ── Shared dot sprite (1×1 white pixel) ──────────────────────────────

    private static Sprite _dotSprite;
    private static Sprite BuildDotSprite()
    {
        if (_dotSprite != null) return _dotSprite;
        Texture2D tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[16];
        for (int i = 0; i < 16; i++) pixels[i] = Color.white;
        tex.SetPixels(pixels);
        tex.Apply();
        _dotSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
        return _dotSprite;
    }

    // ── Screen shake ──────────────────────────────────────────────────────

    private IEnumerator ShakeCamera()
    {
        Camera cam = Camera.main;
        if (cam == null) yield break;

        Vector3 origin  = cam.transform.localPosition;
        float   elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;
            float dampen = 1f - (elapsed / shakeDuration);
            cam.transform.localPosition = origin +
                (Vector3)Random.insideUnitCircle * shakeMagnitude * dampen;
            yield return null;
        }

        cam.transform.localPosition = origin;
    }
}
