using System.Collections;
using UnityEngine;

/// <summary>
/// Capstone: "Ghost Step" (meta_ghost_step)
/// After a dash, the player is invulnerable for 0.3 s.
/// Internal cooldown of 3 s to prevent rapid-dash abuse.
/// Requires PlayerController to expose a dash event.
/// </summary>
[RequireComponent(typeof(PlayerHealth))]
[RequireComponent(typeof(PlayerController))]
public class GhostStepPassive : MonoBehaviour
{
    [SerializeField] private float iFramesDuration = 0.3f;
    [SerializeField] private float internalCooldown = 3f;

    private PlayerHealth health;
    private PlayerController controller;
    private float cooldownTimer = 0f;

    void Awake()
    {
        health     = GetComponent<PlayerHealth>();
        controller = GetComponent<PlayerController>();
    }

    void Start()
    {
        if (controller != null)
            controller.OnDashEnd += HandleDashEnd;
    }

    void OnDestroy()
    {
        if (controller != null)
            controller.OnDashEnd -= HandleDashEnd;
    }

    void Update()
    {
        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;
    }

    private void HandleDashEnd()
    {
        if (cooldownTimer > 0f) return;
        cooldownTimer = internalCooldown;
        StartCoroutine(GrantIFrames());
    }

    private IEnumerator GrantIFrames()
    {
        health?.ForceIFrames(iFramesDuration);
        yield return null;
    }
}
