using UnityEngine;

/// <summary>
/// Handles player movement (WASD), mouse-aim rotation, and dash (Shift).
/// Requires a CharacterController component.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    // ── Tunables ──────────────────────────────────────────────────────────
    [Header("Movement")]
    [SerializeField] public float moveSpeed = 7f;
    [SerializeField] private float gravity = -20f;

    [Header("Dash")]
    [SerializeField] private float dashSpeed = 20f;
    [SerializeField] private float dashDuration = 0.18f;
    [SerializeField] public float dashCooldown = 1.5f;

    // ── State ─────────────────────────────────────────────────────────────
    private CharacterController cc;
    private Vector3 velocity;            // gravity accumulator
    private float dashTimer;
    private float dashCooldownTimer;
    private bool isDashing;
    private Vector3 dashDirection;
    private float stunTimer;             // when > 0, movement and dash are disabled

    // ── Ground detection ──────────────────────────────────────────────────
    private int groundLayer;

    private float baseMoveSpeed;
    private float baseDashCooldown;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        groundLayer = LayerMask.GetMask("Default", "Ground");
        baseMoveSpeed    = moveSpeed;
        baseDashCooldown = dashCooldown;
    }

    /// <summary>Undo all upgrade-applied stat changes, returning movement to serialized defaults.</summary>
    public void ResetToBase()
    {
        moveSpeed    = baseMoveSpeed;
        dashCooldown = baseDashCooldown;
        isDashing    = false;
        stunTimer    = 0f;
        velocity     = Vector3.zero;
    }

    void Update()
    {
        if (stunTimer > 0f) stunTimer -= Time.deltaTime;

        if (GameManager.Instance != null &&
            GameManager.Instance.State != GameManager.GameState.Playing) return;

        HandleMovement();
        HandleAim();
        HandleDash();
    }

    /// <summary>Disables movement and dash for the given duration (e.g. trap stun).</summary>
    public void StunFor(float duration)
    {
        if (duration > stunTimer) stunTimer = duration;
    }

    public bool IsStunned => stunTimer > 0f;

    private void HandleMovement()
    {
        if (stunTimer > 0f)
        {
            // Apply gravity only so player doesn't float
            if (cc.isGrounded && velocity.y < 0f) velocity.y = -2f;
            velocity.y += gravity * Time.deltaTime;
            cc.Move(velocity * Time.deltaTime);
            return;
        }

        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");
        Vector3 move = new Vector3(x, 0f, z).normalized;

        if (!isDashing)
        {
            if (cc.isGrounded && velocity.y < 0f) velocity.y = -2f;
            velocity.y += gravity * Time.deltaTime;

            Vector3 horizontalMove = move * moveSpeed * Time.deltaTime;
            cc.Move(horizontalMove + velocity * Time.deltaTime);
        }
    }

    private void HandleAim()
    {
        // Rotate player to face mouse cursor position (raycasted onto ground plane)
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Plane ground = new Plane(Vector3.up, Vector3.zero);
        if (ground.Raycast(ray, out float dist))
        {
            Vector3 hitPoint = ray.GetPoint(dist);
            Vector3 dir = hitPoint - transform.position;
            dir.y = 0;
            if (dir.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(dir);
        }
    }

    private void HandleDash()
    {
        dashCooldownTimer -= Time.deltaTime;

        if (isDashing)
        {
            dashTimer -= Time.deltaTime;
            cc.Move(dashDirection * dashSpeed * Time.deltaTime);
            if (dashTimer <= 0f) isDashing = false;
            return;
        }

        if (stunTimer > 0f) return;

        if (Input.GetKeyDown(KeyCode.LeftShift) && dashCooldownTimer <= 0f)
        {
            float x = Input.GetAxisRaw("Horizontal");
            float z = Input.GetAxisRaw("Vertical");
            dashDirection = new Vector3(x, 0f, z).normalized;

            if (dashDirection == Vector3.zero)
                dashDirection = transform.forward;

            isDashing = true;
            dashTimer = dashDuration;
            dashCooldownTimer = dashCooldown;
        }
    }

    public bool IsDashing => isDashing;

    /// <summary>Small vertical pop (used by boss slam).</summary>
    public void LaunchUp(float upwardVelocity)
    {
        if (upwardVelocity <= 0f) return;
        if (velocity.y < upwardVelocity) velocity.y = upwardVelocity;
    }
}
