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

    // ── State ─────────────────────────────────────────────────────────────
    private float fireTimer;

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
        Vector3 dir = (mousePos - shootPos).normalized;

        if (projectileCount == 1)
        {
            SpawnProjectile(dir);
        }
        else
        {
            // Spread multiple projectiles in a fan
            float spreadAngle = 20f;
            float step = (projectileCount > 1) ? spreadAngle / (projectileCount - 1) : 0f;
            float startAngle = -spreadAngle * 0.5f;
            for (int i = 0; i < projectileCount; i++)
            {
                float angle = startAngle + step * i;
                Vector3 spread = Quaternion.Euler(0f, angle, 0f) * dir;
                SpawnProjectile(spread);
            }
        }
    }

    private void SpawnProjectile(Vector3 dir)
    {
        if (projectilePrefab == null) return;
        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position + dir * 0.6f;
        GameObject go = Instantiate(projectilePrefab, spawnPos, Quaternion.LookRotation(dir));
        Projectile p = go.GetComponent<Projectile>();
        if (p != null)
            p.Init(dir, damage, projectileSpeed, pierceCount);
    }
}
