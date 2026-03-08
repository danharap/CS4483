using UnityEngine;

/// <summary>
/// Automatically applies sprite billboard to game objects at runtime
/// Attach to Player and Enemy prefabs
/// </summary>
public class SpriteCharacter : MonoBehaviour
{
    [Header("Sprite Settings")]
    public Sprite[] animationFrames; // Drag sprite frames here
    public float frameRate = 12f;
    public Color tintColor = Color.white;
    public Vector3 spriteScale = new Vector3(0.8f, 1.2f, 1f);
    
    private GameObject spriteObj;
    private SpriteRenderer spriteRenderer;
    private float frameTimer;
    private int currentFrame;
    
    void Start()
    {
        SetupSpriteBillboard();
        
        // Hide the 3D mesh renderer but keep collider
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null)
            meshRenderer.enabled = false;
    }
    
    void SetupSpriteBillboard()
    {
        // Create sprite child object
        spriteObj = new GameObject("Sprite_Billboard");
        spriteObj.transform.SetParent(transform);
        spriteObj.transform.localPosition = Vector3.zero;
        spriteObj.transform.localScale = spriteScale;
        
        // Add sprite renderer
        spriteRenderer = spriteObj.AddComponent<SpriteRenderer>();
        spriteRenderer.color = tintColor;
        spriteRenderer.sortingOrder = 10;
        
        // Add billboard to face camera
        spriteObj.AddComponent<Billboard>();
        
        if (animationFrames != null && animationFrames.Length > 0)
            spriteRenderer.sprite = animationFrames[0];
    }
    
    void Update()
    {
        if (animationFrames == null || animationFrames.Length == 0) return;
        
        // Animate through frames
        frameTimer += Time.deltaTime;
        if (frameTimer >= 1f / frameRate)
        {
            frameTimer = 0f;
            currentFrame = (currentFrame + 1) % animationFrames.Length;
            spriteRenderer.sprite = animationFrames[currentFrame];
        }
    }
}
