using UnityEngine;

/// <summary>
/// XP orb dropped by enemies. Floats toward the player when within pickup radius,
/// then grants XP on collection.
/// </summary>
public class XPOrb : MonoBehaviour
{
    // ── Tunables ──────────────────────────────────────────────────────────
    [Header("Orb Settings")]
    [SerializeField] public float xpValue    = 10f;
    [SerializeField] private float attractSpeed = 8f;
    [SerializeField] private float collectRadius = 0.4f;   // snap-collect distance
    [SerializeField] private float maxLifetime   = 30f;

    // ── State ─────────────────────────────────────────────────────────────
    private Transform player;
    private PlayerXP  playerXP;
    private float     lifetime;
    private bool      attracted;

    void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            player   = p.transform;
            playerXP = p.GetComponent<PlayerXP>();
        }
    }

    void Update()
    {
        lifetime += Time.deltaTime;
        if (lifetime > maxLifetime) { Destroy(gameObject); return; }
        if (player == null || playerXP == null) return;

        float dist = Vector3.Distance(transform.position, player.position);

        // Check if within pickup radius
        if (!attracted && dist <= playerXP.pickupRadius)
            attracted = true;

        if (attracted)
        {
            transform.position = Vector3.MoveTowards(
                transform.position, player.position, attractSpeed * Time.deltaTime);

            if (dist <= collectRadius)
            {
                playerXP.AddXP(xpValue);
                Destroy(gameObject);
            }
        }
    }
}
