using System.Collections;
using UnityEngine;

/// <summary>
/// Dynamic worldbuilding system (Activity 3, Step 4 — Player Impact on the World).
/// WorldBeacons are dormant lights placed around the arena. Each time the player
/// clears a wave, the next beacon "activates" — its light brightens and its material
/// emits a glow — visually showing the player's combat prowess restoring the Grounds.
/// </summary>
public class WorldBeacon : MonoBehaviour
{
    // ── Tunables ──────────────────────────────────────────────────────────
    [Header("Beacon Settings")]
    [SerializeField] private int   activatesOnWave = 1;     // which wave clear triggers this beacon
    [SerializeField] private Color dormantColor    = new Color(0.1f, 0.1f, 0.1f);
    [SerializeField] private Color activeColor     = new Color(0.3f, 0.7f, 1.0f);
    [SerializeField] private float activationTime  = 1.5f;

    [Header("References")]
    [SerializeField] private Light  beaconLight;            // point light on this GO
    [SerializeField] private Renderer beaconRenderer;       // the pillar/prop mesh

    // ── State ─────────────────────────────────────────────────────────────
    private bool isActive;
    private float dormantIntensity;
    private float activeIntensity = 2.5f;

    void Awake()
    {
        if (beaconLight == null)   beaconLight    = GetComponentInChildren<Light>();
        if (beaconRenderer == null) beaconRenderer = GetComponentInChildren<Renderer>();

        SetDormant();
    }

    void Start()
    {
        var wm = GameManager.Instance?.WaveManager;
        if (wm != null)
            wm.OnWaveCleared += OnWaveCleared;
    }

    void OnDestroy()
    {
        var wm = GameManager.Instance?.WaveManager;
        if (wm != null)
            wm.OnWaveCleared -= OnWaveCleared;
    }

    private void OnWaveCleared(int waveIndex)
    {
        // waveIndex is 0-based; activatesOnWave is 1-based display wave
        if (!isActive && waveIndex + 1 >= activatesOnWave)
            StartCoroutine(Activate());
    }

    private void SetDormant()
    {
        if (beaconLight)
        {
            dormantIntensity    = 0.1f;
            beaconLight.color     = dormantColor;
            beaconLight.intensity = dormantIntensity;
        }
        if (beaconRenderer)
            beaconRenderer.material.color = dormantColor;
    }

    private IEnumerator Activate()
    {
        isActive = true;
        float elapsed = 0f;

        while (elapsed < activationTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / activationTime;

            if (beaconLight)
            {
                beaconLight.color     = Color.Lerp(dormantColor, activeColor, t);
                beaconLight.intensity = Mathf.Lerp(dormantIntensity, activeIntensity, t);
            }
            if (beaconRenderer)
                beaconRenderer.material.color = Color.Lerp(dormantColor, activeColor, t);

            yield return null;
        }

        if (beaconLight)
        {
            beaconLight.color     = activeColor;
            beaconLight.intensity = activeIntensity;
        }
        if (beaconRenderer)
            beaconRenderer.material.color = activeColor;
    }
}
