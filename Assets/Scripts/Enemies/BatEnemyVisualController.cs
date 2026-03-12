using System.Collections;
using UnityEngine;

/// <summary>
/// Sprite billboard controller for bat enemies.
/// - Loops normalFrames while alive
/// - On bite trigger, plays biteFrames for a short duration then resumes normal
/// - Supports hit flash + death fade similar to SpriteCharacter
/// </summary>
public class BatEnemyVisualController : MonoBehaviour
{
    [Header("Frames")]
    public Sprite[] normalFrames;
    public Sprite[] biteFrames;
    public Sprite   deathSprite;

    [Header("Animation")]
    public float normalFrameRate = 12f;
    public float biteFrameRate   = 14f;
    public float biteDuration    = 0.35f;

    [Header("Sprite")]
    public Vector3 spriteScale = new Vector3(1.5f, 1.5f, 1f);
    public Vector3 spriteLocalOffset = Vector3.zero;
    public int sortingOrder = 10;

    private GameObject spriteObj;
    private SpriteRenderer sr;
    private bool isDead;
    private bool isBiting;
    private float timer;
    private int frameIndex;
    private float biteTimer;

    void Start()
    {
        Setup();
    }

    private void Setup()
    {
        if (spriteObj != null) return;
        spriteObj = new GameObject("Sprite_Billboard");
        spriteObj.transform.SetParent(transform);
        spriteObj.transform.localPosition = spriteLocalOffset;
        spriteObj.transform.localScale = spriteScale;

        sr = spriteObj.AddComponent<SpriteRenderer>();
        sr.sortingOrder = sortingOrder;
        spriteObj.AddComponent<Billboard>();

        if (normalFrames != null && normalFrames.Length > 0)
            sr.sprite = normalFrames[0];

        // Hide any 3D mesh renderer (keep collider)
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null) meshRenderer.enabled = false;
    }

    void Update()
    {
        if (isDead || sr == null) return;

        Sprite[] frames = isBiting ? biteFrames : normalFrames;
        float rate = isBiting ? biteFrameRate : normalFrameRate;

        if (frames == null || frames.Length == 0) return;

        timer += Time.deltaTime;
        if (timer >= 1f / rate)
        {
            timer = 0f;
            frameIndex = (frameIndex + 1) % frames.Length;
            sr.sprite = frames[frameIndex];
        }

        if (isBiting)
        {
            biteTimer -= Time.deltaTime;
            if (biteTimer <= 0f)
            {
                isBiting = false;
                frameIndex = 0;
                timer = 0f;
                if (normalFrames != null && normalFrames.Length > 0)
                    sr.sprite = normalFrames[0];
            }
        }
    }

    public void PlayBite()
    {
        if (isDead) return;
        if (biteFrames == null || biteFrames.Length == 0) return;
        isBiting = true;
        biteTimer = biteDuration;
        frameIndex = 0;
        timer = 0f;
        sr.sprite = biteFrames[0];
    }

    public void FlashRed(float duration)
    {
        if (sr == null) return;
        StartCoroutine(FlashCoroutine(duration));
    }

    private IEnumerator FlashCoroutine(float duration)
    {
        sr.color = Color.red;
        yield return new WaitForSeconds(duration);
        sr.color = Color.white;
    }

    public void PlayDeathAnimation()
    {
        if (isDead) return;
        isDead = true;
        if (sr != null && deathSprite != null) sr.sprite = deathSprite;
        StartCoroutine(FadeOut());
    }

    private IEnumerator FadeOut()
    {
        if (sr == null) yield break;
        float elapsed = 0f;
        float duration = 0.4f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
            sr.color = new Color(1f, 1f, 1f, alpha);
            yield return null;
        }
    }
}

