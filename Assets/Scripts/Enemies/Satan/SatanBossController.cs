using System.Collections;
using UnityEngine;

/// <summary>
/// Main state machine for the Satan boss.
/// Controls state transitions, phase logic, health thresholds, and attack sequencing.
/// Attach to the Satan prefab root alongside SatanAnimationController and SatanAttacks.
/// </summary>
[RequireComponent(typeof(SatanAnimationController))]
[RequireComponent(typeof(SatanAttacks))]
public class SatanBossController : MonoBehaviour
{
    // ── Boss States ───────────────────────────────────────────────────────

    public enum BossState
    {
        None,
        Spawning,         // just instantiated, first frame
        StatueIdle,       // frozen on frame 0 of Awakening sprite
        WaitingForFallen, // same visual, waiting for Fallen death signal
        Awakening,        // playing the rest of the Awakening animation
        CombatPhase,      // main attack loop
        TransitionToFootPhase,
        FootPhase,        // stomp-only second phase
        Dying,
        Dead
    }

    // ── Serialized Fields ─────────────────────────────────────────────────

    [Header("Health")]
    [SerializeField] public float maxHP = 1500f;
    [SerializeField] [Range(0f, 1f)]
    private float footPhaseHPThreshold = 0.35f; // transition at 35% HP

    [Header("Spawn")]
    [Tooltip("World position where Satan spawns. Override per scene if needed.")]
    [SerializeField] private Vector3 spawnOffset = new Vector3(0f, 0f, 16f); // top of arena

    [Header("Fallen Integration")]
    [Tooltip("If true, Satan waits on frame 0 until NotifyFallenDefeated() is called. " +
             "Set false to test the full fight immediately.")]
    [SerializeField] private bool waitForFallenKillBeforeAwakening = false;
    [Tooltip("Time before auto-awakening when waitForFallenKillBeforeAwakening = false.")]
    [SerializeField] private float autoAwakenDelay = 1.5f;

    [Header("Arena Bounds (for positioning)")]
    [Tooltip("Satan stays near the top of the arena. Controls X wander range.")]
    [SerializeField] private float satanArenaTopZ = 14f;
    [SerializeField] private float satanXRange = 4f;

    [Header("References")]
    [SerializeField] private SatanFootPhase footPhase;
    [SerializeField] private GameObject damageNumberPrefab;

    // ── Runtime State ─────────────────────────────────────────────────────

    public BossState CurrentState { get; private set; } = BossState.None;
    public float CurrentHP        { get; private set; }
    public bool  IsAlive          { get; private set; } = true;
    public Transform Player       { get; private set; }

    private SatanAnimationController anim;
    private SatanAttacks attacks;
    private CameraController cam;
    private bool deathTriggered;

    // ── Events (for future wiring) ────────────────────────────────────────

    public event System.Action OnBossDefeated;

    // ─────────────────────────────────────────────────────────────────────
    #region Unity Lifecycle

    private void Awake()
    {
        CurrentHP = maxHP;
        anim      = GetComponent<SatanAnimationController>();
        attacks   = GetComponent<SatanAttacks>();
        cam       = Camera.main != null ? Camera.main.GetComponent<CameraController>() : null;

        // ── Collider setup ────────────────────────────────────────────────
        // Remove any large non-trigger colliders that would block enemy pathfinding.
        foreach (Collider col in GetComponents<Collider>())
        {
            if (!col.isTrigger)
                Destroy(col);
        }
        // Add a reasonably-sized trigger for player bullet detection (see OnTriggerEnter).
        CapsuleCollider hitbox = gameObject.AddComponent<CapsuleCollider>();
        hitbox.isTrigger = true;
        hitbox.radius    = 2.5f;
        hitbox.height    = 5f;
        hitbox.center    = new Vector3(0f, 2.5f, 0f);
    }

    private void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) Player = p.transform;

        // Show boss HP bar immediately (bar hides itself until boss awakens)
        HUDManager.Instance?.ShowBossHP("SATAN", maxHP, maxHP);

        StartCoroutine(SpawnSequence());
    }

    private void Update()
    {
        if (CurrentState == BossState.CombatPhase)
        {
            CheckPhaseTransition();
        }
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────
    #region State Machine

    private IEnumerator SpawnSequence()
    {
        SetState(BossState.Spawning);
        yield return null; // let Awake/Start finish

        // Position Satan at top of arena
        Vector3 spawnPos = new Vector3(spawnOffset.x, spawnOffset.y, satanArenaTopZ);
        transform.position = spawnPos;

        // Show frame 0 of Awakening — frozen statue
        anim.SetStatueFrame();
        SetState(BossState.StatueIdle);

        if (waitForFallenKillBeforeAwakening)
        {
            // Remain frozen until NotifyFallenDefeated() is called externally
            SetState(BossState.WaitingForFallen);
        }
        else
        {
            // Auto-awaken after delay (testing mode)
            yield return new WaitForSeconds(autoAwakenDelay);
            StartCoroutine(AwakenSequence());
        }
    }

    private IEnumerator AwakenSequence()
    {
        SetState(BossState.Awakening);
        yield return StartCoroutine(anim.PlayAwakening());
        EnterCombat();
    }

    private void EnterCombat()
    {
        SetState(BossState.CombatPhase);
        attacks.enabled = true;
        StartCoroutine(attacks.CombatLoop(this));
    }

    private void CheckPhaseTransition()
    {
        if (!IsAlive) return;
        if (CurrentHP / maxHP <= footPhaseHPThreshold)
            StartCoroutine(TransitionToFootPhase());
    }

    private IEnumerator TransitionToFootPhase()
    {
        // Only trigger once
        if (CurrentState == BossState.TransitionToFootPhase ||
            CurrentState == BossState.FootPhase ||
            CurrentState == BossState.Dying ||
            CurrentState == BossState.Dead) yield break;

        SetState(BossState.TransitionToFootPhase);
        attacks.StopAttacking();

        // Brief dramatic pause, then move Satan off-screen upward
        yield return new WaitForSeconds(0.5f);
        yield return StartCoroutine(FloatUpward(2.5f));

        // Hide Satan body — foot phase takes over
        anim.HideAll();
        SetState(BossState.FootPhase);

        if (footPhase != null)
            footPhase.Begin(this);
        else
            Debug.LogWarning("[Satan] footPhase reference not assigned on SatanBossController.");
    }

    private IEnumerator FloatUpward(float duration)
    {
        Vector3 start = transform.position;
        Vector3 end   = start + new Vector3(0f, 12f, 0f);
        float   t     = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            transform.position = Vector3.Lerp(start, end, t / duration);
            yield return null;
        }
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────
    #region Damage & Death

    public void TakeDamage(float amount)
    {
        if (!IsAlive || deathTriggered) return;
        if (CurrentState == BossState.StatueIdle ||
            CurrentState == BossState.WaitingForFallen ||
            CurrentState == BossState.Spawning ||
            CurrentState == BossState.Awakening) return; // invincible until active

        CurrentHP = Mathf.Max(0f, CurrentHP - amount);
        anim.FlashRed();
        SpawnDamageNumber(amount);
        HUDManager.Instance?.UpdateBossHP(CurrentHP, maxHP);

        if (CurrentHP <= 0f) StartCoroutine(DeathSequence());
    }

    private void SpawnDamageNumber(float amount)
    {
        if (damageNumberPrefab == null) return;
        Vector3 pos = transform.position + Vector3.up * 3f;
        GameObject go = Object.Instantiate(damageNumberPrefab, pos, Quaternion.identity);
        go.GetComponent<DamageNumber>()?.Initialize(amount, new Color(1f, 0.5f, 0.1f));
    }

    private void OnTriggerEnter(Collider other)
    {
        // Player projectiles use Projectile.cs overlap-sphere which now handles Satan directly.
        // This OnTriggerEnter is a secondary catch for any projectile that slips through.
        Projectile proj = other.GetComponent<Projectile>();
        if (proj != null)
            proj.TryHitSatan(this);
    }

    private IEnumerator DeathSequence()
    {
        if (deathTriggered) yield break;
        deathTriggered = true;
        IsAlive        = false;

        SetState(BossState.Dying);
        attacks.StopAttacking();

        if (footPhase != null) footPhase.Stop();
        HUDManager.Instance?.HideBossHP();

        yield return StartCoroutine(anim.PlayDeath());

        // Float upward and despawn
        yield return StartCoroutine(FloatUpward(1.8f));

        SetState(BossState.Dead);
        OnBossDefeated?.Invoke();
        GameManager.Instance?.WaveManager?.NotifyBossKilled();

        Destroy(gameObject, 0.1f);
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────
    #region Public API

    /// <summary>
    /// Call this when The Fallen enemy is defeated to trigger Satan's awakening.
    /// Wire from FallenEnemy.Die() in the future.
    /// </summary>
    public void NotifyFallenDefeated()
    {
        if (CurrentState != BossState.WaitingForFallen) return;
        StartCoroutine(AwakenSequence());
    }

    public void SetState(BossState state)
    {
        CurrentState = state;
    }

    public bool InCombat => CurrentState == BossState.CombatPhase;

    #endregion
}
