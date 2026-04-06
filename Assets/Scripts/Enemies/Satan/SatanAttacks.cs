using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// All combat attacks for Satan. Runs the main combat loop and dispatches individual
/// attack coroutines. Designed to be readable and independently tunable per attack.
///
/// Attacks:
///   1. Direct Attack   – aimed fan spread at the player
///   2. Down Beam       – vertical beam straight downward, punishes standing under Satan
///   3. Fan Attack      – radial burst of projectiles in all directions
///   4. Dual Hand Beam  – two beams closing inward with a safe center gap
///
/// Attach to the same GameObject as SatanBossController.
/// </summary>
[RequireComponent(typeof(SatanBossController))]
[RequireComponent(typeof(SatanAnimationController))]
public class SatanAttacks : MonoBehaviour
{
    // ─────────────────────────────────────────────────────────────────────
    #region Serialized Tunables

    [Header("Combat Pacing")]
    [Tooltip("Idle time between attacks (wait AFTER linger, BEFORE hop into next attack).")]
    [SerializeField] private float timeBetweenAttacks = 1.2f;
    [Tooltip("Initial delay before Satan's first attack after entering combat.")]
    [SerializeField] private float initialCombatDelay = 2.0f;
    [Tooltip("Minimum gap between using the same attack twice in a row.")]
    [SerializeField] private int   antiRepeatWindow   = 2;

    [Header("Post-Attack Last-Frame Linger")]
    [Tooltip("How long to hold the last attack frame before returning to idle. " +
             "Laser attacks should be longer; burst attacks can be short.")]
    [SerializeField] private float lingerDirect      = 0.35f;
    [SerializeField] private float lingerDownBeam    = 0.5f;  // on top of beam duration
    [SerializeField] private float lingerFan         = 0.35f;
    [SerializeField] private float lingerDualBeam    = 0.5f;  // on top of beam duration

    [Header("Mini Hop (pre-attack repositioning)")]
    [SerializeField] private float hopDistance  = 3.5f;
    [SerializeField] private float hopDuration  = 0.35f;
    [SerializeField] private float hopArcHeight = 1.2f;
    [Tooltip("Probability to hop toward the player instead of random side.")]
    [SerializeField] [Range(0f, 1f)] private float hopBiasTowardPlayer = 0.5f;

    // ── Direct Attack ─────────────────────────────────────────────────────

    [Header("Direct Attack")]
    [SerializeField] private GameObject bulletPrefab;
    [Tooltip("Bullet sprite from Assets/Sprites/Final Boss/Bullets.png — wired by SpriteSetup.")]
    [SerializeField] private Sprite     bulletSprite;
    [Tooltip("Raise spawn above the arena floor so the trigger does not overlap solid ground on frame 0.")]
    [SerializeField] private float bulletSpawnYOffset = 0.65f;
    [SerializeField] private float      directBulletSpeed  = 10f;
    [SerializeField] private int        directBulletCount  = 5;
    [SerializeField] private float      directSpreadAngle  = 30f;  // total cone angle
    [SerializeField] private float      directDamage       = 15f;
    [SerializeField] private float      directDelayBeforeFire = 0.3f;
    [SerializeField] private int        directVolleys      = 1;
    [SerializeField] private float      directVolleyDelay  = 0.4f;
    [SerializeField] private float      directCooldown     = 5f;

    // ── Down Beam ─────────────────────────────────────────────────────────

    [Header("Down Beam Attack")]
    [SerializeField] private GameObject downBeamPrefab;   // thin LineRenderer or quad beam prefab
    [SerializeField] private float      downBeamChargeDuration = 0.5f;
    [SerializeField] private float      downBeamDuration       = 1.5f;
    [SerializeField] private float      downBeamWidth          = 1.2f;
    [SerializeField] private float      downBeamDamagePerTick  = 12f;
    [SerializeField] private float      downBeamTickRate        = 0.25f;
    [SerializeField] private float      downBeamCooldown       = 7f;

    // ── Fan Attack ────────────────────────────────────────────────────────

    [Header("Fan Attack (Radial Burst)")]
    [SerializeField] private float fanBulletSpeed    = 9f;
    [SerializeField] private int   fanBulletCount    = 16;  // bullets per wave
    [SerializeField] private int   fanWaves           = 2;
    [SerializeField] private float fanWaveDelay       = 0.35f;
    [SerializeField] private float fanAngleOffset     = 0f;
    [SerializeField] private float fanDamage          = 12f;
    [SerializeField] private float fanCooldown        = 8f;

    // ── Dual Hand Beam ────────────────────────────────────────────────────

    [Header("Dual Hand Beam")]
    [SerializeField] private GameObject handBeamPrefab;
    [SerializeField] private float      dualChargeDuration  = 0.6f;
    [SerializeField] private float      dualBeamDuration    = 2.5f;
    [SerializeField] private float      dualInwardSpeed     = 2.5f;
    [SerializeField] private float      dualMinCenterGap    = 2.5f; // safe gap between beams
    [SerializeField] private float      dualBeamWidth       = 1.5f; // actual damaging width of each beam
    [SerializeField] private float      dualBeamDamagePerTick = 10f;
    [SerializeField] private float      dualBeamTickRate     = 0.2f;
    [SerializeField] private float      dualBeamStartX       = 8f;  // initial X offset from Satan's center
    [SerializeField] private float      dualCooldown         = 9f;

    [Tooltip("Height of Satan's hands relative to Satan's Y. Beams visually originate here.")]
    [SerializeField] private float      dualHandOffsetY      = 1.5f;
    [Tooltip("Z offset of hands in front of Satan's body (positive = toward player).")]
    [SerializeField] private float      dualHandOffsetZ      = 1.0f;
    [Tooltip("How far the beam covers in the -Z (toward player) direction from the hand.")]
    [SerializeField] private float      dualBeamLength       = 36f;
    [SerializeField] private Color      dualBeamColor        = new Color(1f, 0.35f, 0f, 0.85f);

    #endregion

    // ─────────────────────────────────────────────────────────────────────
    #region State

    private SatanBossController boss;
    private SatanAnimationController anim;
    private Transform player;

    private bool running;
    private Coroutine combatCoroutine;

    // Cooldown tracking
    private Dictionary<int, float> cooldownTimers = new Dictionary<int, float>
    {
        {0, 0f}, {1, 0f}, {2, 0f}, {3, 0f}
    };

    private Queue<int> recentAttacks = new Queue<int>();

    // Active beam objects — tracked so we can forcibly destroy them if the phase ends mid-attack.
    private readonly List<GameObject> activeBeams = new List<GameObject>();

    #endregion

    // ─────────────────────────────────────────────────────────────────────
    #region Unity Lifecycle

    private void Awake()
    {
        boss = GetComponent<SatanBossController>();
        anim = GetComponent<SatanAnimationController>();
        enabled = false; // disabled until EnterCombat is called
    }

    private void Update()
    {
        // Tick cooldown timers
        foreach (int k in new List<int>(cooldownTimers.Keys))
            cooldownTimers[k] = Mathf.Max(0f, cooldownTimers[k] - Time.deltaTime);
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────
    #region Combat Loop

    /// <summary>Called by SatanBossController when entering combat phase.</summary>
    public IEnumerator CombatLoop(SatanBossController controller)
    {
        player  = controller.Player;
        running = true;

        anim.PlayIdleLoop();
        yield return new WaitForSeconds(initialCombatDelay);

        while (running && boss.InCombat)
        {
            if (!running || !boss.InCombat) break;

            int choice = ChooseAttack();
            if (choice < 0)
            {
                // All on cooldown – idle briefly
                yield return new WaitForSeconds(0.5f);
                continue;
            }

            TrackRecent(choice);

            switch (choice)
            {
                case 0: yield return StartCoroutine(DirectAttack());    break;
                case 1: yield return StartCoroutine(DownBeamAttack());  break;
                case 2: yield return StartCoroutine(FanAttack());       break;
                case 3: yield return StartCoroutine(DualHandBeam());    break;
            }

            // Hold the last attack frame for a tunable duration, then go idle.
            float linger = GetAttackLinger(choice);
            yield return StartCoroutine(anim.HoldLastFrame(linger));
            anim.PlayIdleLoop();

            // Short idle gap before the next attack hop.
            yield return new WaitForSeconds(timeBetweenAttacks);
        }
    }

    private float GetAttackLinger(int attackIndex)
    {
        switch (attackIndex)
        {
            case 0: return lingerDirect;
            case 1: return lingerDownBeam;
            case 2: return lingerFan;
            case 3: return lingerDualBeam;
            default: return 0.3f;
        }
    }

    public void StopAttacking()
    {
        running = false;
        StopAllCoroutines();
        DestroyAllActiveBeams();
    }

    private void RegisterBeam(GameObject beam)
    {
        if (beam != null) activeBeams.Add(beam);
    }

    private void UnregisterBeam(GameObject beam)
    {
        activeBeams.Remove(beam);
    }

    private void DestroyAllActiveBeams()
    {
        foreach (GameObject beam in activeBeams)
        {
            if (beam != null) Destroy(beam);
        }
        activeBeams.Clear();
    }

    /// <summary>
    /// Weighted random attack selection, respecting cooldowns and anti-repeat window.
    /// </summary>
    private int ChooseAttack()
    {
        // Weights: Direct=35%, DownBeam=20%, Fan=25%, DualHand=20%
        float[] weights = { 35f, 20f, 25f, 20f };
        float[] cd      = { directCooldown, downBeamCooldown, fanCooldown, dualCooldown };

        var available = new List<(int idx, float weight)>();
        for (int i = 0; i < 4; i++)
        {
            if (cooldownTimers[i] > 0f) continue;
            if (WasRecentlyUsed(i))     continue;
            available.Add((i, weights[i]));
        }

        if (available.Count == 0) return -1;

        float total  = 0f;
        foreach (var (_, w) in available) total += w;

        float roll = Random.Range(0f, total);
        float acc  = 0f;
        foreach (var (idx, w) in available)
        {
            acc += w;
            if (roll <= acc)
            {
                cooldownTimers[idx] = cd[idx];
                return idx;
            }
        }
        return available[available.Count - 1].idx;
    }

    private void TrackRecent(int idx)
    {
        recentAttacks.Enqueue(idx);
        while (recentAttacks.Count > antiRepeatWindow)
            recentAttacks.Dequeue();
    }

    private bool WasRecentlyUsed(int idx)
    {
        int count = 0;
        foreach (int r in recentAttacks)
            if (r == idx) count++;
        // Block if used in ALL recent slots
        return count >= antiRepeatWindow;
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────
    #region Attack: Direct (aimed fan)

    private IEnumerator DirectAttack()
    {
        yield return StartCoroutine(PerformHop(true));
        yield return StartCoroutine(anim.PlayAttackAnim(AttackAnimType.Direct));
        yield return new WaitForSeconds(directDelayBeforeFire);

        if (player == null) yield break;

        Vector3 origin = transform.position;
        Vector3 toPlayer = (player.position - origin);
        toPlayer.y = 0f;
        if (toPlayer.sqrMagnitude < 0.01f) toPlayer = Vector3.forward;
        toPlayer.Normalize();

        for (int v = 0; v < directVolleys; v++)
        {
            SpawnBulletFan(origin, toPlayer, directBulletCount, directSpreadAngle, directBulletSpeed, directDamage);
            if (v < directVolleys - 1)
                yield return new WaitForSeconds(directVolleyDelay);
        }
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────
    #region Attack: Down Beam (vertical lane)

    private IEnumerator DownBeamAttack()
    {
        yield return StartCoroutine(PerformHop(false));
        yield return StartCoroutine(anim.PlayAttackAnim(AttackAnimType.DownBeam));

        // Charge-up telegraph
        yield return new WaitForSeconds(downBeamChargeDuration);

        // Spawn or activate beam going downward (-Z in top-down)
        GameObject beam = SpawnBeam(transform.position, Vector3.back, downBeamWidth, downBeamPrefab);
        RegisterBeam(beam);

        // Damage tick loop
        float elapsed = 0f;
        float tickTimer = 0f;
        while (elapsed < downBeamDuration)
        {
            elapsed   += Time.deltaTime;
            tickTimer += Time.deltaTime;
            if (tickTimer >= downBeamTickRate)
            {
                tickTimer = 0f;
                DamagePlayerInBeam(transform.position, Vector3.back, downBeamWidth, downBeamDamagePerTick);
            }
            yield return null;
        }

        UnregisterBeam(beam);
        if (beam != null) Destroy(beam);
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────
    #region Attack: Fan (radial burst)

    private IEnumerator FanAttack()
    {
        yield return StartCoroutine(PerformHop(Random.value < 0.5f));
        yield return StartCoroutine(anim.PlayAttackAnim(AttackAnimType.Fan));

        Vector3 origin = transform.position;

        for (int w = 0; w < fanWaves; w++)
        {
            SpawnRadialBurst(origin, fanBulletCount, fanAngleOffset + w * (360f / fanBulletCount / fanWaves), fanBulletSpeed, fanDamage);
            if (w < fanWaves - 1)
                yield return new WaitForSeconds(fanWaveDelay);
        }
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────
    #region Attack: Dual Hand Beam (closing beams)

    private IEnumerator DualHandBeam()
    {
        yield return StartCoroutine(PerformHop(false));
        yield return StartCoroutine(anim.PlayAttackAnim(AttackAnimType.DualHandBeam));

        yield return new WaitForSeconds(dualChargeDuration);

        // Hand positions: slightly elevated and in front of Satan
        float cx      = transform.position.x;
        float handY   = transform.position.y + dualHandOffsetY;
        float handZ   = transform.position.z - dualHandOffsetZ; // negative Z = toward player

        float leftX  = cx - dualBeamStartX;
        float rightX = cx + dualBeamStartX;

        // Spawn beams anchored at each hand, extending toward the player (-Z)
        GameObject leftBeam  = SpawnDualBeamVisual(new Vector3(leftX,  handY, handZ));
        GameObject rightBeam = SpawnDualBeamVisual(new Vector3(rightX, handY, handZ));
        RegisterBeam(leftBeam);
        RegisterBeam(rightBeam);

        float elapsed   = 0f;
        float tickTimer = 0f;

        while (elapsed < dualBeamDuration)
        {
            elapsed   += Time.deltaTime;
            tickTimer += Time.deltaTime;

            // Move beams inward until they reach the center gap
            float halfGap = dualMinCenterGap * 0.5f;
            leftX  = Mathf.Min(leftX  + dualInwardSpeed * Time.deltaTime, cx - halfGap);
            rightX = Mathf.Max(rightX - dualInwardSpeed * Time.deltaTime, cx + halfGap);

            if (leftBeam  != null) leftBeam.transform.position  = new Vector3(leftX,  handY, handZ);
            if (rightBeam != null) rightBeam.transform.position = new Vector3(rightX, handY, handZ);

            // ── Damage tick ──────────────────────────────────────────────
            // Only damage when the player is INSIDE an actual beam strip.
            // Safe zones: center gap between beams, and areas outside both beams.
            if (tickTimer >= dualBeamTickRate)
            {
                tickTimer = 0f;
                if (player != null)
                {
                    float px    = player.position.x;
                    float halfW = dualBeamWidth * 0.5f;

                    bool inLeftBeam  = px >= leftX  - halfW && px <= leftX  + halfW;
                    bool inRightBeam = px >= rightX - halfW && px <= rightX + halfW;

                    if (inLeftBeam || inRightBeam)
                        player.GetComponent<PlayerHealth>()?.TakeDamage(dualBeamDamagePerTick);
                }
            }

            yield return null;
        }

        UnregisterBeam(leftBeam);
        UnregisterBeam(rightBeam);
        if (leftBeam  != null) Destroy(leftBeam);
        if (rightBeam != null) Destroy(rightBeam);
    }

    /// <summary>
    /// Creates a hand-beam visual: a flat lane that starts at the hand position and
    /// extends along -Z (toward the player), anchored at the hand's Y height.
    /// The beam is wide on X (dualBeamWidth), thin on Y (flat in 3D), and long on Z (dualBeamLength).
    /// Moving this object's X position sweeps the beam inward correctly.
    /// </summary>
    private GameObject SpawnDualBeamVisual(Vector3 handPos)
    {
        if (handBeamPrefab != null)
        {
            return Instantiate(handBeamPrefab, handPos, Quaternion.identity);
        }

        // Runtime fallback: flat beam lane extending from the hand toward the player
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "SatanHandBeam";
        Destroy(go.GetComponent<Collider>());

        // Center the cube so its back edge starts at handPos and extends forward (-Z)
        go.transform.position   = handPos + Vector3.back * dualBeamLength * 0.5f;
        go.transform.localScale = new Vector3(dualBeamWidth, 0.3f, dualBeamLength);

        Renderer rend = go.GetComponent<Renderer>();
        if (rend != null)
        {
            Material mat = new Material(Shader.Find("Sprites/Default"));
            mat.color = dualBeamColor;
            rend.material = mat;
        }

        // Small glow emitter at the hand origin point so the beam reads as "fired from hand"
        GameObject emitter = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        emitter.name = "HandEmitter";
        emitter.transform.SetParent(go.transform, false);
        emitter.transform.position   = handPos;
        emitter.transform.localScale = Vector3.one * dualBeamWidth * 1.6f;
        Destroy(emitter.GetComponent<Collider>());
        Renderer er = emitter.GetComponent<Renderer>();
        if (er != null)
        {
            Material em = new Material(Shader.Find("Sprites/Default"));
            em.color = new Color(dualBeamColor.r, dualBeamColor.g * 0.6f, 0f, 1f);
            er.material = em;
        }

        return go;
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────
    #region Hop Helper

    /// <summary>Small pre-attack repositioning hop.</summary>
    private IEnumerator PerformHop(bool biasTowardPlayer)
    {
        Vector3 start = transform.position;
        Vector3 hopDir;

        bool moveTowardPlayer = biasTowardPlayer && Random.value < hopBiasTowardPlayer && player != null;
        if (moveTowardPlayer)
        {
            Vector3 toPlayer = player.position - start;
            toPlayer.y = 0f;
            toPlayer.Normalize();
            // Only take the X component to keep Satan at top of arena
            hopDir = new Vector3(toPlayer.x, 0f, 0f).normalized;
        }
        else
        {
            hopDir = Random.value < 0.5f ? Vector3.left : Vector3.right;
        }

        Vector3 end = start + hopDir * hopDistance;
        // Clamp within arena X bounds
        float cx = GetComponentInParent<SatanBossController>()?.transform.position.x ?? 0f;
        end.x = Mathf.Clamp(end.x, cx - 6f, cx + 6f);

        float t = 0f;
        while (t < hopDuration)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / hopDuration);
            float arc = 4f * u * (1f - u) * hopArcHeight;
            transform.position = new Vector3(
                Mathf.Lerp(start.x, end.x, u),
                start.y + arc,
                transform.position.z);
            yield return null;
        }
        transform.position = new Vector3(end.x, start.y, transform.position.z);
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────
    #region Spawn Helpers

    private void SpawnBulletFan(Vector3 origin, Vector3 center, int count, float totalAngle, float speed, float damage)
    {
        float half = totalAngle * 0.5f;
        for (int i = 0; i < count; i++)
        {
            float t   = count > 1 ? i / (float)(count - 1) : 0.5f;
            float ang = Mathf.Lerp(-half, half, t);
            Vector3 dir = Quaternion.AngleAxis(ang, Vector3.up) * center;
            SpawnOneBullet(origin, dir, speed, damage);
        }
    }

    private void SpawnRadialBurst(Vector3 origin, int count, float angleOffset, float speed, float damage)
    {
        float step = 360f / count;
        for (int i = 0; i < count; i++)
        {
            float  ang = (step * i + angleOffset) * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Sin(ang), 0f, Mathf.Cos(ang));
            SpawnOneBullet(origin, dir, speed, damage);
        }
    }

    /// <summary>
    /// Spawns a single Satan bullet. Uses bulletPrefab if wired; otherwise builds one at runtime
    /// so bullets always fire even before Setup Everything is run.
    /// </summary>
    private void SpawnOneBullet(Vector3 origin, Vector3 dir, float speed, float damage)
    {
        // Clear the floor plane (floor mesh top ~ y=0): spawning at y=0 overlaps solid colliders
        // and SatanBullet was destroying itself immediately in OnTriggerEnter.
        Vector3 spawnPos = origin + Vector3.up * bulletSpawnYOffset;

        GameObject b;
        if (bulletPrefab != null)
        {
            b = Instantiate(bulletPrefab, spawnPos, Quaternion.LookRotation(dir));
        }
        else
        {
            // Runtime fallback: build a minimal bullet GameObject on the fly
            b = new GameObject("SatanBullet_RT");
            b.transform.position = spawnPos;
            b.transform.rotation = Quaternion.LookRotation(dir);

            SphereCollider col = b.AddComponent<SphereCollider>();
            col.isTrigger = true;
            col.radius    = 0.35f;

            Rigidbody rb = b.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity  = false;

            b.AddComponent<SatanBullet>();
            b.AddComponent<Billboard>();
        }

        SatanBullet sb = b.GetComponent<SatanBullet>();
        if (sb != null)
        {
            if (bulletSprite != null) sb.SetSprite(bulletSprite);
            sb.Init(dir, speed, damage);
        }
    }

    private GameObject SpawnBeam(Vector3 origin, Vector3 direction, float width, GameObject prefab)
    {
        if (prefab != null)
        {
            GameObject beam = Instantiate(prefab, origin, Quaternion.LookRotation(direction));
            return beam;
        }
        // Fallback: primitive beam using a scaled cube
        return CreatePrimitiveBeam(origin, direction, width, 30f, Color.red);
    }

    private GameObject CreatePrimitiveBeam(Vector3 pos, Vector3 dir, float width, float length, Color color)
    {
        GameObject go  = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name        = "SatanBeam_Primitive";
        Destroy(go.GetComponent<Collider>());
        go.transform.position   = pos + dir * length * 0.5f;
        go.transform.localScale = new Vector3(width, 0.15f, length);
        go.transform.rotation   = Quaternion.LookRotation(dir);

        Renderer rend = go.GetComponent<Renderer>();
        if (rend != null)
        {
            Material mat = new Material(Shader.Find("Sprites/Default"));
            mat.color = color;
            rend.material = mat;
        }
        return go;
    }

    private void DamagePlayerInBeam(Vector3 origin, Vector3 dir, float width, float damage)
    {
        if (player == null) return;
        // Simple: is player within beam width of the beam line?
        Vector3 toPlayer = player.position - origin;
        toPlayer.y = 0f;
        Vector3 d = dir; d.y = 0f; d.Normalize();
        float along = Vector3.Dot(toPlayer, d);
        if (along < 0f) return;
        Vector3 closest = origin + d * along;
        if (Vector3.Distance(new Vector3(player.position.x, 0, player.position.z),
                             new Vector3(closest.x, 0, closest.z)) < width * 0.5f + 0.3f)
        {
            player.GetComponent<PlayerHealth>()?.TakeDamage(damage);
        }
    }

    #endregion
}
