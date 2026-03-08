using UnityEngine;

/// <summary>
/// Adds a sprite billboard to pickup items (XP orbs, health packs)
/// Attach this to pickup prefabs
/// </summary>
public class PickupSprite : MonoBehaviour
{
    [Header("Sprite Settings")]
    public Sprite pickupSprite;
    public Vector3 spriteScale = new Vector3(1.5f, 1.5f, 1f);
    public float rotationSpeed = 45f; // Degrees per second
    public float bobSpeed = 2f; // Up/down bobbing speed
    public float bobAmount = 0.2f; // How much to bob
    
    private GameObject spriteObj;
    private SpriteRenderer spriteRenderer;
    private float bobTimer;
    private Vector3 startLocalPos;
    
    void Start()
    {
        SetupSpriteBillboard();
        startLocalPos = spriteObj.transform.localPosition;
    }
    
    void SetupSpriteBillboard()
    {
        // Hide the 3D mesh renderer but keep collider
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null)
            meshRenderer.enabled = false;
        
        // Create sprite child
        spriteObj = new GameObject("Pickup_Sprite");
        spriteObj.transform.SetParent(transform);
        spriteObj.transform.localPosition = Vector3.zero;
        spriteObj.transform.localScale = spriteScale;
        
        // Add sprite renderer
        spriteRenderer = spriteObj.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = pickupSprite;
        spriteRenderer.sortingOrder = 10;
        
        // Add billboard to face camera
        spriteObj.AddComponent<Billboard>();
    }
    
    void Update()
    {
        if (spriteObj == null) return;
        
        // Rotate sprite for visual interest
        spriteObj.transform.Rotate(Vector3.forward, rotationSpeed * Time.deltaTime, Space.Self);
        
        // Bob up and down
        bobTimer += Time.deltaTime * bobSpeed;
        float bobOffset = Mathf.Sin(bobTimer) * bobAmount;
        spriteObj.transform.localPosition = startLocalPos + new Vector3(0f, bobOffset, 0f);
    }
}
