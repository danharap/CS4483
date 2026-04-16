using System.Collections;
using UnityEngine;

/// <summary>
/// Auto-attack system: finds nearest enemy, fires projectiles at a set rate.
/// All tunables are serialized for inspector tweaking.
/// </summary>
public class PlayerWeapon : MonoBehaviour
{
    // ── Tunables ──────────────────────────────────────────────────────────
    [Header("Weapon Stats")]
    [SerializeField] public float damage = 20f;
    [SerializeField] public float fireRate = 2f;      // shots per second
    [SerializeField] public float projectileSpeed = 14f;
    [SerializeField] public int   pierceCount = 0;    // extra enemies pierced
    [SerializeField] public int   projectileCount = 1; // simultaneous projectiles

    [Header("References")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform  firePoint;    // child transform at barrel
    
    [Header("Audio")]
    [SerializeField] private AudioClip shootSound;
    [SerializeField] [Range(0f, 1f)] private float shootVolume = 0.35f;

    // ── State ─────────────────────────────────────────────────────────────
    private float fireTimer;
    private AudioSource audioSource;
    private bool wasHoldingFire;

    [Header("Input / Locks")]
    [Tooltip("If false, the player cannot shoot (e.g., in lobby/main menu until entering arena).")]
    [SerializeField] private bool shootingEnabled = true;

    private float baseDamage;
    private float baseFireRate;
    private int   basePierceCount;
    private int   baseProjectileCount;

    void Awake()
    {
        baseDamage          = damage;
        baseFireRate        = fireRate;
        basePierceCount     = pierceCount;
        baseProjectileCount = projectileCount;
    }

    /// <summary>Undo all upgrade-applied stat changes, returning weapon to its serialized defaults.</summary>
    public void ResetToBase()
    {
        damage          = baseDamage;
        fireRate        = baseFireRate;
        pierceCount     = basePierceCount;
        projectileCount = baseProjectileCount;
        fireTimer       = 0f;
    }

    public void RestoreFromSave(float dmg, float rate, int pierce, int projCount)
    {
        damage          = Mathf.Max(0.01f, dmg);
        fireRate        = Mathf.Max(0.01f, rate);
        pierceCount     = Mathf.Max(0, pierce);
        projectileCount = Mathf.Max(1, projCount);
        fireTimer       = 0f;
    }

    void Start()
    {
        AudioClip weaponRes = Resources.Load<AudioClip>("SFX/WeaponEffect");
        if (weaponRes != null)
            shootSound = weaponRes;

        // Setup audio source for shooting sounds
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // 2D sound
    }
    
    void Update()
    {
        // If we're not in the main gameplay scene (no GameManager), never auto-shoot.
        // This prevents weapon fire in MainMenu and other non-game scenes.
        if (GameManager.Instance == null) return;

        if (GameManager.Instance.State != GameManager.GameState.Playing) return;
        if (!shootingEnabled) return;

        float interval = 1f / Mathf.Max(0.01f, fireRate); // guard divide-by-zero / inf

        bool holdingFire = Input.GetMouseButton(0);
        wasHoldingFire = holdingFire;

        if (!holdingFire) return;

        // Single source of truth: fireTimer is time-since-last-shot.
        // Clicking and holding both respect the same rate limit (prevents click-spam exploits).
        fireTimer += Time.deltaTime;
        if (fireTimer < interval) return;

        fireTimer = 0f;
        TryShoot();
    }

    public void SetShootingEnabled(bool enabled)
    {
        shootingEnabled = enabled;
        if (!enabled)
        {
            fireTimer = 0f;
            wasHoldingFire = false;
        }
    }

    private void TryShoot()
    {
        // Shoot toward mouse cursor position
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Plane ground = new Plane(Vector3.up, Vector3.zero);
        
        if (!ground.Raycast(ray, out float dist)) return;
        
        Vector3 mousePos = ray.GetPoint(dist);
        Vector3 shootPos = firePoint != null ? firePoint.position : transform.position;
        Vector3 dir = (mousePos - shootPos);
        dir.y = 0f;                  // keep projectiles on the ground plane
        if (dir.sqrMagnitude < 0.0001f)
            dir = transform.forward; // sensible fallback
        dir.Normalize();

        bool hasOpeningStrike = GetComponent<OpeningStrikeTracker>() != null;

        if (projectileCount == 1)
        {
            SpawnProjectile(dir, hasOpeningStrike, isExtraShot: false);
        }
        else
        {
            // Spread multiple projectiles in a fan; Opening Strike and full damage apply to the first only.
            float spreadAngle = 20f;
            float step = (projectileCount > 1) ? spreadAngle / (projectileCount - 1) : 0f;
            float startAngle = -spreadAngle * 0.5f;
            for (int i = 0; i < projectileCount; i++)
            {
                float angle = startAngle + step * i;
                Vector3 spread = Quaternion.Euler(0f, angle, 0f) * dir;
                spread.y = 0f;
                spread.Normalize();
                bool isFirst = (i == 0);
                SpawnProjectile(spread, applyOpeningStrike: hasOpeningStrike && isFirst, isExtraShot: !isFirst);
            }
        }
    }

    /// <summary>
    /// Damage per projectile when multishot is active is reduced slightly to keep
    /// multishot powerful but not overwhelmingly so.
    /// ~32% reduction applies to every projectile while multishot is active.
    /// </summary>
    private const float MultishotDamageScale = 0.68f;

    private void SpawnProjectile(Vector3 dir, bool applyOpeningStrike = false, bool isExtraShot = false)
    {
        if (projectilePrefab == null) return;
        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position + dir * 0.6f;
        GameObject go = Instantiate(projectilePrefab, spawnPos, Quaternion.LookRotation(dir));
        Projectile p = go.GetComponent<Projectile>();
        if (p != null)
        {
            float finalDamage = damage;
            if (projectileCount > 1) finalDamage *= MultishotDamageScale;
            if (applyOpeningStrike)
            {
                var tracker = GetComponent<OpeningStrikeTracker>();
                if (tracker != null) finalDamage *= tracker.ConsumeProcMultiplier();
            }
            p.Init(dir, finalDamage, projectileSpeed, pierceCount);
        }
        
        // Play shooting sound
        if (shootSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(shootSound, shootVolume);
        }
    }
}
