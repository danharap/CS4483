using UnityEngine;

/// <summary>
/// Makes a Light component flicker randomly — used in the boss arena
/// to create an ominous atmosphere. Satisfies Activity 3 Step 2:
/// "Modify Unity's lighting to reinforce tone."
/// </summary>
[RequireComponent(typeof(Light))]
public class LightFlicker : MonoBehaviour
{
    [Header("Flicker Settings")]
    [SerializeField] private float baseIntensity  = 2.5f;
    [SerializeField] private float flickerAmount  = 1.2f;
    [SerializeField] private float flickerSpeed   = 8f;
    [SerializeField] private float irregularity   = 0.4f;  // random offset to make it feel alive

    private Light lt;
    private float noiseOffset;

    void Awake()
    {
        lt = GetComponent<Light>();
        noiseOffset = Random.Range(0f, 100f);
    }

    void Update()
    {
        float noise = Mathf.PerlinNoise(Time.time * flickerSpeed, noiseOffset);
        float irregular = Random.Range(-irregularity, irregularity) * Time.deltaTime * flickerSpeed;
        lt.intensity = baseIntensity + (noise + irregular - 0.5f) * flickerAmount;
        lt.intensity = Mathf.Max(0f, lt.intensity);
    }
}
