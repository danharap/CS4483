using System.Collections;
using UnityEngine;
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
    [SerializeField] private float   portalPulseSpeed     = 1.8f;
    [SerializeField] private float   portalPulseAmplitude = 0.06f;

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

    private const string Arena1Name = "=== LEVEL (ProBuilder) ===";
    private const string Arena2Name = "=== LEVEL (ProBuilder) Arena2 ===";
    private const int    PortalAfterWaveIndex = 4; // 0-based: after wave 5

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
        if (waveIndex == PortalAfterWaveIndex)
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
        yield return StartCoroutine(ScaleIn(departurePortal, portalScale, portalScaleInDuration));
        StartCoroutine(PulsePortal(departurePortal));
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
        // Arena 1 is now disabled, so CollectObjects.All only picks up Arena 2 geometry.
        // If the surface uses CollectObjects.Children, only Arena 2's own colliders are included
        // regardless — either way, the bake is clean at this point.
        NavMeshSurface nav = arena2Root.GetComponent<NavMeshSurface>();
        if (nav != null)
        {
            nav.BuildNavMesh();
            Debug.Log("[ArenaPortalManager] Arena 2 NavMesh baked.");
        }
        else
            Debug.LogWarning("[ArenaPortalManager] No NavMeshSurface on Arena 2 root — enemies may not navigate.");

        // Move player to arrival spot
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            player.transform.position = arena2PlayerSpawn;

        // Remove departure portal — disable physics first so no residual collider/obstacle lingers
        if (departurePortal != null)
        {
            CleanupPortalPhysics(departurePortal);
            Destroy(departurePortal);
            departurePortal = null;
        }

        // Arrival portal appears in Arena 2
        arrivalPortal = BuildPortalObject("Portal_Arrival", arena2PortalSpawn);
        arrivalPortal.transform.localScale = portalScale; // no scale-in anim needed
        StartCoroutine(PulsePortal(arrivalPortal));
        StartCoroutine(DespawnArrivalPortal(arrivalPortalLifetime));

        // Switch wave spawner to Arena 2 spawn points
        if (spawner != null) spawner.UseArena2Spawns();

        // Advance wave counter
        WaveManager wm = GameManager.Instance?.WaveManager;
        if (wm != null)
        {
            wm.SetWaveIndex(4);
            GameManager.Instance?.HUD?.UpdateWaveNumber(5);
        }

        // ── Fade back in ──────────────────────────────────────────────────
        if (fade != null)
        {
            bool done = false;
            fade.FadeIn(fadeInDuration, () => done = true);
            yield return new WaitUntil(() => done);
        }

        // Show arrival message
        GameManager.Instance?.HUD?.ShowTransition("You enter the Nether Colosseum…");

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

    /// <summary>Single flat disc + simple URP-friendly material (no stacked meshes — avoids z-fighting / white seam).</summary>
    private GameObject BuildPortalObject(string goName, Vector3 position)
    {
        // Use a cylinder rotated flat (90° on X) as the portal disc
        GameObject portal = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        portal.name = goName;
        portal.transform.position = position;
        portal.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        portal.transform.localScale = Vector3.zero; // starts invisible (scale-in plays separately)

        // Remove collider — Portal.cs handles trigger detection on a separate child
        Destroy(portal.GetComponent<Collider>());

        Renderer rend = portal.GetComponent<Renderer>();
        if (rend != null)
            rend.material = CreatePortalDiscMaterial(portalColor, portalGlow * 1.2f);

        // Trigger collider on a separate child so Portal.cs can detect the player.
        // No Rigidbody here — the player's own Rigidbody is enough to fire OnTriggerEnter
        // on a static trigger collider.  Adding a kinematic Rigidbody to the portal would
        // register it as a NavMesh local-avoidance obstacle, causing enemies to steer around
        // the invisible portal area even after it disappears.
        GameObject triggerChild = new GameObject("Trigger");
        triggerChild.transform.SetParent(portal.transform, false);
        CapsuleCollider trigger = triggerChild.AddComponent<CapsuleCollider>();
        trigger.isTrigger = true;
        trigger.radius    = 1.8f;
        trigger.height    = 2f;
        trigger.direction = 1; // Y-axis

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

    private IEnumerator PulsePortal(GameObject go)
    {
        while (go != null)
        {
            float pulse = 1f + Mathf.Sin(Time.time * portalPulseSpeed) * portalPulseAmplitude;
            if (go != null) go.transform.localScale = portalScale * pulse;
            yield return null;
        }
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

            // Disable the renderer so the sprite can no longer show, but leave the object
            // in place so the hierarchy reference isn't broken.
            sr.enabled = false;
            Debug.Log($"[ArenaPortalManager] Suppressed stray floor renderer '{sr.gameObject.name}' " +
                      $"(parent: {(sr.transform.parent != null ? sr.transform.parent.name : "scene root")})");
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
