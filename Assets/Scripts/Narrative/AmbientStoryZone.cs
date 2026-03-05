using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// A zone that plays atmospheric audio and a brief text blurb when entered.
/// Used in the boss arena and hub center. Satisfies Activity 3 Step 3:
/// "Use Unity's AudioSource to play a recorded message or ambient sound."
/// Requires a Trigger Collider on this GameObject.
/// </summary>
public class AmbientStoryZone : MonoBehaviour
{
    // ── Tunables ──────────────────────────────────────────────────────────
    [Header("Text")]
    [TextArea(2, 4)]
    [SerializeField] private string  ambientText      = "The air here feels wrong.";
    [SerializeField] private float   textDuration     = 3.5f;

    [Header("Audio")]
    [SerializeField] private AudioClip  audioClip;
    [SerializeField] private float      volume        = 0.5f;
    [SerializeField] private bool       loopAudio     = false;

    [Header("UI (wired by NarrativeSetup)")]
    [SerializeField] private TMP_Text   ambientTextUI;

    // ── State ─────────────────────────────────────────────────────────────
    private AudioSource audioSrc;
    private bool triggered;

    void Awake()
    {
        audioSrc = gameObject.AddComponent<AudioSource>();
        audioSrc.playOnAwake  = false;
        audioSrc.loop         = loopAudio;
        audioSrc.spatialBlend = 1f;   // 3D sound
        audioSrc.volume       = volume;
        if (audioClip != null) audioSrc.clip = audioClip;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (triggered && !loopAudio) return;
        triggered = true;

        if (audioClip != null) audioSrc.Play();
        StartCoroutine(ShowText());
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (loopAudio) audioSrc.Stop();
    }

    private IEnumerator ShowText()
    {
        if (ambientTextUI == null) yield break;

        ambientTextUI.text  = ambientText;
        ambientTextUI.alpha = 0f;

        float t = 0f;
        while (t < 0.3f)
        {
            t += Time.deltaTime;
            ambientTextUI.alpha = Mathf.Lerp(0f, 1f, t / 0.3f);
            yield return null;
        }

        yield return new WaitForSeconds(textDuration);

        t = 0f;
        while (t < 0.5f)
        {
            t += Time.deltaTime;
            ambientTextUI.alpha = Mathf.Lerp(1f, 0f, t / 0.5f);
            yield return null;
        }
        ambientTextUI.alpha = 0f;
        ambientTextUI.text  = "";
    }

    public void SetTextUI(TMP_Text ui) => ambientTextUI = ui;
}
