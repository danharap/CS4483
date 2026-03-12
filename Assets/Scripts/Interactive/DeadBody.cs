using UnityEngine;

/// <summary>
/// Static prop: a dead body at the edge of the map. When the player is within trigger range,
/// plays the fly ambient sound (looped). Stops when the player leaves. Requires a trigger
/// collider on this GameObject. Sprite is applied by SpriteSetup; fly clip by AudioSetup.
/// </summary>
[RequireComponent(typeof(Collider))]
public class DeadBody : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] public AudioClip flySound;
    [SerializeField] [Range(0f, 1f)] private float flyVolume = 0.4f;

    private AudioSource audioSource;

    void Awake()
    {
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
            col.isTrigger = true;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && flySound != null)
            audioSource = gameObject.AddComponent<AudioSource>();
        if (audioSource != null)
        {
            audioSource.clip = flySound;
            audioSource.loop = true;
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;
            audioSource.volume = flyVolume;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (audioSource != null && flySound != null)
            audioSource.Play();
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (audioSource != null)
            audioSource.Stop();
    }
}
