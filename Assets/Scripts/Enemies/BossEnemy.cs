using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Boss enemy: very high HP, occasionally performs a jump-slam attack.
/// Spawns every 5 waves. On death, notifies WaveManager.OnBossKilled.
/// </summary>
public class BossEnemy : EnemyBase
{
    [Header("Jump Slam")]
    [SerializeField] private float attackCooldown = 5.5f;
    [SerializeField] private float chargeTime = 0.65f;
    [SerializeField] private float jumpSpeed = 10f;
    [SerializeField] private float jumpHeight = 2.8f;
    [SerializeField] private float slamRadius = 4.5f;
    [SerializeField] private float slamDamage = 25f;
    [SerializeField] private float airContactDamage = 15f;
    [SerializeField] private float minAttackDistance = 6f;
    [SerializeField] private float slamKnockUpVelocity = 7.5f;
    [SerializeField] private float slamZoomOut = 1.25f;

    [Header("Telegraph")]
    [SerializeField] private float telegraphLineWidth = 0.08f;
    [SerializeField] private float impactFxDuration = 0.25f;

    private float attackTimer;
    private bool isAttacking;
    private Vector3 slamTarget;
    private BossVisualController vis;
    // NOTE: EnemyBase already has private fields named agent/rb. Use unique names here to avoid Unity serialization conflicts.
    private UnityEngine.AI.NavMeshAgent bossAgent;
    private Rigidbody bossRb;

    private LineRenderer telegraph;
    private LineRenderer impactFx;
    private float airHitCooldownTimer;
    private Transform visualRoot;
    private CameraController cam;

    protected override void Awake()
    {
        base.Awake();
        if (maxHP <= 0f)     maxHP     = 500f;
        if (moveSpeed <= 0f) moveSpeed = 2.5f;
        if (xpDrop <= 0f)    xpDrop    = 100f;
        attackTimer = attackCooldown * 0.5f;
        vis = GetComponent<BossVisualController>();
        bossAgent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        bossRb = GetComponent<Rigidbody>();
        visualRoot = transform.Find("VisualRoot");
        cam = Camera.main != null ? Camera.main.GetComponent<CameraController>() : null;

        // Boss should pass through obstacle boxes at all times (unstoppable).
        // EnemyBase contact damage is distance-based, and projectiles use overlap, so gameplay still works.
        CapsuleCollider col = GetComponent<CapsuleCollider>();
        if (col != null) col.isTrigger = true;
        if (bossRb != null)
        {
            // Keep Rigidbody configured like other enemies (EnemyBase handles gravity/constraints).
            // Use EnemyBase movement/pathing to avoid "tied to player movement" bug.
            bossRb.detectCollisions = true;
            bossRb.isKinematic = false;
            bossRb.velocity = Vector3.zero;
            bossRb.angularVelocity = Vector3.zero;
        }

        // Ensure boss uses NavMeshAgent movement like other enemies.
        // If spawned slightly off-mesh, warp it onto the nearest NavMesh point.
        if (bossAgent != null && !bossAgent.isOnNavMesh)
        {
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 5f, NavMesh.AllAreas))
                bossAgent.Warp(hit.position);
        }
    }

    protected override void Update()
    {
        if (!IsAlive || player == null) return;

        if (!isAttacking)
        {
            base.Update(); // normal move + contact damage
            attackTimer -= Time.deltaTime;

            if (attackTimer <= 0f)
            {
                float dist = Vector3.Distance(transform.position, player.position);
                if (dist <= minAttackDistance)
                    StartCoroutine(SlamRoutine());
            }
        }
    }

    private IEnumerator SlamRoutine()
    {
        isAttacking = true;
        attackTimer = attackCooldown;
        airHitCooldownTimer = 0f;

        if (bossAgent != null) bossAgent.enabled = false;
        if (bossRb != null) bossRb.velocity = Vector3.zero;

        // Collisions are already disabled in Awake so we can always pass through boxes.

        if (vis == null) vis = GetComponent<BossVisualController>();
        vis?.SetAttacking(true);
        vis?.SetBodyAttackFrame(0); // charge/telegraph
        cam?.SetZoom(slamZoomOut);

        // Lock the landing target at start of charge so player can dodge.
        slamTarget = player.position;
        slamTarget.y = transform.position.y;

        EnsureTelegraph();
        ShowCircle(telegraph, slamTarget, slamRadius, 1f);

        float t = 0f;
        while (t < chargeTime)
        {
            if (!IsAlive) { CleanupTelegraphs(); isAttacking = false; yield break; }
            t += Time.deltaTime;
            // Keep indicator visible (fixed target)
            ShowCircle(telegraph, slamTarget, slamRadius, 1f);
            yield return null;
        }

        // Jump / arc to target
        Vector3 start = transform.position;
        Vector3 end = new Vector3(slamTarget.x, start.y, slamTarget.z);
        float dist = Vector3.Distance(new Vector3(start.x, 0f, start.z), new Vector3(end.x, 0f, end.z));
        float duration = Mathf.Max(0.25f, dist / jumpSpeed);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (!IsAlive) { CleanupTelegraphs(); isAttacking = false; yield break; }
            elapsed += Time.deltaTime;
            float u = Mathf.Clamp01(elapsed / duration);

            // Arc height
            float h = 4f * u * (1f - u) * jumpHeight;
            Vector3 groundPos = Vector3.Lerp(start, end, SmoothStep01(u));
            transform.position = groundPos; // keep collider on ground plane
            if (visualRoot != null)
                visualRoot.localPosition = new Vector3(0f, h, 0f); // lift visuals only

            // Mid-air collision damage (cooldowned)
            airHitCooldownTimer -= Time.deltaTime;
            if (airHitCooldownTimer <= 0f)
            {
                float pd = Vector3.Distance(new Vector3(groundPos.x, 0f, groundPos.z), new Vector3(player.position.x, 0f, player.position.z));
                if (pd < 1.3f)
                {
                    player.GetComponent<PlayerHealth>()?.TakeDamage(airContactDamage);
                    airHitCooldownTimer = 0.5f;
                }
            }

            // Descent frame in second half
            if (u >= 0.55f) vis?.SetBodyAttackFrame(1);
            if (u >= 0.55f) cam?.ResetZoom();

            // Keep telegraph under landing point
            ShowCircle(telegraph, slamTarget, slamRadius, 1f);
            yield return null;
        }

        // Impact
        transform.position = end;
        if (visualRoot != null) visualRoot.localPosition = Vector3.zero;
        vis?.SetBodyAttackFrame(2);

        // Impact FX: expanding ring
        EnsureImpactFx();
        StartCoroutine(ImpactFxRoutine(slamTarget, slamRadius));

        // Deal AOE damage
        float pdImpact = Vector3.Distance(new Vector3(player.position.x, 0f, player.position.z), new Vector3(slamTarget.x, 0f, slamTarget.z));
        if (pdImpact <= slamRadius)
        {
            player.GetComponent<PlayerHealth>()?.TakeDamage(slamDamage);
            player.GetComponent<PlayerController>()?.LaunchUp(slamKnockUpVelocity);
        }

        // brief recovery
        yield return new WaitForSeconds(0.25f);

        CleanupTelegraphs();
        vis?.SetAttacking(false);
        isAttacking = false;
        if (bossAgent != null) bossAgent.enabled = true;
        cam?.ResetZoom();

        // Leave collisions disabled (boss always ignores boxes).
    }

    private static float SmoothStep01(float t) => t * t * (3f - 2f * t);

    private IEnumerator ImpactFxRoutine(Vector3 center, float radius)
    {
        float t = 0f;
        while (t < impactFxDuration)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / impactFxDuration);
            float r = Mathf.Lerp(radius * 0.2f, radius, u);
            ShowCircle(impactFx, center, r, 1f - u);
            yield return null;
        }
        if (impactFx != null) impactFx.enabled = false;
    }

    private void EnsureTelegraph()
    {
        if (telegraph != null) return;
        telegraph = CreateCircleRenderer("SlamTelegraph", new Color(1f, 0.35f, 0.15f, 0.85f));
    }

    private void EnsureImpactFx()
    {
        if (impactFx != null) return;
        impactFx = CreateCircleRenderer("SlamImpactFx", new Color(1f, 1f, 1f, 0.9f));
        impactFx.enabled = false;
    }

    private LineRenderer CreateCircleRenderer(string name, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform, false);
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.loop = true;
        lr.positionCount = 48;
        lr.startWidth = telegraphLineWidth;
        lr.endWidth = telegraphLineWidth;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = color;
        lr.endColor = color;
        lr.sortingOrder = 30;
        return lr;
    }

    private void ShowCircle(LineRenderer lr, Vector3 center, float radius, float alpha)
    {
        if (lr == null) return;
        lr.enabled = true;
        Color c0 = lr.startColor; c0.a = alpha;
        lr.startColor = c0;
        lr.endColor = c0;

        float y = 0.05f;
        for (int i = 0; i < lr.positionCount; i++)
        {
            float a = (i / (float)lr.positionCount) * Mathf.PI * 2f;
            lr.SetPosition(i, new Vector3(center.x + Mathf.Cos(a) * radius, y, center.z + Mathf.Sin(a) * radius));
        }
    }

    private void CleanupTelegraphs()
    {
        if (telegraph != null) telegraph.enabled = false;
        if (impactFx != null) impactFx.enabled = false;
    }

    protected override void Die()
    {
        base.Die(); // handles XP drop, registry, destroy

        // Signal the WaveManager that the boss is dead
        GameManager.Instance?.WaveManager?.NotifyBossKilled();
    }
}
