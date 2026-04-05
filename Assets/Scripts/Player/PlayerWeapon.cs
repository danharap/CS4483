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
    [SerializeField] [Range(0f, 1f)] private float shootVolume = 0.06f;

    // ── State ─────────────────────────────────────────────────────────────
    private float fireTimer;
    private AudioSource audioSource;

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

    void Start()
    {
        // Setup audio source for shooting sounds
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // 2D sound
    }
    
    void Update()
    {
        if (GameManager.Instance != null &&
            GameManager.Instance.State != GameManager.GameState.Playing) return;

        fireTimer += Time.deltaTime;
        if (fireTimer >= 1f / fireRate)
        {
            fireTimer = 0f;
            TryShoot();
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
            SpawnProjectile(dir, hasOpeningStrike);
        }
        else
        {
            // Spread multiple projectiles in a fan; Opening Strike applies to the first only
            float spreadAngle = 20f;
            float step = (projectileCount > 1) ? spreadAngle / (projectileCount - 1) : 0f;
            float startAngle = -spreadAngle * 0.5f;
            for (int i = 0; i < projectileCount; i++)
            {
                float angle = startAngle + step * i;
                Vector3 spread = Quaternion.Euler(0f, angle, 0f) * dir;
                spread.y = 0f;
                spread.Normalize();
                SpawnProjectile(spread, hasOpeningStrike && i == 0);
            }
        }
    }

    private void SpawnProjectile(Vector3 dir, bool applyOpeningStrike = false)
    {
        if (projectilePrefab == null) return;
        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position + dir * 0.6f;
        GameObject go = Instantiate(projectilePrefab, spawnPos, Quaternion.LookRotation(dir));
        Projectile p = go.GetComponent<Projectile>();
        if (p != null)
        {
            float finalDamage = damage;
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
