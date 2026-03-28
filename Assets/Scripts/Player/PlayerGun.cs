using UnityEngine;

/// <summary>
/// Manages the visual gun sprite that rotates around player to point at mouse
/// Gun pivots around player and rotates to aim at cursor
/// </summary>
public class PlayerGun : MonoBehaviour
{
    [Header("Gun Settings")]
    public Sprite gunSprite;
    public float orbitRadius = 0.6f; // Distance from player center (increased from 0.4)
    public float gunHeight = 0.5f; // Height above ground
    
    private GameObject gunPivot; // Parent for billboard
    private GameObject gunSpriteObj;  // Child for actual sprite rotation
    private SpriteRenderer gunRenderer;
    private Camera mainCamera;
    
    void Start()
    {
        mainCamera = Camera.main;
        SetupGunSprite();
    }
    
    void SetupGunSprite()
    {
        // Pivot rotates around world Y so gun points at mouse (no billboard — rotation is axis-based)
        gunPivot = new GameObject("Gun_Pivot");
        gunPivot.transform.SetParent(transform);
        gunPivot.transform.localPosition = new Vector3(0f, gunHeight, 0f);
        
        gunSpriteObj = new GameObject("Gun_Sprite");
        gunSpriteObj.transform.SetParent(gunPivot.transform);
        gunSpriteObj.transform.localPosition = new Vector3(orbitRadius, 0f, 0f); // Barrel (right side) at this offset
        gunSpriteObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // Lay sprite in XZ plane so it orbits correctly
        gunSpriteObj.transform.localScale = Vector3.one * 1.5f;
        
        gunRenderer = gunSpriteObj.AddComponent<SpriteRenderer>();
        gunRenderer.sprite = gunSprite;
        gunRenderer.sortingOrder = 8;
        // No Billboard: pivot rotation alone makes the gun's right side point at the cursor on an axis around the player
    }
    
    void LateUpdate()
    {
        if (gunPivot == null || mainCamera == null) return;
        
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        // Use player height so the plane matches the gun position
        Plane ground = new Plane(Vector3.up, new Vector3(0f, transform.position.y, 0f));
        
        if (ground.Raycast(ray, out float dist))
        {
            Vector3 mousePos = ray.GetPoint(dist);
            Vector3 playerPos = transform.position;
            Vector3 dirToMouse = new Vector3(mousePos.x - playerPos.x, 0f, mousePos.z - playerPos.z);
            if (dirToMouse.sqrMagnitude < 0.01f) return;
            dirToMouse.Normalize();
            
            // Rotate pivot around world Y so gun local +X (barrel) points at mouse.
            // Subtract 90° because atan2(x,z) is relative to +Z, but Y=0° already points +X.
            float angleY = Mathf.Atan2(dirToMouse.x, dirToMouse.z) * Mathf.Rad2Deg - 90f;
            gunPivot.transform.rotation = Quaternion.Euler(0f, angleY, 0f);
            
            // Flip: negate the sprite object's Y scale when aiming left.
            // The sprite lies flat in XZ (rotated 90° on X). A 180° pivot rotation
            // would leave the art upside-down; negating localScale.y mirrors it through
            // the XZ plane, keeping the gun right-side-up from the angled camera.
            float baseScale = 1.5f;
            bool facingLeft = dirToMouse.x < 0f;
            float sy = facingLeft ? -baseScale : baseScale;
            gunSpriteObj.transform.localScale = new Vector3(baseScale, sy, baseScale);
        }
    }
}
