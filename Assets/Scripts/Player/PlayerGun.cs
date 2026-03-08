using UnityEngine;

/// <summary>
/// Manages the visual gun sprite that follows the player and rotates toward mouse
/// </summary>
public class PlayerGun : MonoBehaviour
{
    [Header("Gun Settings")]
    public Sprite gunSprite;
    public Vector3 gunOffset = new Vector3(0.3f, 0.5f, 0f); // Position relative to player
    public Vector3 gunScale = new Vector3(1.2f, 1.2f, 1f);
    
    private GameObject gunObject;
    private SpriteRenderer gunRenderer;
    private Camera mainCamera;
    
    void Start()
    {
        mainCamera = Camera.main;
        SetupGunSprite();
    }
    
    void SetupGunSprite()
    {
        // Create gun sprite child
        gunObject = new GameObject("Gun_Sprite");
        gunObject.transform.SetParent(transform);
        gunObject.transform.localPosition = gunOffset;
        gunObject.transform.localScale = gunScale;
        
        // Add sprite renderer
        gunRenderer = gunObject.AddComponent<SpriteRenderer>();
        gunRenderer.sprite = gunSprite;
        gunRenderer.sortingOrder = 15; // Above character sprite
        
        // Make it face camera
        gunObject.AddComponent<Billboard>();
    }
    
    void LateUpdate()
    {
        if (gunObject == null || mainCamera == null) return;
        
        // Calculate angle to mouse cursor
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        Plane ground = new Plane(Vector3.up, Vector3.zero);
        
        if (ground.Raycast(ray, out float dist))
        {
            Vector3 mousePos = ray.GetPoint(dist);
            Vector3 dirToMouse = (mousePos - transform.position).normalized;
            
            // Calculate angle in degrees
            float angle = Mathf.Atan2(dirToMouse.x, dirToMouse.z) * Mathf.Rad2Deg;
            
            // Rotate gun sprite to point at mouse
            // Use localRotation since Billboard handles camera facing
            gunObject.transform.localRotation = Quaternion.Euler(0, 0, -angle);
            
            // Flip sprite if pointing left
            if (dirToMouse.x < 0)
                gunRenderer.flipY = true;
            else
                gunRenderer.flipY = false;
        }
    }
}
