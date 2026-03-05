using TMPro;
using UnityEngine;

/// <summary>
/// Healing shrine (green zone). Press E when nearby to heal +20 HP with 30s cooldown.
/// Requires a Trigger Collider on this GameObject.
/// </summary>
public class HealingShrine : MonoBehaviour
{
    // ── Tunables ──────────────────────────────────────────────────────────
    [Header("Shrine Settings")]
    [SerializeField] private float healAmount  = 20f;
    [SerializeField] private float cooldown    = 30f;

    [Header("UI Prompt")]
    [SerializeField] private GameObject promptObject; // world-space "Press E" text
    [SerializeField] private TMP_Text   cooldownText;

    // ── State ─────────────────────────────────────────────────────────────
    private float cooldownTimer;
    private bool  playerInRange;
    private PlayerHealth playerHealth;

    void Awake()
    {
        if (promptObject) promptObject.SetActive(false);
    }

    void Update()
    {
        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;
            if (cooldownText) cooldownText.text = $"Cooldown: {Mathf.CeilToInt(cooldownTimer)}s";
        }
        else
        {
            if (cooldownText) cooldownText.text = "Press E to Heal";
        }

        if (playerInRange && Input.GetKeyDown(KeyCode.E) && cooldownTimer <= 0f)
            Activate();
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerHealth = other.GetComponent<PlayerHealth>();
        playerInRange = true;
        if (promptObject) promptObject.SetActive(true);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = false;
        playerHealth  = null;
        if (promptObject) promptObject.SetActive(false);
    }

    private void Activate()
    {
        playerHealth?.HealHP(healAmount);
        cooldownTimer = cooldown;
        Debug.Log($"[HealingShrine] Healed player for {healAmount}");
    }
}
