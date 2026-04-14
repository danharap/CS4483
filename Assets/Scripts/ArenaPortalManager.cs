using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Unity.AI.Navigation;

/// <summary>
/// Manages the stage 1 → 2 portal transition.
///
/// Flow:
///   1. Wave 5 boss dies → SpawnPortal() called
///   2. Portal scale-in animation plays at top of arena
///   3. Player walks in → FadeOut → arena swap → FadeIn
///   4. Arrival portal appears in arena 2 and auto-despawns after arrivalPortalLifetime
///   5. Player input re-enabled
///
/// F5 in play mode skips the wave requirement for testing.
/// </summary>
public class ArenaPortalManager : MonoBehaviour
{
    public static ArenaPortalManager Instance { get; private set; }

    // ── Arena refs ────────────────────────────────────────────────────────

    [Header("Arena Roots")]
    [SerializeField] private GameObject arena1Root;
    [SerializeField] private GameObject arena2Root;

    [Header("References")]
    [SerializeField] private EnemySpawner spawner;

    // ── Portal spawn ──────────────────────────────────────────────────────

    [Header("Portal – Spawn")]
    [Tooltip("Where the portal appears in Arena 1. Top-middle of the arena, away from player.")]
    [SerializeField] private Vector3 portalSpawnPosition = new Vector3(0f, 0.5f, 14f);
    [Tooltip("Seconds after boss death before the portal appears.")]
    [SerializeField] private float portalSpawnDelay = 1.2f;
    [Tooltip("Seconds the portal takes to scale in on appearance.")]
    [SerializeField] private float portalScaleInDuration = 0.7f;

    // ── Portal visuals ────────────────────────────────────────────────────

    [Header("Portal – Visuals")]
    [SerializeField] private Vector3 portalScale   = new Vector3(3.5f, 3.5f, 0.3f);
    [SerializeField] private Color   portalColor   = new Color(0.35f, 0.05f, 0.6f, 1f);
    [SerializeField] private Color   portalGlow    = new Color(0.6f,  0.0f, 1.0f, 1f);
    // ── Transition ────────────────────────────────────────────────────────

    [Header("Transition")]
    [SerializeField] private float fadeOutDuration  = 0.7f;
    [SerializeField] private float fadeInDuration   = 0.8f;
    [Tooltip("Extra black-screen hold between fade-out and arena swap (feels more dramatic).")]
    [SerializeField] private float blackHoldDuration = 0.3f;
    [Tooltip("Player input is locked for this long after arrival.")]
    [SerializeField] private float arrivalLockDuration = 1.5f;

    // ── Arena 2 arrival ───────────────────────────────────────────────────

    [Header("Arena 2 – Arrival")]
    [Tooltip("Where the player appears in Arena 2. Slightly below the arrival portal.")]
    [SerializeField] private Vector3 arena2PlayerSpawn = new Vector3(0f, 1.1f, 10f);
    [Tooltip("Where the arrival portal spawns in Arena 2 (player emerges from here).")]
    [SerializeField] private Vector3 arena2PortalSpawn = new Vector3(0f, 0.5f, 14f);
    [Tooltip("Seconds before the arrival portal disappears.")]
    [SerializeField] private float arrivalPortalLifetime = 3f;

    // ── Audio ─────────────────────────────────────────────────────────────

    [Header("Audio")]
    [SerializeField] private AudioClip portalTransitionSound;
    [SerializeField] private AudioClip portalOpenSound;

    // ── Internal ──────────────────────────────────────────────────────────

    private GameObject departurePortal;
    private GameObject arrivalPortal;
    private bool       transitionLocked;
    // Once the player has crossed into Arena 2 the departure portal must never reappear.
    private bool       portalUsed;

    private const string Arena1Name = "=== LEVEL (ProBuilder) ===";
    private const string Arena2Name = "=== LEVEL (ProBuilder) Arena2 ===";
    private const int    PortalAfterWaveIndex = 4; // 0-based: after wave 5

    private const string NetherPortalSpriteResource = "portals/vecteezy_pixel-art-stone-portal-with-glowing-light_72637298";

    // ─────────────────────────────────────────────────────────────────────
    #region Unity Lifecycle

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (arena1Root == null) arena1Root = FindRootByName(Arena1Name);
        if (arena2Root == null) arena2Root = FindRootByName(Arena2Name);
        if (spawner    == null) spawner    = FindFirstObjectByType<EnemySpawner>();

        WaveManager wm = GameManager.Instance?.WaveManager;
        if (wm != null) wm.OnWaveCleared += OnWaveCleared;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        var wm = GameManager.Instance?.WaveManager;
        if (wm != null) wm.OnWaveCleared -= OnWaveCleared;
    }

    void Update()
    {
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.F5)) SpawnPortalForDebug();
#endif
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────
    #region Portal Spawn

    private void OnWaveCleared(int waveIndex)
    {
        // Only spawn portal once — never again after the player has used it.
        if (!portalUsed && waveIndex == PortalAfterWaveIndex)
            StartCoroutine(DelayedPortalSpawn());
    }

    public void SpawnPortalForDebug() => StartCoroutine(DelayedPortalSpawn(0f));

    private IEnumerator DelayedPortalSpawn(float overrideDelay = -1f)
    {
        if (departurePortal != null) yield break; // already spawned

        float delay = overrideDelay >= 0f ? overrideDelay : portalSpawnDelay;
        yield return new WaitForSeconds(delay);

        if (portalOpenSound != null)
            AudioSource.PlayClipAtPoint(portalOpenSound, portalSpawnPosition, 0.9f);

        departurePortal = BuildPortalObject("Portal_Departure", portalSpawnPosition);
        yield return StartCoroutine(ScaleIn(departurePortal, UniformPortalScale(), portalScaleInDuration));
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────
    #region Transition

    /// <summary>Called by Portal trigger when player enters.</summary>
    public void TransitionToArena2()
    {
        if (transitionLocked) return;
        transitionLocked = true;
        StartCoroutine(TransitionRoutine());
    }

    /// <summary>
    /// Called on respawn to reset the portal manager back to its initial state
    /// so the stage 1 → 2 transition can be used again in the new run.
    /// </summary>
    public void ResetForRespawn()
    {
        transitionLocked = false;
        portalUsed = false;

        // Clean up any in-flight portal objects
        if (departurePortal != null)
        {
            CleanupPortalPhysics(departurePortal);
            Destroy(departurePortal);
            departurePortal = null;
        }
        if (arrivalPortal != null)
        {
            CleanupPortalPhysics(arrivalPortal);
            Destroy(arrivalPortal);
            arrivalPortal = null;
        }

        StopAllCoroutines();
    }

    private IEnumerator TransitionRoutine()
    {
        // Lock player input during transition
        PlayerController pc = GameManager.Instance?.PlayerController;
        if (pc != null) pc.enabled = false;

        if (portalTransitionSound != null)
            AudioSource.PlayClipAtPoint(portalTransitionSound, portalSpawnPosition, 1f);

        // Fade to black
        ScreenFadeController fade = ScreenFadeController.Instance;
        if (fade != null)
        {
            bool done = false;
            fade.FadeOut(fadeOutDuration, () => done = true);
            yield return new WaitUntil(() => done);
        }

        yield return new WaitForSeconds(blackHoldDuration);

        // ── Swap arenas ───────────────────────────────────────────────────
        if (arena1Root == null) arena1Root = FindRootByName(Arena1Name);
        if (arena2Root == null) arena2Root = FindRootByName(Arena2Name);

        if (arena2Root == null)
        {
            Debug.LogWarning("[ArenaPortalManager] Arena 2 not found — transition aborted.");
            if (pc != null) pc.enabled = true;
            transitionLocked = false;
            yield break;
        }

        // ── Step 1: Disable every Arena 1 root (handles duplicate refs) ─────
        SetAllSceneRootsActive(Arena1Name, false);

        // Step 2: Explicitly suppress any Floor_Map / Background_Plane renderers
        // that might be floating at scene-root level (outside any arena hierarchy).
        // This prevents stale Stage 1 floor sprites from showing through Stage 2.
        SuppressStrayFloorRenderers(arena2Root);

        // Step 3: Activate Arena 2 and apply its floor theme BEFORE fading in.
        arena2Root.SetActive(true);

        // Apply Arena 2 floor/theme now, while the screen is still black.
        // Include inactive — MANAGERS may be inactive in some editor setups.
        ArenaThemeController[] themes = FindObjectsByType<ArenaThemeController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (themes != null && themes.Length > 0)
            themes[0].ApplyArena2Theme();
        else
            Debug.LogWarning("[ArenaPortalManager] No ArenaThemeController found — Stage 2 floor may not swap.");

        // Bake NavMesh for Arena 2.
        // IMPORTANT: force CollectObjects.Children so the player's CharacterController capsule
        // (and any other scene-level colliders) are excluded from the bake.  Using .All would
        // carve a player-shaped hole in the NavMesh at the portal area, causing enemies to stop
        // dead whenever they tried to path toward that region.
        NavMeshSurface nav = arena2Root.GetComponent<NavMeshSurface>();
        if (nav != null)
        {
            nav.collectObjects = CollectObjects.Children;
            nav.useGeometry    = NavMeshCollectGeometry.PhysicsColliders;
            nav.BuildNavMesh();
            Debug.Log("[ArenaPortalManager] Arena 2 NavMesh baked (CollectObjects.Children).");
        }
        else
            Debug.LogWarning("[ArenaPortalManager] No NavMeshSurface on Arena 2 root — enemies may not navigate.");

        // Move player to arrival spot
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            player.transform.position = arena2PlayerSpawn;
            PlayerHealth ph = player.GetComponent<PlayerHealth>();
            if (ph != null)
                ph.ForceIFrames(fadeInDuration + arrivalLockDuration);
        }

        // Remove departure portal — disable physics first so no residual collider/obstacle lingers
        if (departurePortal != null)
        {
            CleanupPortalPhysics(departurePortal);
            Destroy(departurePortal);
            departurePortal = null;
        }

        // Arrival portal appears in Arena 2
        arrivalPortal = BuildPortalObject("Portal_Arrival", arena2PortalSpawn);
        arrivalPortal.transform.localScale = UniformPortalScale(); // no scale-in anim needed
        StartCoroutine(DespawnArrivalPortal(arrivalPortalLifetime));

        // Switch wave spawner to Arena 2 spawn points
        if (spawner != null) spawner.UseArena2Spawns();

        // Mark portal as used — prevents it from ever respawning this run.
        portalUsed = true;

        // Update HUD to show current wave; do NOT call SetWaveIndex — the WaveLoop is already
        // counting correctly and resetting it causes subsequent OnWaveCleared calls to fire
        // with waveIndex 4 again, re-triggering the portal and breaking the Satan wave trigger.
        GameManager.Instance?.HUD?.UpdateWaveNumber((GameManager.Instance?.WaveManager?.WaveIndex ?? 4) + 1);

        // ── Fade back in ──────────────────────────────────────────────────
        if (fade != null)
        {
            bool done = false;
            fade.FadeIn(fadeInDuration, () => done = true);
            yield return new WaitUntil(() => done);
        }

        // Show arrival message
        GameManager.Instance?.HUD?.ShowTransition("You enter the Nether Colosseum…");

        GameplayMusicController.Instance?.PlayArena2();

        // Briefly lock movement so the arrival moment reads cleanly, then unlock
        yield return new WaitForSeconds(arrivalLockDuration);
        if (pc != null) pc.enabled = true;
    }

    private IEnumerator DespawnArrivalPortal(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (arrivalPortal == null) yield break;

        // Disable physics immediately so enemies are not blocked during the visual scale-out
        CleanupPortalPhysics(arrivalPortal);

        // Quickly scale out then destroy
        float t = 0f;
        float dur = 0.4f;
        Vector3 full = arrivalPortal.transform.localScale;
        while (t < dur)
        {
            t += Time.deltaTime;
            float u = 1f - Mathf.Clamp01(t / dur);
            arrivalPortal.transform.localScale = full * u;
            yield return null;
        }
        Destroy(arrivalPortal);
        arrivalPortal = null;
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────
    #region Portal Visual Helpers

    Vector3 UniformPortalScale()
    {
        float u = Mathf.Max(portalScale.x, portalScale.y);
        return new Vector3(u, u, u);
    }

    static Material CreatePortalDiscMaterial(Color baseColor, Color emissionColor)
    {
        Shader sh = Shader.Find("Universal Render Pipeline/Lit");
        if (sh == null) sh = Shader.Find("Standard");
        Material mat = new Material(sh);
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", baseColor);
        else
            mat.color = baseColor;
        if (mat.HasProperty("_Smoothness"))
            mat.SetFloat("_Smoothness", 0f);
        if (mat.HasProperty("_Metallic"))
            mat.SetFloat("_Metallic", 0f);
        if (mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", emissionColor);
        }
        return mat;
    }

    /// <summary>Billboarded sprite + dark backing; trigger child unchanged for <see cref="Portal"/>.</summary>
    private GameObject BuildPortalObject(string goName, Vector3 position)
    {
        GameObject portal = new GameObject(goName);
        portal.transform.rotation = Quaternion.identity;
        portal.transform.localScale = Vector3.zero; // scale-in / pulse on root

        Sprite nether = Resources.Load<Sprite>(NetherPortalSpriteResource);
        if (nether != null)
        {
            const float targetH = 3.1f;
            float fit = targetH / Mathf.Max(0.001f, nether.bounds.size.y);
            float worldH = nether.bounds.size.y * fit;
            float centerY = worldH * 0.5f;
            portal.transform.position = new Vector3(position.x, centerY, position.z);

            GameObject frameGo = new GameObject("Portal_Frame");
            frameGo.transform.SetParent(portal.transform, false);
            frameGo.transform.localPosition = Vector3.zero;
            frameGo.AddComponent<Billboard>();

            frameGo.transform.localScale = new Vector3(fit, fit, 1f);

            SpriteRenderer sr = frameGo.AddComponent<SpriteRenderer>();
            sr.sprite = nether;
            sr.sortingOrder = 80;

            GameObject blackGo = new GameObject("Portal_Black");
            blackGo.transform.SetParent(frameGo.transform, false);
            blackGo.transform.localPosition = new Vector3(0f, 0.05f, 0.02f);
            blackGo.transform.localScale = new Vector3(0.42f, 0.52f, 1f);
            SpriteRenderer bsr = blackGo.AddComponent<SpriteRenderer>();
            bsr.sprite = LobbyPortalVisual.BlackBackingSprite;
            bsr.color = Color.black;
            bsr.sortingOrder = 79;
        }
        else
        {
            Debug.LogWarning("[ArenaPortalManager] Nether portal sprite missing at Resources/" + NetherPortalSpriteResource + " — using cylinder fallback.");
            portal.transform.position = position;
            GameObject disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disc.name = "Portal_FallbackDisc";
            disc.transform.SetParent(portal.transform, false);
            disc.transform.localPosition = Vector3.zero;
            disc.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            disc.transform.localScale = Vector3.one;
            Destroy(disc.GetComponent<Collider>());
            Renderer rend = disc.GetComponent<Renderer>();
            if (rend != null)
                rend.material = CreatePortalDiscMaterial(portalColor, portalGlow * 1.2f);
        }

        GameObject triggerChild = new GameObject("Trigger");
        triggerChild.transform.SetParent(portal.transform, false);
        CapsuleCollider trigger = triggerChild.AddComponent<CapsuleCollider>();
        trigger.isTrigger = true;
        trigger.radius    = 1.8f;
        trigger.height    = 2f;
        trigger.direction = 1;

        triggerChild.AddComponent<Portal>();

        return portal;
    }

    private IEnumerator ScaleIn(GameObject go, Vector3 targetScale, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float u = Mathf.SmoothStep(0f, 1f, t / duration);
            go.transform.localScale = targetScale * u;
            yield return null;
        }
        go.transform.localScale = targetScale;
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────
    #region Helpers

    private static GameObject FindRootByName(string name)
    {
        Scene scene = SceneManager.GetActiveScene();
        foreach (GameObject root in scene.GetRootGameObjects())
            if (root.name == name) return root;
        return null;
    }

    /// <summary>Enable/disable every matching scene root (handles duplicate or missing serialized refs).</summary>
    static void SetAllSceneRootsActive(string rootName, bool active)
    {
        Scene scene = SceneManager.GetActiveScene();
        foreach (GameObject root in scene.GetRootGameObjects())
            if (root.name == rootName)
                root.SetActive(active);
    }

    /// <summary>
    /// Disable any SpriteRenderer named "Floor_Map" or "Background_Plane" that is NOT a
    /// descendant of <paramref name="validArena"/>. This catches objects that ended up at
    /// scene-root level through editor quirks, preventing Stage 1 floor from showing in Stage 2.
    /// </summary>
    static void SuppressStrayFloorRenderers(GameObject validArena)
    {
        if (validArena == null) return;
        foreach (SpriteRenderer sr in FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            string n = sr.gameObject.name;
            if (n != "Floor_Map" && n != "Background_Plane") continue;
            if (IsDescendantOf(sr.transform, validArena.transform)) continue;

            sr.enabled = false;
            Debug.Log($"[ArenaPortalManager] Suppressed stray floor renderer '{sr.gameObject.name}' " +
                      $"(parent: {(sr.transform.parent != null ? sr.transform.parent.name : "scene root")})");
        }

        // Solid black backdrop quads (replaces old sBg sprites)
        foreach (MeshRenderer mr in FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (mr.gameObject.name != "Background_Black") continue;
            if (IsDescendantOf(mr.transform, validArena.transform)) continue;
            mr.enabled = false;
            Debug.Log($"[ArenaPortalManager] Suppressed stray black backdrop '{mr.gameObject.name}'.");
        }
    }

    static bool IsDescendantOf(Transform child, Transform parent)
    {
        Transform t = child;
        while (t != null)
        {
            if (t == parent) return true;
            t = t.parent;
        }
        return false;
    }

    /// <summary>
    /// Disable all colliders (and any lingering Rigidbodies) on a portal before destroying it.
    /// This ensures no physics/NavMesh obstacle remains active during Unity's deferred Destroy frame.
    /// </summary>
    private static void CleanupPortalPhysics(GameObject portal)
    {
        if (portal == null) return;
        foreach (Collider col in portal.GetComponentsInChildren<Collider>(true))
            col.enabled = false;
        foreach (Rigidbody rb in portal.GetComponentsInChildren<Rigidbody>(true))
            rb.detectCollisions = false;
    }

    public WaveManager WaveManager => GameManager.Instance?.WaveManager;
    public HUDManager  HUD         => GameManager.Instance?.HUD;

    #endregion
}
