using UnityEngine;

/// <summary>
/// Arena trap: when the player steps on it (trigger enter), deals damage and stuns for a short duration.
/// Requires a Collider with Is Trigger = true. Player must be tagged "Player".
/// </summary>
[RequireComponent(typeof(Collider))]
public class ArenaTrap : MonoBehaviour
{
    [Header("Trap Effect")]
    [SerializeField] private float damage = 10f;
    [SerializeField] private float stunDuration = 1.5f;
    [Tooltip("Seconds before this trap can trigger again (avoids repeated triggers while standing).")]
    [SerializeField] private float cooldownAfterTrigger = 2f;

    [Header("Optional")]
    [SerializeField] private string playerTag = "Player";

    private float cooldownTimer;

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        if (cooldownTimer > 0f) return;

        PlayerHealth health = other.GetComponent<PlayerHealth>();
        PlayerController controller = other.GetComponent<PlayerController>();

        if (health != null)
            health.TakeDamage(damage);
        if (controller != null)
            controller.StunFor(stunDuration);

        cooldownTimer = cooldownAfterTrigger;
    }

    void Update()
    {
        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;
    }
}
