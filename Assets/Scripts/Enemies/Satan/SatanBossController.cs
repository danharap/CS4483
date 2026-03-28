using System.Collections;
using UnityEngine;

/// <summary>
/// Main state machine for the Satan boss.
///
/// PHASE 1  – Normal combat. Player depletes HP to zero.
/// FAKE-OUT  – Satan plays a death animation, then RISES off-screen instead of dying.
///             The player should think the fight is over. Phase 2 begins.
/// PHASE 2  – Foot stomp phase managed by SatanFootPhaseController.
///             Feet are the only damageable targets. Damage is routed here via TakeDamagePhase2.
/// FINAL DEATH – Phase 2 HP pool empties → true final defeat.
/// </summary>
[RequireComponent(typeof(SatanAnimationController))]
[RequireComponent(typeof(SatanAttacks))]
public class SatanBossController : MonoBehaviour
{
    // ── Boss States ───────────────────────────────────────────────────────

    public enum BossState
    {
        None,
        Spawning,
        StatueIdle,
        WaitingForFallen,
        Awakening,
        CombatPhase,
        FakeDying,          // HP hit 0 – playing "death" anim before rising
        TransitionToPhase2, // rising off-screen
        FootPhase,          // stomp second phase
        FinalDying,         // true final death
        Dead
    }

    // ── Phase 1 Health ────────────────────────────────────────────────────

    [Header("Phase 1 Health")]
    [SerializeField] public float maxHP = 1500f;

    // ── Phase 2 Health ────────────────────────────────────────────────────

    [Header("Phase 2 Health (Foot Phase)")]
    [Tooltip("Shared HP pool drained by foot hits. Player wins when this hits 0.")]
    [SerializeField] private float phase2MaxHP = 800f;

    // ── Spawn ─────────────────────────────────────────────────────────────

    [Header("Spawn")]
    [SerializeField] private Vector3 spawnOffset = new Vector3(0f, 0f, 16f);

    // ── Fallen Integration ────────────────────────────────────────────────

    [Header("Fallen Integration")]
    [SerializeField] private bool waitForFallenKillBeforeAwakening = false;
    [SerializeField] private float autoAwakenDelay = 1.5f;

    // ── Arena Bounds ──────────────────────────────────────────────────────

    [Header("Arena Bounds")]
    [SerializeField] private float satanArenaTopZ = 14f;
    [SerializeField] private float satanXRange    = 4f;

    // ── Fake-Death Transition ─────────────────────────────────────────────

    [Header("Fake-Death Transition")]
    [Tooltip("Pause after the fake-death anim plays — dramatic beat before Satan rises.")]
    [SerializeField] private float fakeDeathPause   = 1.5f;
    [Tooltip("Time Satan takes to float up and off-screen before foot phase begins.")]
    [SerializeField] private float riseOffScreenTime = 2.5f;

    // ── References ────────────────────────────────────────────────────────

    [Header("References")]
    [SerializeField] private SatanFootPhaseController footPhaseController;
    [SerializeField] private GameObject damageNumberPrefab;

    // ── Runtime State ─────────────────────────────────────────────────────

    public BossState CurrentState  { get; private set; } = BossState.None;
    public float     CurrentHP     { get; private set; }
    public float     Phase2HP      { get; private set; }
    /// <summary>True only while the Phase 1 body is alive and targetable.</summary>
    public bool      IsAlive       { get; private set; } = true;
    /// <summary>True once Phase 2 foot stomp begins.</summary>
    public bool      Phase2Active  { get; private set; }
    public Transform Player        { get; private set; }

    private SatanAnimationController anim;
    private SatanAttacks             attacks;
    private bool                     phase1Ended; // guards against double-trigger

    public event System.Action OnBossDefeated;

    // ─────────────────────────────────────────────────────────────────────
    #region Unity Lifecycle

    private void Awake()
    {
        CurrentHP = maxHP;
        anim      = GetComponent<SatanAnimationController>();
        attacks   = GetComponent<SatanAttacks>();

        int bossLayer = LayerMask.NameToLayer("Boss");
        if (bossLayer >= 0) gameObject.layer = bossLayer;

        // Phase 1 hurtbox – trigger only so it never blocks enemy Rigidbodies.
        foreach (Collider col in GetComponentsInChildren<Collider>(true))
            if (!col.isTrigger) Destroy(col);

        CapsuleCollider hb = gameObject.AddComponent<CapsuleCollider>();
        hb.isTrigger = true;
        hb.radius    = 2.5f;
        hb.height    = 5f;
        hb.center    = new Vector3(0f, 2.5f, 0f);
    }

    private void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) Player = p.transform;

        HUDManager.Instance?.ShowBossHP("SATAN", maxHP, maxHP);
        StartCoroutine(SpawnSequence());
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────
    #region State Machine

    private IEnumerator SpawnSequence()
    {
        SetState(BossState.Spawning);
        yield return null;

        transform.position = new Vector3(spawnOffset.x, spawnOffset.y, satanArenaTopZ);
        anim.SetStatueFrame();
        SetState(BossState.StatueIdle);

        if (waitForFallenKillBeforeAwakening)
            SetState(BossState.WaitingForFallen);
        else
        {
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

    // ─────────────────────────────────────────────────────────────────────

    private IEnumerator FakeDeathTransition()
    {
        if (phase1Ended) yield break;
        phase1Ended = true;

        Debug.Log("[Satan] Phase 1 HP = 0 → Fake-death transition begins.");
        SetState(BossState.FakeDying);
        IsAlive = false; // phase 1 body no longer targetable
        attacks.StopAttacking();

        // Player sees the HP bar empty and a death animation – thinks boss died.
        HUDManager.Instance?.HideBossHP();
        yield return StartCoroutine(anim.PlayDeath());
        yield return new WaitForSeconds(fakeDeathPause);

        // TWIST: Satan rises instead of dying.
        Debug.Log("[Satan] Rising off-screen – Phase 2 begins!");
        SetState(BossState.TransitionToPhase2);
        yield return StartCoroutine(FloatUpward(riseOffScreenTime));

        // Body disappears – foot stomp takes over.
        anim.HideAll();
        Phase2Active = true;
        Phase2HP     = phase2MaxHP;
        SetState(BossState.FootPhase);

        HUDManager.Instance?.ShowBossHP("SATAN", phase2MaxHP, phase2MaxHP);

        if (footPhaseController != null)
            footPhaseController.Begin(this);
        else
            Debug.LogWarning("[Satan] footPhaseController not assigned – foot phase will not start!");
    }

    // ─────────────────────────────────────────────────────────────────────

    private IEnumerator FinalDeathSequence()
    {
        if (CurrentState == BossState.FinalDying || CurrentState == BossState.Dead) yield break;

        Debug.Log("[Satan] FINAL DEFEAT – Phase 2 HP depleted!");
        SetState(BossState.FinalDying);
        Phase2Active = false;

        footPhaseController?.Stop();
        HUDManager.Instance?.HideBossHP();

        yield return new WaitForSeconds(0.8f);

        SetState(BossState.Dead);
        OnBossDefeated?.Invoke();
        GameManager.Instance?.WaveManager?.NotifyBossKilled();

        Destroy(gameObject, 0.5f);
    }

    // ─────────────────────────────────────────────────────────────────────

    private IEnumerator FloatUpward(float duration)
    {
        Vector3 start = transform.position;
        Vector3 end   = start + new Vector3(0f, 16f, 0f);
        float   t     = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            transform.position = Vector3.Lerp(start, end, Mathf.SmoothStep(0f, 1f, t / duration));
            yield return null;
        }
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────
    #region Damage

    /// <summary>Phase 1 damage – only accepted while the body is alive and active.</summary>
    public void TakeDamage(float amount)
    {
        if (!IsAlive || phase1Ended) return;
        if (CurrentState == BossState.StatueIdle   ||
            CurrentState == BossState.WaitingForFallen ||
            CurrentState == BossState.Spawning     ||
            CurrentState == BossState.Awakening) return;

        CurrentHP = Mathf.Max(0f, CurrentHP - amount);
        anim.FlashRed();
        SpawnDamageNumber(amount, transform.position + Vector3.up * 3f);
        HUDManager.Instance?.UpdateBossHP(CurrentHP, maxHP);

        if (CurrentHP <= 0f)
            StartCoroutine(FakeDeathTransition());
    }

    /// <summary>
    /// Phase 2 damage routed here by SatanFootController when a grounded foot is hit.
    /// Reduces the shared Phase 2 HP pool.
    /// </summary>
    public void TakeDamagePhase2(float amount)
    {
        if (!Phase2Active ||
            CurrentState == BossState.FinalDying ||
            CurrentState == BossState.Dead) return;

        Phase2HP = Mathf.Max(0f, Phase2HP - amount);
        SpawnDamageNumber(amount, transform.position + Vector3.up * 3f);
        HUDManager.Instance?.UpdateBossHP(Phase2HP, phase2MaxHP);
        Debug.Log($"[Satan P2] Foot hit! -{amount}  Phase2HP={Phase2HP}/{phase2MaxHP}");

        if (Phase2HP <= 0f)
            StartCoroutine(FinalDeathSequence());
    }

    private void SpawnDamageNumber(float amount, Vector3 pos)
    {
        if (damageNumberPrefab == null) return;
        GameObject go = Object.Instantiate(damageNumberPrefab, pos, Quaternion.identity);
        go.GetComponent<DamageNumber>()?.Initialize(amount, new Color(1f, 0.5f, 0.1f));
    }

    private void OnTriggerEnter(Collider other)
    {
        Projectile proj = other.GetComponent<Projectile>();
        if (proj != null)
            proj.TryHitSatan(this);
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────
    #region Public API

    public void NotifyFallenDefeated()
    {
        if (CurrentState != BossState.WaitingForFallen) return;
        StartCoroutine(AwakenSequence());
    }

    public void SetState(BossState state) => CurrentState = state;

    public bool InCombat => CurrentState == BossState.CombatPhase;

    #endregion
}
