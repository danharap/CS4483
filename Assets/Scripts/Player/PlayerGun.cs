using UnityEngine;

/// <summary>
/// Manages the visual gun sprite that rotates around player to point at mouse
/// Gun pivots around player and rotates to aim at cursor
/// </summary>
public class PlayerGun : MonoBehaviour
{
    [Header("Gun Settings")]
    public Sprite gunSprite;
    public float orbitRadius = 0.4f; // Distance from player center
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
        // Create pivot object that will orbit around player
        gunPivot = new GameObject("Gun_Pivot");
        gunPivot.transform.SetParent(transform);
        
        // Add billboard to pivot so it faces camera
        gunPivot.AddComponent<Billboard>();
        
        // Create sprite child that will rotate within the billboard
        gunSpriteObj = new GameObject("Gun_Sprite");
        gunSpriteObj.transform.SetParent(gunPivot.transform);
        gunSpriteObj.transform.localPosition = Vector3.zero;
        gunSpriteObj.transform.localScale = Vector3.one * 1.3f;
        
        // Add sprite renderer to child
        gunRenderer = gunSpriteObj.AddComponent<SpriteRenderer>();
        gunRenderer.sprite = gunSprite;
        gunRenderer.sortingOrder = 15; // Above character sprite
    }
    
    void LateUpdate()
    {
        if (gunPivot == null || mainCamera == null) return;
        
        // Get mouse position in world space
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        Plane ground = new Plane(Vector3.up, Vector3.zero);
        
        if (ground.Raycast(ray, out float dist))
        {
            Vector3 mousePos = ray.GetPoint(dist);
            Vector3 playerPos = transform.position;
            
            // Calculate direction from player to mouse (on ground plane)
            Vector3 dirToMouse = new Vector3(mousePos.x - playerPos.x, 0f, mousePos.z - playerPos.z).normalized;
            
            // Position gun pivot orbiting around player toward mouse
            Vector3 pivotWorldPos = playerPos + dirToMouse * orbitRadius;
            pivotWorldPos.y = playerPos.y + gunHeight;
            gunPivot.transform.position = pivotWorldPos;
            
            // Calculate angle to rotate gun sprite (Z-axis rotation in billboard space)
            float angleToMouse = Mathf.Atan2(dirToMouse.x, dirToMouse.z) * Mathf.Rad2Deg;
            
            // Rotate the sprite within the billboard's local space
            gunSpriteObj.transform.localRotation = Quaternion.Euler(0f, 0f, -angleToMouse);
            
            // Flip sprite when aiming left
            if (dirToMouse.x < 0)
                gunRenderer.flipY = true;
            else
                gunRenderer.flipY = false;
        }
    }
}
