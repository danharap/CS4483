using UnityEngine;

/// <summary>
/// Projectile fired by Satan's Direct Attack and Fan Attack.
/// Moves in a straight line, damages only the player.
///
/// Sprite timing fix:
///   Awake() always creates the SpriteRenderer child but does NOT destroy it if the sprite is null.
///   SetSprite() is called by SatanAttacks immediately after Instantiate(), before Start() runs.
///   Start() then checks: if a sprite was provided, keep it; otherwise build the orange-sphere fallback.
///   This prevents the fallback from replacing a valid sprite that arrives between Awake and Start.
/// </summary>
public class SatanBullet : MonoBehaviour
{
    [Header("Visual")]
    [Tooltip("The bullet sprite. Assign Bullets.png in the Inspector (wired automatically by SpriteSetup).")]
    [SerializeField] public Sprite bulletSprite;
    [SerializeField] private float visualScale = 1.8f;

    [Header("Travel")]
    [SerializeField] private float maxTravelDistance = 40f;

    private Vector3 direction;
    private float   speed;
    private float   damage;
    private float   travelled;
    private SpriteRenderer sr;

    private void Awake()
    {
        int bp = LayerMask.NameToLayer("BossProjectile");
        if (bp >= 0)
            gameObject.layer = bp;

        // Always build the sprite visual child in Awake.
        // Do NOT destroy or replace it here even if bulletSprite is null —
        // SetSprite() may be called before Start() runs.
        sr = GetComponentInChildren<SpriteRenderer>();
        if (sr == null)
        {
            GameObject vis = new GameObject("BulletVisual");
            vis.transform.SetParent(transform, false);
            vis.transform.localPosition = Vector3.zero;
            vis.transform.localScale    = new Vector3(visualScale, visualScale, 1f);
            vis.AddComponent<Billboard>();
            sr             = vis.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 35;
        }

        // If the prefab already has the sprite baked in, apply it immediately.
        if (bulletSprite != null)
            sr.sprite = bulletSprite;
    }

    private void Start()
    {
        // By the time Start runs, SatanAttacks.SpawnOneBullet() has already called SetSprite().
        // Only fall back to the orange sphere if no sprite was ever provided.
        if (sr != null && sr.sprite != null) return;
        if (bulletSprite != null)
        {
            if (sr != null) sr.sprite = bulletSprite;
            return;
        }

        // No sprite — swap in a visible orange sphere so bullets are never invisible.
        if (sr != null && sr.gameObject != gameObject)
            Destroy(sr.gameObject);
        sr = null;

        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = "BulletSphere";
        sphere.transform.SetParent(transform, false);
        sphere.transform.localPosition = Vector3.zero;
        sphere.transform.localScale    = Vector3.one * (visualScale * 0.4f);
        Destroy(sphere.GetComponent<Collider>());

        MeshRenderer mr = sphere.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = new Color(1f, 0.4f, 0.05f);
            mat.SetColor("_EmissionColor", new Color(1f, 0.25f, 0f) * 1.5f);
            mat.EnableKeyword("_EMISSION");
            mr.material = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }

    public void Init(Vector3 dir, float spd, float dmg)
    {
        dir.y     = 0f;
        direction = dir.sqrMagnitude > 0.001f ? dir.normalized : Vector3.forward;
        speed     = spd;
        damage    = dmg;
    }

    /// <summary>
    /// Override the bullet sprite at spawn time (called by SatanAttacks immediately after Instantiate).
    /// Safe to call before Start() runs.
    /// </summary>
    public void SetSprite(Sprite s)
    {
        bulletSprite = s;
        if (sr != null) sr.sprite = s;
    }

    private void Update()
    {
        float step = speed * Time.deltaTime;
        transform.position += direction * step;
        travelled += step;
        if (travelled >= maxTravelDistance)
            Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            other.GetComponent<PlayerHealth>()?.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        if (other.GetComponent<EnemyBase>() != null) return;

        int propLayer = LayerMask.NameToLayer("ArenaProp");
        if (propLayer >= 0 && other.gameObject.layer == propLayer) return;

        if (other.isTrigger) return;

        if (IsGroundLikeCollider(other)) return;

        Destroy(gameObject);
    }

    private bool IsGroundLikeCollider(Collider other)
    {
        const float minClearanceBelowCenter = 0.35f;
        return other.bounds.max.y < transform.position.y - minClearanceBelowCenter;
    }
}
