using UnityEngine;
using System.Collections;

/// <summary>
/// Automatically applies sprite billboard to game objects at runtime
/// Attach to Player and Enemy prefabs
/// </summary>
public class SpriteCharacter : MonoBehaviour
{
    [Header("Sprite Settings")]
    public Sprite[] idleFrames; // Idle animation frames
    public Sprite[] runFrames; // Run animation frames (for player)
    public Sprite deathSprite; // Optional death sprite
    public float frameRate = 12f;
    public Color tintColor = Color.white;
    public Vector3 spriteScale = new Vector3(0.8f, 1.2f, 1f);
    
    private GameObject spriteObj;
    private SpriteRenderer spriteRenderer;
    private float frameTimer;
    private int currentFrame;
    private Color originalTint;
    private bool isDead = false;
    private Sprite[] currentAnimationFrames;
    private CharacterController characterController; // For detecting player movement
    
    void Start()
    {
        originalTint = tintColor;
        currentAnimationFrames = idleFrames;
        characterController = GetComponent<CharacterController>(); // For player movement detection
        
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
        
        if (currentAnimationFrames != null && currentAnimationFrames.Length > 0)
            spriteRenderer.sprite = currentAnimationFrames[0];
    }
    
    void Update()
    {
        if (isDead) return;
        
        // Switch between idle and run animations for player
        if (characterController != null && runFrames != null && runFrames.Length > 0)
        {
            // Check if player is moving (velocity magnitude > threshold)
            bool isMoving = characterController.velocity.sqrMagnitude > 0.1f;
            
            Sprite[] targetFrames = isMoving ? runFrames : idleFrames;
            
            // Reset frame counter if switching animations
            if (targetFrames != currentAnimationFrames)
            {
                currentAnimationFrames = targetFrames;
                currentFrame = 0;
                frameTimer = 0f;
            }
        }
        
        if (currentAnimationFrames == null || currentAnimationFrames.Length == 0) return;
        
        // Animate through frames
        frameTimer += Time.deltaTime;
        if (frameTimer >= 1f / frameRate)
        {
            frameTimer = 0f;
            currentFrame = (currentFrame + 1) % currentAnimationFrames.Length;
            spriteRenderer.sprite = currentAnimationFrames[currentFrame];
        }
    }
    
    public void FlashWhite(float duration)
    {
        if (spriteRenderer != null)
            StartCoroutine(FlashCoroutine(duration));
    }
    
    private IEnumerator FlashCoroutine(float duration)
    {
        if (spriteRenderer == null) yield break;
        spriteRenderer.color = Color.white;
        yield return new WaitForSeconds(duration);
        spriteRenderer.color = originalTint;
    }
    
    public void PlayDeathAnimation()
    {
        isDead = true;
        
        if (spriteRenderer != null && deathSprite != null)
        {
            spriteRenderer.sprite = deathSprite;
        }
        
        // Fade out
        if (spriteRenderer != null)
            StartCoroutine(FadeOut());
    }
    
    private IEnumerator FadeOut()
    {
        float elapsed = 0f;
        float duration = 0.4f;
        Color startColor = spriteRenderer.color;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
            spriteRenderer.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            yield return null;
        }
    }
}
