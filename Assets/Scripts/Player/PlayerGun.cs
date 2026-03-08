using UnityEngine;

/// <summary>
/// Manages the visual gun sprite that follows the player and rotates toward mouse
/// </summary>
public class PlayerGun : MonoBehaviour
{
    [Header("Gun Settings")]
    public Sprite gunSprite;
    public Vector3 gunOffset = new Vector3(0.5f, 0.3f, 0.5f); // Position relative to player (offset to side)
    public Vector3 gunScale = new Vector3(1.0f, 1.0f, 1f);
    
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
        
        // Calculate direction to mouse cursor
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        Plane ground = new Plane(Vector3.up, Vector3.zero);
        
        if (ground.Raycast(ray, out float dist))
        {
            Vector3 mousePos = ray.GetPoint(dist);
            Vector3 dirToMouse = (mousePos - gunObject.transform.position).normalized;
            
            // Move gun offset based on mouse direction (orbit around player slightly)
            Vector3 orbitOffset = dirToMouse * 0.4f; // Offset toward mouse
            orbitOffset.y = gunOffset.y; // Keep height constant
            gunObject.transform.localPosition = orbitOffset;
            
            // Flip sprite based on mouse side
            if (dirToMouse.x < 0)
                gunRenderer.flipX = true;
            else
                gunRenderer.flipX = false;
        }
    }
}
