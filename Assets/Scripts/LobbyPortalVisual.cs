using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Replaces the lobby cube mesh with the stone portal sprite + dark backing.
/// Used from ProBuilderLevelBuilder (editor) and at runtime so existing scenes pick up art without manual edits.
/// </summary>
public static class LobbyPortalVisual
{
    public const string FrameChildName = "PortalVisual_Frame";
    public const string BlackChildName = "PortalVisual_Black";
    /// <summary>Child marker: portal root has had the wall punch-out offset applied (avoids double moves).</summary>
    public const string PosOffsetMarkerName = "PortalVisual_PosOffset_v1";

    static readonly Vector3 LobbyPortalWallOffset = new Vector3(0f, 0f, -0.35f);
    /// <summary>East hallway end wall: pull slightly into the corridor (-X) so the frame reads cleanly.</summary>
    static readonly Vector3 TutorialPortalWallOffset = new Vector3(-0.35f, 0f, 0f);

    static Sprite s_blackBacking;

    /// <summary>Shared black sprite for transparent portal centers.</summary>
    public static Sprite BlackBackingSprite
    {
        get
        {
            if (s_blackBacking != null) return s_blackBacking;
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.SetPixels(new[] { Color.black, Color.black, Color.black, Color.black });
            tex.Apply(false, true);
            s_blackBacking = Sprite.Create(tex, new Rect(0f, 0f, 2f, 2f), new Vector2(0.5f, 0.5f), 100f);
            return s_blackBacking;
        }
    }

    /// <summary>
    /// If <paramref name="stoneSprite"/> is null, does nothing (keeps cube).
    /// Safe to call multiple times.
    /// </summary>
    public static void ApplyIfNeeded(GameObject portalRoot, Sprite stoneSprite)
    {
        if (portalRoot == null || stoneFrameExists(portalRoot.transform)) return;

        var meshR = portalRoot.GetComponent<MeshRenderer>();
        if (meshR != null) meshR.enabled = false;

        if (stoneSprite == null)
        {
            Debug.LogWarning("[LobbyPortalVisual] Stone portal sprite missing — lobby portal stays invisible until import path is valid.");
            return;
        }

        float targetW = 2.55f;
        float targetH = 3.45f;
        float sx = targetW / Mathf.Max(0.001f, stoneSprite.bounds.size.x);
        float sy = targetH / Mathf.Max(0.001f, stoneSprite.bounds.size.y);
        float fit = Mathf.Min(sx, sy);

        GameObject frameGo = new GameObject(FrameChildName);
        frameGo.transform.SetParent(portalRoot.transform, false);
        frameGo.transform.localPosition = new Vector3(0f, 0f, -0.18f);
        frameGo.transform.localRotation = Quaternion.identity;
        frameGo.transform.localScale = new Vector3(fit, fit, 1f);
        frameGo.AddComponent<Billboard>();

        var frameSr = frameGo.AddComponent<SpriteRenderer>();
        frameSr.sprite = stoneSprite;
        frameSr.sortingOrder = 50;

        GameObject blackGo = new GameObject(BlackChildName);
        blackGo.transform.SetParent(frameGo.transform, false);
        blackGo.transform.localPosition = new Vector3(0f, 0.05f, 0.02f);
        blackGo.transform.localRotation = Quaternion.identity;
        blackGo.transform.localScale = new Vector3(0.42f, 0.52f, 1f);

        var blackSr = blackGo.AddComponent<SpriteRenderer>();
        blackSr.sprite = BlackBackingSprite;
        blackSr.color = Color.black;
        blackSr.sortingOrder = 49;

        ApplyWallPunchoutOffset(portalRoot);
        AlignTutorialTriggerToVisual(portalRoot);
    }

    /// <summary>
    /// Moves the portal root off the wall (once). Call when creating stone visuals or from migration.
    /// </summary>
    public static void ApplyWallPunchoutOffset(GameObject portalRoot)
    {
        if (portalRoot == null) return;
        if (portalRoot.transform.Find(PosOffsetMarkerName) != null) return;

        if (portalRoot.name == "LobbyToArena_Portal")
            portalRoot.transform.position += LobbyPortalWallOffset;
        else if (portalRoot.name == "TutorialToArena_Portal")
            portalRoot.transform.position += TutorialPortalWallOffset;
        else
            return;

        GameObject marker = new GameObject(PosOffsetMarkerName);
        marker.transform.SetParent(portalRoot.transform, false);
    }

    /// <summary>Scenes saved before punch-out: frame exists but root was never shifted.</summary>
    public static void MigrateLegacyWallPunchoutIfNeeded(GameObject portalRoot)
    {
        if (portalRoot == null) return;
        if (portalRoot.transform.Find(FrameChildName) == null) return;
        if (portalRoot.transform.Find(PosOffsetMarkerName) != null) return;
        ApplyWallPunchoutOffset(portalRoot);
    }

    static bool stoneFrameExists(Transform portalRoot)
    {
        return portalRoot.Find(FrameChildName) != null;
    }

    /// <summary>
    /// Keep TutorialToArena trigger centered on the visible stone frame even if only the visual child was moved in-scene.
    /// </summary>
    public static void AlignTutorialTriggerToVisual(GameObject portalRoot)
    {
        if (portalRoot == null) return;
        if (portalRoot.name != "TutorialToArena_Portal") return;
        if (portalRoot.GetComponent<TutorialExitPortal>() == null) return;

        Transform frame = portalRoot.transform.Find(FrameChildName);
        if (frame == null) return;

        BoxCollider box = portalRoot.GetComponent<BoxCollider>();
        if (box == null) return;

        // Match trigger center to the frame's current world position projected into portal local space.
        box.center = portalRoot.transform.InverseTransformPoint(frame.position);
        box.isTrigger = true;
    }
}

/// <summary>
/// Ensures <see cref="LobbyPortalVisual"/> runs for the lobby portal in saved scenes (no manual component add).
/// </summary>
static class LobbyPortalVisualBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Register()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Sprite stone = Resources.Load<Sprite>("portals/stone_portal_gate");
        if (stone == null) return;

        GameObject lobbyPortal = GameObject.Find("LobbyToArena_Portal");
        if (lobbyPortal != null)
        {
            LobbyPortalVisual.MigrateLegacyWallPunchoutIfNeeded(lobbyPortal);
            LobbyPortalVisual.ApplyIfNeeded(lobbyPortal, stone);
        }

        GameObject tutorialExit = GameObject.Find("TutorialToArena_Portal");
        if (tutorialExit != null)
        {
            // Source of truth is the scene-authored visual transform.
            // Do not auto-snap/migrate tutorial visual placement at load.
            if (tutorialExit.transform.Find(LobbyPortalVisual.FrameChildName) == null)
                LobbyPortalVisual.ApplyIfNeeded(tutorialExit, stone);

            // Pin to approved in-scene location.
            tutorialExit.transform.position = new Vector3(
                TutorialPrisonLayout.TutorialExitPortalCenterX,
                tutorialExit.transform.position.y,
                TutorialPrisonLayout.TutorialExitPortalCenterZ);

            LobbyPortalVisual.AlignTutorialTriggerToVisual(tutorialExit);
        }
    }
}
