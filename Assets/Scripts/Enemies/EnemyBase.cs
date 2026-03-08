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
            // Fallback: direct movement on XZ plane
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
        StartCoroutine(HitFlash());

        if (CurrentHP <= 0f) Die();
    }

    protected virtual void Die()
    {
        if (!IsAlive) return;
        IsAlive = false;

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
        
        // Play death animation if sprite exists
        SpriteCharacter spriteChar = GetComponent<SpriteCharacter>();
        if (spriteChar != null)
        {
            spriteChar.PlayDeathAnimation();
            Destroy(gameObject, 0.5f); // Delay destruction for death animation
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
        
        // Also flash sprite if it exists
        SpriteCharacter spriteChar = GetComponent<SpriteCharacter>();
        if (spriteChar != null)
            spriteChar.FlashWhite(hitFlashDuration);
        
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
}
