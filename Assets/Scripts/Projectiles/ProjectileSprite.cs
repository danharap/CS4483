using UnityEngine;

/// <summary>
/// Adds a sprite billboard to projectiles
/// Attach this to the Projectile prefab
/// </summary>
public class ProjectileSprite : MonoBehaviour
{
    [Header("Sprite Settings")]
    public Sprite bulletSprite;
    public Vector3 spriteScale = new Vector3(2.0f, 2.0f, 1f); // Enlarged bullets
    
    private GameObject spriteObj;
    private SpriteRenderer spriteRenderer;
    
    void Start()
    {
        SetupSpriteBillboard();
    }
    
    void SetupSpriteBillboard()
    {
        // Create sprite child
        spriteObj = new GameObject("Bullet_Sprite");
        spriteObj.transform.SetParent(transform);
        spriteObj.transform.localPosition = Vector3.zero;
        spriteObj.transform.localScale = spriteScale;
        
        // Add sprite renderer
        spriteRenderer = spriteObj.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = bulletSprite;
        spriteRenderer.sortingOrder = 10;
        
        // Add billboard to face camera
        spriteObj.AddComponent<Billboard>();
    }
}
