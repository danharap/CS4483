using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Abstract base for all enemy types. Handles HP, damage feedback, contact damage,
/// XP drop on death, NavMeshAgent movement with direct-movement fallback.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public abstract class EnemyBase : MonoBehaviour
{
    // ── Tunables ──────────────────────────────────────────────────────────
    [Header("Stats")]
    [SerializeField] public  float maxHP          = 60f;
    [SerializeField] public  float moveSpeed      = 3.5f;
    [SerializeField] private float contactDamage  = 10f;
    [SerializeField] private float contactRange   = 1.2f;
    [SerializeField] private float contactCooldown = 1f;   // seconds between damage ticks
    [SerializeField] public  float xpDrop         = 10f;

    [Header("Hit Feedback")]
    [SerializeField] private float hitFlashDuration = 0.12f;

    [Header("Prefab References")]
    [SerializeField] protected GameObject xpOrbPrefab;
    [SerializeField] protected GameObject healthPackPrefab;
    [SerializeField] [Range(0f, 1f)] private float healthPackDropChance = 0.1f; // 10% chance
    
    [Header("Audio")]
    [SerializeField] private AudioClip deathSound;
    [SerializeField] [Range(0f, 1f)] private float deathVolume = 0.5f;

    [Header("FX")]
    [SerializeField] private GameObject damageNumberPrefab;

    // ── State ─────────────────────────────────────────────────────────────
    public float CurrentHP { get; protected set; }
    public bool  IsAlive   { get; private set; } = true;

    protected Transform player;
    protected PlayerHealth playerHealth;
    private NavMeshAgent agent;
    private Rigidbody rb;
    private Collider selfCollider;
    private Collider playerCollider;
    private Renderer[] renderers;
    private Color[] originalColors;
    private float contactTimer;
    private bool useNavMesh;
    private float dashKnockbackLockTimer;

    protected virtual void Awake()
    {
        CurrentHP = maxHP;
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer >= 0)
            gameObject.layer = enemyLayer;

        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
        rb.drag = 0f;
        rb.angularDrag = 0.05f;
        selfCollider = GetComponent<Collider>();

        agent = GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.speed = moveSpeed;
            agent.angularSpeed = 360f;
            agent.acceleration = 20f;
            // Prevent corner/avoidance braking that looks like "slowing near chests/props".
            agent.autoBraking = false;
            agent.stoppingDistance = 0f;
            useNavMesh = agent.isOnNavMesh;
        }

        renderers = GetComponentsInChildren<Renderer>();
        CacheColors();

        AudioClip deathRes = Resources.Load<AudioClip>("SFX/EnemyDeath");
        if (deathRes != null)
            deathSound = deathRes;
    }

    protected virtual void OnEnable()
    {
        EnemyRegistry.Register(this);
        if (agent != null) useNavMesh = agent.isOnNavMesh;
    }

    protected virtual void OnDisable() => EnemyRegistry.Unregister(this);

    protected virtual void Start()
    {
        TryAcquirePlayer();

        // Snap agent to NavMesh one frame after spawn.
        // The bake is synchronous and finishes before enemies are ever instantiated,
        // but the NavMeshAgent needs one physics tick to register its position.
        if (agent != null)
            StartCoroutine(SnapAgentToNavMesh());
    }

    private IEnumerator SnapAgentToNavMesh()
    {
        yield return null; // wait one fixed-update frame
        if (agent == null || agent.isOnNavMesh) yield break;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(transform.position, out hit, 3f, NavMesh.AllAreas))
        {
            agent.Warp(hit.position);
            Debug.Log($"[EnemyBase] {name} warped to NavMesh at {hit.position}");
        }
        else
        {
            Debug.LogWarning($"[EnemyBase] {name} could not find NavMesh near {transform.position} — falling back to direct movement.");
        }
    }

    protected virtual void Update()
    {
        if (!IsAlive) return;
        if (dashKnockbackLockTimer > 0f)
        {
            dashKnockbackLockTimer -= Time.deltaTime;
            return;
        }
        if (player == null) TryAcquirePlayer();
        if (player == null) return;
        MoveTowardPlayer();
        HandleContactDamage();
    }

    private void TryAcquirePlayer()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p == null) return;
        player = p.transform;
        playerHealth = p.GetComponent<PlayerHealth>();
        playerCollider = p.GetComponent<Collider>();
    }

    // ── Movement ──────────────────────────────────────────────────────────

    protected virtual void MoveTowardPlayer()
    {
        if (agent != null && agent.isOnNavMesh)
        {
            if (rb != null) rb.velocity = Vector3.zero;
            agent.speed = moveSpeed;
            agent.SetDestination(player.position);
        }
        else
        {
            // Dash collisions can push enemies off the NavMesh: resnap quickly so they resume chase.
            if (agent != null && Time.frameCount % 12 == 0)
            {
                NavMeshHit hit;
                if (NavMesh.SamplePosition(transform.position, out hit, 8f, NavMesh.AllAreas))
                {
                    agent.Warp(hit.position);
                    rb.velocity = Vector3.zero;
                }
            }

            // Fallback: direct movement on XZ plane (no wall avoidance, but better than standing still)
            Vector3 dir = (player.position - transform.position);
            dir.y = 0f;
            dir.Normalize();
            rb.velocity = new Vector3(dir.x * moveSpeed, rb.velocity.y, dir.z * moveSpeed);
            if (dir.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.LookRotation(dir);
        }
    }

    // ── Contact Damage ────────────────────────────────────────────────────

    private void HandleContactDamage()
    {
        if (player == null) return;
        contactTimer -= Time.deltaTime;
        if (contactTimer > 0f) return;

        float dist = Vector3.Distance(transform.position, player.position);
        if (selfCollider != null && playerCollider != null)
            dist = Vector3.Distance(selfCollider.ClosestPoint(player.position), playerCollider.ClosestPoint(transform.position));

        if (dist <= contactRange)
        {
            playerHealth?.TakeDamage(contactDamage);
            contactTimer = contactCooldown;
        }
    }

    protected float ContactDamage
    {
        get => contactDamage;
        set => contactDamage = Mathf.Max(0f, value);
    }

    public void ApplyDashKnockback(Vector3 direction, float distance)
    {
        if (!IsAlive || distance <= 0f) return;

        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f) return;
        direction.Normalize();

        dashKnockbackLockTimer = 0.14f;
        contactTimer = Mathf.Max(contactTimer, 0.1f);

        if (agent != null && agent.isOnNavMesh)
        {
            Vector3 desired = transform.position + direction * distance;
            if (NavMesh.SamplePosition(desired, out NavMeshHit hit, Mathf.Max(2f, distance), NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
                if (rb != null) rb.velocity = Vector3.zero;
                return;
            }
        }

        // Fallback when navmesh is unavailable: controlled impulse with velocity cap.
        if (rb != null)
        {
            Vector3 knock = direction * (distance / Mathf.Max(0.01f, dashKnockbackLockTimer));
            knock.y = rb.velocity.y;
            rb.velocity = knock;
        }
    }

    // ── Damage & Death ────────────────────────────────────────────────────

    public virtual void TakeDamage(float amount)
    {
        if (!IsAlive) return;
        CurrentHP -= amount;
        SpawnDamageNumber(amount);
        StartCoroutine(HitFlash());

        if (CurrentHP <= 0f) Die();
    }

    public void ScaleMaxHP(float multiplier)
    {
        if (multiplier <= 0f) return;
        maxHP *= multiplier;
        CurrentHP = maxHP;
    }

    protected virtual void Die()
    {
        if (!IsAlive) return;
        IsAlive = false;

        // Play death sound
        if (deathSound != null)
        {
            AudioSource.PlayClipAtPoint(deathSound, transform.position, deathVolume);
        }

        // Drop XP orb
        if (xpOrbPrefab != null)
            Instantiate(xpOrbPrefab, transform.position + Vector3.up * 0.3f, Quaternion.identity);

        // 10% chance to drop health pack
        if (healthPackPrefab != null && Random.value < healthPackDropChance)
        {
            Instantiate(healthPackPrefab, transform.position + Vector3.up * 0.5f, Quaternion.identity);
            Debug.Log("[Enemy] Dropped health pack!");
        }

        GameManager.Instance?.Logger?.RecordFirstKill();
        GameManager.Instance?.WaveManager?.NotifyEnemyDied(gameObject);
        TutorialManager.Instance?.NotifyTrigger(TutorialTriggerType.KillEnemy);

        EnemyRegistry.Unregister(this);
        
        // Play death animation if sprite component exists
        SpriteCharacter spriteChar = GetComponent<SpriteCharacter>();
        HeavyEnemyVisualController heavyVis = GetComponent<HeavyEnemyVisualController>();
        BatEnemyVisualController batVis = GetComponent<BatEnemyVisualController>();
        BossVisualController bossVis = GetComponent<BossVisualController>();
        if (spriteChar != null)
        {
            spriteChar.PlayDeathAnimation();
            Destroy(gameObject, 0.5f);
        }
        else if (heavyVis != null)
        {
            heavyVis.PlayDeathAnimation();
            Destroy(gameObject, 0.5f);
        }
        else if (batVis != null)
        {
            batVis.PlayDeathAnimation();
            Destroy(gameObject, 0.5f);
        }
        else if (bossVis != null)
        {
            bossVis.PlayDeathAnimation();
            Destroy(gameObject, 0.6f);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // ── Feedback ──────────────────────────────────────────────────────────

    private IEnumerator HitFlash()
    {
        SetRenderColor(Color.white);
        
        // Flash sprite red if it exists
        SpriteCharacter spriteChar = GetComponent<SpriteCharacter>();
        HeavyEnemyVisualController heavyVis = GetComponent<HeavyEnemyVisualController>();
        BatEnemyVisualController batVis = GetComponent<BatEnemyVisualController>();
        BossVisualController bossVis = GetComponent<BossVisualController>();
        if (spriteChar != null)
            spriteChar.FlashRed(hitFlashDuration);
        else if (heavyVis != null)
            heavyVis.FlashRed(hitFlashDuration);
        else if (batVis != null)
            batVis.FlashRed(hitFlashDuration);
        else if (bossVis != null)
            bossVis.FlashRed(hitFlashDuration);
        
        yield return new WaitForSeconds(hitFlashDuration);
        RestoreColors();
    }

    private void CacheColors()
    {
        originalColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
            originalColors[i] = renderers[i].material.color;
    }

    private void SetRenderColor(Color c)
    {
        foreach (var r in renderers) r.material.color = c;
    }

    private void RestoreColors()
    {
        for (int i = 0; i < renderers.Length; i++)
            if (renderers[i] != null) renderers[i].material.color = originalColors[i];
    }

    private void SpawnDamageNumber(float amount)
    {
        if (damageNumberPrefab == null) return;
        GameObject go = Instantiate(
            damageNumberPrefab,
            transform.position + Vector3.up * 1.4f,
            Quaternion.identity);

        DamageNumber dn = go.GetComponent<DamageNumber>();
        if (dn != null)
        {
            // Soft yellow so it stands out on dark floor and red hit flash
            dn.Initialize(amount, new Color(1f, 0.9f, 0.5f));
        }
    }
}
