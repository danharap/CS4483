using UnityEngine;

/// <summary>
/// Runtime theme switcher between Arena 1 and Arena 2.
/// Swaps the floor sprite and zap-trap animation frames when transitioning through the portal.
/// </summary>
public class ArenaThemeController : MonoBehaviour
{
    [Header("Floor")]
    public Sprite arena1FloorSprite;
    public Sprite arena2FloorSprite;

    [Header("Background")]
    public Sprite arena1BgSprite;
    public Sprite arena2BgSprite;

    [Header("Zap Trap Animations")]
    public Sprite[] zapTrapBlueFrames;
    public Sprite[] zapTrapRedFrames;

    private SpriteRenderer floorRenderer;
    private SpriteRenderer bgRenderer;

    void Start()
    {
        floorRenderer = GameObject.Find("Floor_Map")?.GetComponent<SpriteRenderer>();
        bgRenderer = GameObject.Find("Background_Plane")?.GetComponent<SpriteRenderer>();
        ApplyArena1Theme();
    }

    public void ApplyArena1Theme()
    {
        if (floorRenderer != null && arena1FloorSprite != null)
            floorRenderer.sprite = arena1FloorSprite;

        if (bgRenderer != null && arena1BgSprite != null)
            bgRenderer.sprite = arena1BgSprite;

        ApplyTrapFrames(false);
    }

    public void ApplyArena2Theme()
    {
        if (floorRenderer != null && arena2FloorSprite != null)
            floorRenderer.sprite = arena2FloorSprite;

        if (bgRenderer != null && arena2BgSprite != null)
            bgRenderer.sprite = arena2BgSprite;

        ApplyTrapFrames(true);
    }

    void ApplyTrapFrames(bool arena2)
    {
        Sprite[] frames = arena2 ? zapTrapRedFrames : zapTrapBlueFrames;
        if (frames == null || frames.Length == 0) return;

        foreach (ArenaTrap trap in FindObjectsByType<ArenaTrap>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Transform visual = trap.transform.Find("TrapVisual");
            if (visual == null) continue;
            TrapSpriteAnimator anim = visual.GetComponent<TrapSpriteAnimator>();
            if (anim == null) continue;
            anim.SetFrames(frames);
        }
    }
}

