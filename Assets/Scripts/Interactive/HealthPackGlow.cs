using UnityEngine;

/// <summary>
/// Pulsing glow for health packs. Assumes there is a child Light named "GlowLight".
/// Uses ForcePixel render mode so the light is never downgraded in busy scenes (arena).
/// </summary>
public class HealthPackGlow : MonoBehaviour
{
    [SerializeField] private Light glowLight;
    [SerializeField] private float baseIntensity = 1.4f;  // subtle base (was 4.0)
    [SerializeField] private float pulseAmount   = 0.7f;  // gentle pulse range (was 2.0)
    [SerializeField] private float pulseSpeed    = 1.6f;
    [SerializeField] private float lightRange    = 9f;    // wider reach for open arena (was 6)

    void Awake()
    {
        if (glowLight == null)
        {
            Transform t = transform.Find("GlowLight");
            if (t != null) glowLight = t.GetComponent<Light>();
        }

        if (glowLight != null)
        {
            // ForcePixel bypasses Unity's per-object pixel light count limit, which is what
            // causes the glow to disappear in the arena when many enemy/effect lights are active.
            glowLight.renderMode = LightRenderMode.ForcePixel;
            glowLight.range = lightRange;
        }
    }

    void Update()
    {
        if (glowLight == null) return;
        float t = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f; // 0..1
        glowLight.intensity = baseIntensity + pulseAmount * t;
    }
}

