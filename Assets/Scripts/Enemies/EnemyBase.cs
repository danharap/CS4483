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
    private NavMeshAgent agent;
    private Rigidbody rb;
    private Renderer[] renderers;
    private Color[] originalColors;
    private float contactTimer;
    private bool useNavMesh;

    protected virtual void Awake()
    {
        CurrentHP = maxHP;
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (enemyLayer >= 0)
            gameObject.layer = enemyLayer;

        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;

        agent = GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.speed = moveSpeed;
            agent.angularSpeed = 360f;
            agent.acceleration = 20f;
            useNavMesh = agent.isOnNavMesh;
        }

        renderers = GetComponentsInChildren<Renderer>();
        CacheColors();
    }

    protected virtual void OnEnable()
    {
        EnemyRegistry.Register(this);
        if (agent != null) useNavMesh = agent.isOnNavMesh;
    }

    protected virtual void OnDisable() => EnemyRegistry.Unregister(this);

    protected virtual void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;

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
        if (!IsAlive || player == null) return;
        MoveTowardPlayer();
        HandleContactDamage();
    }

    // ── Movement ──────────────────────────────────────────────────────────

    protected virtual void MoveTowardPlayer()
    {
        if (agent != null && agent.isOnNavMesh)
        {
            agent.speed = moveSpeed;
            agent.SetDestination(player.position);
        }
        else
        {
            // Try to re-snap to NavMesh every 60 frames (~1 s) in case the agent slipped off
            if (agent != null && Time.frameCount % 60 == 0)
            {
                NavMeshHit hit;
                if (NavMesh.SamplePosition(transform.position, out hit, 3f, NavMesh.AllAreas))
                    agent.Warp(hit.position);
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
        if (dist < 1.2f)
        {
            player.GetComponent<PlayerHealth>()?.TakeDamage(contactDamage);
            contactTimer = contactCooldown;
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
