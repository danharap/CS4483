using UnityEngine;

/// <summary>
/// Simple pulsing glow for health packs. Assumes there is a child Light named "GlowLight".
/// </summary>
public class HealthPackGlow : MonoBehaviour
{
    [SerializeField] private Light glowLight;
    [SerializeField] private float baseIntensity = 4.0f;
    [SerializeField] private float pulseAmount   = 2.0f;
    [SerializeField] private float pulseSpeed    = 2.0f;

    void Awake()
    {
        if (glowLight == null)
        {
            Transform t = transform.Find("GlowLight");
            if (t != null) glowLight = t.GetComponent<Light>();
        }
    }

    void Update()
    {
        if (glowLight == null) return;
        float t = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f; // 0..1
        glowLight.intensity = baseIntensity + pulseAmount * t;
    }
}

