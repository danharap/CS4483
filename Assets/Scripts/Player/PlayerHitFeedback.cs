using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Enhanced hit feedback:
///   • Full-screen radial vignette that flashes red on hit.
///   • Player sprite briefly flashes white then returns to normal.
///
/// Attach to the Player GameObject. Requires a PlayerHealth component.
/// If no Canvas / damageOverlay is wired it creates its own vignette Image at runtime.
/// </summary>
[RequireComponent(typeof(PlayerHealth))]
public class PlayerHitFeedback : MonoBehaviour
{
    [Header("Vignette")]
    [Tooltip("Assigned automatically if left null.")]
    [SerializeField] private Image vignetteImage;
    [SerializeField] private float vignetteMaxAlpha  = 0.55f;
    [SerializeField] private float vignetteFadeSpeed = 3.5f;
    [SerializeField] private Color vignetteColor     = new Color(0.8f, 0.02f, 0.02f, 0f);

    [Header("Sprite Flash")]
    [SerializeField] private SpriteRenderer[] flashRenderers; // auto-populated if empty
    [SerializeField] private Color flashColor     = Color.white;
    [SerializeField] private float flashDuration  = 0.12f;
    [SerializeField] private int   flashCount     = 3;

    private PlayerHealth ph;
    private float currentAlpha;
    private Coroutine spriteFlashCoroutine;

    void Awake()
    {
        ph = GetComponent<PlayerHealth>();
    }

    void Start()
    {
        ph.OnHealthChanged += OnHealthChanged;

        if (flashRenderers == null || flashRenderers.Length == 0)
            flashRenderers = GetComponentsInChildren<SpriteRenderer>(true);

        EnsureVignette();
    }

    void OnDestroy()
    {
        if (ph != null) ph.OnHealthChanged -= OnHealthChanged;
    }

    void Update()
    {
        if (currentAlpha > 0f)
        {
            currentAlpha = Mathf.MoveTowards(currentAlpha, 0f, vignetteFadeSpeed * Time.deltaTime);
            if (vignetteImage != null)
                vignetteImage.color = new Color(vignetteColor.r, vignetteColor.g, vignetteColor.b, currentAlpha);
        }
    }

    private float lastKnownHP = -1f;

    private void OnHealthChanged(float current, float max)
    {
        bool wasDamaged = lastKnownHP > 0f && current < lastKnownHP;
        lastKnownHP = current;

        if (!wasDamaged) return;

        // Vignette flash
        currentAlpha = vignetteMaxAlpha;

        // Sprite flash
        if (flashRenderers != null && flashRenderers.Length > 0)
        {
            if (spriteFlashCoroutine != null) StopCoroutine(spriteFlashCoroutine);
            spriteFlashCoroutine = StartCoroutine(FlashSprite());
        }
    }

    private IEnumerator FlashSprite()
    {
        Color[] originalColors = new Color[flashRenderers.Length];
        for (int i = 0; i < flashRenderers.Length; i++)
            originalColors[i] = flashRenderers[i].color;

        for (int f = 0; f < flashCount; f++)
        {
            foreach (var sr in flashRenderers)
                if (sr != null) sr.color = flashColor;

            yield return new WaitForSeconds(flashDuration * 0.5f);

            for (int i = 0; i < flashRenderers.Length; i++)
                if (flashRenderers[i] != null) flashRenderers[i].color = originalColors[i];

            yield return new WaitForSeconds(flashDuration * 0.5f);
        }

        spriteFlashCoroutine = null;
    }

    // ── Vignette builder ──────────────────────────────────────────────────

    private void EnsureVignette()
    {
        if (vignetteImage != null) return;

        // Look for an existing Canvas_HUD in the scene.
        Canvas canvas = null;
        GameObject hudGo = GameObject.Find("Canvas_HUD");
        if (hudGo != null) canvas = hudGo.GetComponent<Canvas>();
        if (canvas == null) canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null) return;

        // Create a full-screen radial vignette image using a radial gradient texture.
        GameObject go = new GameObject("VignetteOverlay");
        go.transform.SetParent(canvas.transform, false);

        vignetteImage = go.AddComponent<Image>();
        vignetteImage.raycastTarget = false;
        vignetteImage.color = new Color(vignetteColor.r, vignetteColor.g, vignetteColor.b, 0f);
        vignetteImage.sprite = BuildVignetteSprite();

        RectTransform rt = vignetteImage.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        // Place behind other HUD elements (but in front of world).
        go.transform.SetSiblingIndex(0);
    }

    /// <summary>Generates a radial gradient Sprite: bright centre, dark / transparent edges.</summary>
    private static Sprite BuildVignetteSprite()
    {
        int size = 128;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Vector2 centre = new Vector2(size * 0.5f, size * 0.5f);
        float maxDist = size * 0.5f;

        Color[] pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float d = Vector2.Distance(new Vector2(x, y), centre) / maxDist;
            // White where d is high (edge), transparent where d is low (centre).
            float a = Mathf.Pow(Mathf.Clamp01(d * 1.15f), 2.2f);
            pixels[y * size + x] = new Color(1f, 1f, 1f, a);
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }
}
