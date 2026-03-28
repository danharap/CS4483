using UnityEngine;

/// <summary>
/// Proper gun aiming for a top-down 3D game with an angled camera.
///
/// Hierarchy:
///   Player
///     └─ GunPivot          ← rotates around world-Y to face the mouse cursor
///          └─ GunSprite    ← SpriteRenderer, lies flat in XZ plane (local X = euler(90,0,0))
///               └─ FirePoint  ← world position bullets should spawn from
///
/// Attach this script to the Player. Leave the references null to auto-build the hierarchy.
/// </summary>
public class GunAimController : MonoBehaviour
{
    [Header("Hierarchy (auto-built if left null)")]
    [Tooltip("Empty pivot at player center. Rotates around Y to face the cursor.")]
    [SerializeField] private Transform gunPivot;
    [Tooltip("SpriteRenderer child under GunPivot.")]
    [SerializeField] private SpriteRenderer gunSpriteRenderer;
    [Tooltip("Child transform at barrel tip — bullets are fired from here.")]
    [SerializeField] public Transform firePoint;

    [Header("Visuals")]
    [Tooltip("The gun sprite asset. Barrel must face RIGHT (+X) in the image.")]
    [SerializeField] private Sprite gunSprite;
    [Tooltip("How far the sprite sits from the pivot center.")]
    [SerializeField] private float orbitRadius = 0.6f;
    [Tooltip("Height above ground for the gun pivot.")]
    [SerializeField] private float gunHeight = 0.5f;
    [Tooltip("Uniform scale applied to the gun sprite object.")]
    [SerializeField] private float spriteScale = 1.5f;

    [Header("Debug")]
    [SerializeField] private bool showGizmo = true;

    // ── State ─────────────────────────────────────────────────────────────

    private Camera mainCam;
    private Vector3 aimDir;

    /// <summary>Normalized aim direction in the XZ plane.</summary>
    public Vector3 AimDirection => aimDir;

    /// <summary>World position of the barrel tip (use for bullet spawn).</summary>
    public Vector3 FirePointWorld => firePoint != null ? firePoint.position : transform.position;

    // ─────────────────────────────────────────────────────────────────────

    void Start()
    {
        mainCam = Camera.main;
        if (gunPivot == null) BuildHierarchy();
    }

    /// <summary>Programmatically builds the GunPivot → GunSprite → FirePoint hierarchy.</summary>
    private void BuildHierarchy()
    {
        GameObject pivot = new GameObject("GunPivot");
        pivot.transform.SetParent(transform);
        pivot.transform.localPosition = new Vector3(0f, gunHeight, 0f);
        gunPivot = pivot.transform;

        GameObject spriteGO = new GameObject("GunSprite");
        spriteGO.transform.SetParent(gunPivot);
        // Orbit the pivot at +X so barrel points in the pivot's forward (+X) direction
        spriteGO.transform.localPosition = new Vector3(orbitRadius, 0f, 0f);
        // Rotate 90° on X so the sprite lies flat in the XZ plane (visible from angled camera)
        spriteGO.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        spriteGO.transform.localScale = Vector3.one * spriteScale;

        gunSpriteRenderer = spriteGO.AddComponent<SpriteRenderer>();
        gunSpriteRenderer.sprite = gunSprite;
        gunSpriteRenderer.sortingOrder = 8;

        // FirePoint sits at the tip of the barrel in the sprite's local space.
        // Local +X = tip of barrel (matches sprite art where barrel faces right).
        GameObject fp = new GameObject("FirePoint");
        fp.transform.SetParent(spriteGO.transform);
        fp.transform.localPosition = new Vector3(0.5f, 0f, 0f);
        firePoint = fp.transform;
    }

    void LateUpdate()
    {
        if (gunPivot == null || mainCam == null) return;

        // ── 1. Raycast mouse onto the ground plane at player height ──────────
        Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);
        Plane ground = new Plane(Vector3.up, new Vector3(0f, transform.position.y, 0f));

        if (!ground.Raycast(ray, out float dist)) return;

        Vector3 mouseWorld = ray.GetPoint(dist);
        Vector3 toMouse = mouseWorld - transform.position;
        toMouse.y = 0f;
        if (toMouse.sqrMagnitude < 0.01f) return;
        toMouse.Normalize();
        aimDir = toMouse;

        // ── 2. Rotate GunPivot around world-Y so +X faces the mouse ─────────
        // atan2(x, z) gives angle from +Z; subtract 90° to align with +X-right convention.
        float angleY = Mathf.Atan2(toMouse.x, toMouse.z) * Mathf.Rad2Deg - 90f;
        gunPivot.rotation = Quaternion.Euler(0f, angleY, 0f);

        // ── 3. Flip when aiming left ─────────────────────────────────────────
        // The sprite lies flat in XZ (rotated 90° on X). When the pivot rotates 180°
        // (barrel pointing left), the sprite art appears upside-down from the camera.
        // Negating the sprite object's local Y scale mirrors it through XZ, correcting
        // the orientation so the gun always looks right-side-up.
        bool facingLeft = toMouse.x < 0f;
        float sy = facingLeft ? -spriteScale : spriteScale;
        if (gunSpriteRenderer != null)
            gunSpriteRenderer.transform.localScale = new Vector3(spriteScale, sy, spriteScale);
    }

    void OnDrawGizmosSelected()
    {
        if (!showGizmo || !Application.isPlaying) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position + Vector3.up * gunHeight, aimDir * 2.5f);
        if (firePoint != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(firePoint.position, 0.08f);
        }
    }
}
