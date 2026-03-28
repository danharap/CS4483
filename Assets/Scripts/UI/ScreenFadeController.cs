using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Fades a full-screen black panel in/out over the game UI.
/// Requires a Canvas with a black Image child wired via <see cref="fadeImage"/>.
/// SetupAll creates and wires this automatically.
/// </summary>
public class ScreenFadeController : MonoBehaviour
{
    public static ScreenFadeController Instance { get; private set; }

    [SerializeField] public Image fadeImage;
    [SerializeField] private float defaultFadeDuration = 0.6f;

    private Coroutine currentFade;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        SetAlpha(0f); // start transparent
    }

    // ── Public API ────────────────────────────────────────────────────────

    /// <summary>Fade to black over <paramref name="duration"/> seconds.</summary>
    public void FadeOut(float duration = -1f, System.Action onComplete = null)
        => StartFade(1f, duration < 0 ? defaultFadeDuration : duration, onComplete);

    /// <summary>Fade from black to transparent over <paramref name="duration"/> seconds.</summary>
    public void FadeIn(float duration = -1f, System.Action onComplete = null)
        => StartFade(0f, duration < 0 ? defaultFadeDuration : duration, onComplete);

    /// <summary>Instantly set black overlay without animation.</summary>
    public void SetBlack() => SetAlpha(1f);
    public void SetClear() => SetAlpha(0f);

    // ── Coroutine ─────────────────────────────────────────────────────────

    private void StartFade(float targetAlpha, float duration, System.Action onComplete)
    {
        if (currentFade != null) StopCoroutine(currentFade);
        currentFade = StartCoroutine(FadeRoutine(targetAlpha, duration, onComplete));
    }

    private IEnumerator FadeRoutine(float target, float duration, System.Action onComplete)
    {
        if (fadeImage == null) { onComplete?.Invoke(); yield break; }
        fadeImage.gameObject.SetActive(true);

        float start   = fadeImage.color.a;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            SetAlpha(Mathf.Lerp(start, target, elapsed / duration));
            yield return null;
        }

        SetAlpha(target);
        if (Mathf.Approximately(target, 0f))
            fadeImage.gameObject.SetActive(false);

        onComplete?.Invoke();
    }

    private void SetAlpha(float a)
    {
        if (fadeImage == null) return;
        Color c = fadeImage.color;
        c.a = a;
        fadeImage.color = c;
    }
}
