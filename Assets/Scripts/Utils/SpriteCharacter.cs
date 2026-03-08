using UnityEngine;
using System.Collections;

/// <summary>
/// Automatically applies sprite billboard to game objects at runtime
/// Attach to Player and Enemy prefabs
/// </summary>
public class SpriteCharacter : MonoBehaviour
{
    [Header("Sprite Settings")]
    public Sprite[] animationFrames; // Drag sprite frames here
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
    
    void Start()
    {
        originalTint = tintColor;
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
        if (isDead || animationFrames == null || animationFrames.Length == 0) return;
        
        // Animate through frames
        frameTimer += Time.deltaTime;
        if (frameTimer >= 1f / frameRate)
        {
            frameTimer = 0f;
            currentFrame = (currentFrame + 1) % animationFrames.Length;
            spriteRenderer.sprite = animationFrames[currentFrame];
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
