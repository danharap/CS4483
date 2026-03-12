using UnityEngine;

/// <summary>
/// Simple sprite-sheet animator for arena spike/zap traps.
/// Creates a billboarded SpriteRenderer child and cycles through frames.
/// </summary>
public class TrapSpriteAnimator : MonoBehaviour
{
    [Header("Animation")]
    public Sprite[] frames;
    public float frameRate = 12f;
    public Vector3 spriteScale = new Vector3(1.8f, 1.8f, 1f);
    public int sortingOrder = 1;

    [Header("Glow (Optional)")]
    public bool enableGlow = true;
    public Color glowColor = new Color(0.3f, 0.9f, 1f);
    [Range(0f, 10f)] public float glowIntensity = 1.4f;
    [Range(0.1f, 10f)] public float glowRange = 2.2f;

    private GameObject spriteObj;
    private SpriteRenderer sr;
    private Light glowLight;
    private float timer;
    private int frame;

    void Start()
    {
        EnsureVisual();
    }

    void EnsureVisual()
    {
        if (spriteObj != null) return;
        spriteObj = new GameObject("Trap_Sprite");
        spriteObj.transform.SetParent(transform);
        // Slightly above the floor so it doesn't Z-fight / clip into floor
        spriteObj.transform.localPosition = new Vector3(0f, 0.05f, 0f);
        spriteObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        spriteObj.transform.localScale = spriteScale;

        sr = spriteObj.AddComponent<SpriteRenderer>();
        sr.sortingOrder = sortingOrder;
        spriteObj.AddComponent<Billboard>();

        if (enableGlow)
        {
            glowLight = GetComponent<Light>();
            if (glowLight == null) glowLight = gameObject.AddComponent<Light>();
            glowLight.type = LightType.Point;
            glowLight.color = glowColor;
            glowLight.intensity = glowIntensity;
            glowLight.range = glowRange;
            glowLight.shadows = LightShadows.None;
        }

        if (frames != null && frames.Length > 0)
            sr.sprite = frames[0];
    }

    public void SetFrames(Sprite[] newFrames)
    {
        frames = newFrames;
        frame = 0;
        timer = 0f;
        EnsureVisual();
        if (sr != null && frames != null && frames.Length > 0)
            sr.sprite = frames[0];
    }

    void Update()
    {
        if (frames == null || frames.Length == 0) return;
        if (sr == null) return;

        timer += Time.deltaTime;
        float step = 1f / Mathf.Max(1f, frameRate);
        if (timer >= step)
        {
            timer -= step;
            frame = (frame + 1) % frames.Length;
            sr.sprite = frames[frame];
        }
    }
}

