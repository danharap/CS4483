using UnityEngine;

/// <summary>
/// Projectile fired by Satan's Direct Attack and Fan Attack.
/// Moves in a straight line, damages only the player.
/// Creates its own sprite visual at runtime — no pre-baked SpriteRenderer needed on the prefab.
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
        // Build sprite visual child so it can Billboard independently.
        sr = GetComponentInChildren<SpriteRenderer>();
        if (sr == null)
        {
            GameObject vis = new GameObject("BulletVisual");
            vis.transform.SetParent(transform, false);
            vis.transform.localPosition = Vector3.zero;
            vis.transform.localScale = new Vector3(visualScale, visualScale, 1f);
            vis.AddComponent<Billboard>();
            sr = vis.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 20;
        }

        if (bulletSprite != null)
        {
            sr.sprite = bulletSprite;
        }
        else
        {
            // No sprite assigned — swap in a coloured sphere so bullets are always visible.
            // Destroy the empty sprite visual to avoid confusion.
            if (sr.gameObject != gameObject)
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
                // Emissive orange-red fireball material
                Material mat = new Material(Shader.Find("Standard"));
                mat.color = new Color(1f, 0.4f, 0.05f);
                mat.SetColor("_EmissionColor", new Color(1f, 0.25f, 0f) * 1.5f);
                mat.EnableKeyword("_EMISSION");
                mr.material = mat;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }
    }

    public void Init(Vector3 dir, float spd, float dmg)
    {
        dir.y    = 0f;
        direction = dir.sqrMagnitude > 0.001f ? dir.normalized : Vector3.forward;
        speed    = spd;
        damage   = dmg;
    }

    /// <summary>Override the bullet sprite at spawn time (called by SatanAttacks).</summary>
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
        if (!other.isTrigger)
            Destroy(gameObject);
    }
}
