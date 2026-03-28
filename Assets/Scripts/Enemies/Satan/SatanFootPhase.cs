using System.Collections;
using UnityEngine;

/// <summary>
/// Manages Satan's second phase: the stomp sequence.
///
/// Behavior:
///   • Satan's body is hidden; only the foot sprite is used.
///   • A warning indicator appears where the next stomp will land.
///   • After the warning delay the foot slams down at that saved position.
///   • Impact applies AoE damage and triggers screen shake.
///   • The foot lingers briefly, then retracts before the next stomp.
///   • Repeats for configurable number of stomps, then starts a new sequence after a rest.
///
/// Reuses the LineRenderer circle-telegraph pattern from BossEnemy.
/// Attach to the Satan root GameObject. Wire in the Inspector.
/// </summary>
public class SatanFootPhase : MonoBehaviour
{
    // ── Tunables ──────────────────────────────────────────────────────────

    [Header("Foot Sprite")]
    [Tooltip("The sprite for Satan's foot. Assign the 'Satan Foot' sprite here.")]
    [SerializeField] private Sprite footSprite;
    [SerializeField] private float  footSpriteScale = 3f;
    [SerializeField] private float  footDescentHeight = 18f; // Y start height

    [Header("Stomp Sequence")]
    [SerializeField] private int   stompsPerSequence = 4;
    [SerializeField] private float warningDuration   = 1.2f;  // time warning shows before impact
    [SerializeField] private float delayBetweenStomps = 0.8f;
    [SerializeField] private float stompLingerDuration = 0.4f;
    [SerializeField] private float descentSpeed        = 25f;
    [SerializeField] private float retractSpeed        = 20f;

    [Header("Stomp Damage")]
    [SerializeField] private float stompDamage = 35f;
    [SerializeField] private float stompRadius = 2.5f;

    [Header("Screen Shake")]
    [SerializeField] private float shakeIntensity = 0.4f;
    [SerializeField] private float shakeDuration  = 0.3f;

    [Header("Sequences")]
    [SerializeField] private float timeBetweenSequences = 3f;

    [Header("Telegraph Line Renderer")]
    [SerializeField] private float telegraphLineWidth = 0.1f;
    [SerializeField] private Color telegraphColor = new Color(1f, 0.3f, 0.1f, 0.85f);

    // ── State ─────────────────────────────────────────────────────────────

    private SatanBossController boss;
    private Transform           player;
    private bool                active;

    private SpriteRenderer footRenderer;
    private GameObject     footObject;
    private LineRenderer   warningCircle;

    // ─────────────────────────────────────────────────────────────────────
    #region Public API

    public void Begin(SatanBossController controller)
    {
        boss   = controller;
        player = controller.Player;
        active = true;

        footObject = CreateFootVisual();
        warningCircle = CreateCircle("StompWarning", telegraphColor);
        warningCircle.enabled = false;

        StartCoroutine(StompLoop());
    }

    public void Stop()
    {
        active = false;
        StopAllCoroutines();
        if (footObject != null)    Destroy(footObject);
        if (warningCircle != null) Destroy(warningCircle.gameObject);
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────
    #region Stomp Loop

    private IEnumerator StompLoop()
    {
        while (active && boss != null && boss.IsAlive)
        {
            for (int i = 0; i < stompsPerSequence; i++)
            {
                if (!active) yield break;
                yield return StartCoroutine(SingleStomp());
                yield return new WaitForSeconds(delayBetweenStomps);
            }
            yield return new WaitForSeconds(timeBetweenSequences);
        }
    }

    private IEnumerator SingleStomp()
    {
        if (player == null) yield break;

        // Capture the player's position at warning time — not at impact
        Vector3 targetPos = new Vector3(player.position.x, 0f, player.position.z);

        // Show warning circle on the ground
        warningCircle.enabled = true;
        float warnTimer = 0f;
        while (warnTimer < warningDuration)
        {
            warnTimer += Time.deltaTime;
            float pulse = 0.6f + 0.4f * Mathf.Sin(warnTimer * Mathf.PI * 5f);
            DrawCircle(warningCircle, targetPos, stompRadius, pulse);
            yield return null;
        }
        warningCircle.enabled = false;

        // Position foot above target, off-screen
        if (footObject != null)
        {
            footObject.SetActive(true);
            footObject.transform.position = new Vector3(targetPos.x, footDescentHeight, targetPos.z);
        }

        // Descend
        float groundY = 0.5f;
        while (footObject != null && footObject.transform.position.y > groundY)
        {
            footObject.transform.position -= new Vector3(0f, descentSpeed * Time.deltaTime, 0f);
            yield return null;
        }
        if (footObject != null)
            footObject.transform.position = new Vector3(targetPos.x, groundY, targetPos.z);

        // Impact
        OnStompImpact(targetPos);

        // Linger
        yield return new WaitForSeconds(stompLingerDuration);

        // Retract
        if (footObject != null)
        {
            float target = footDescentHeight;
            while (footObject.transform.position.y < target)
            {
                footObject.transform.position += new Vector3(0f, retractSpeed * Time.deltaTime, 0f);
                yield return null;
            }
            footObject.SetActive(false);
        }
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────
    #region Impact

    private void OnStompImpact(Vector3 pos)
    {
        // AoE damage
        if (player != null)
        {
            float dist = Vector3.Distance(
                new Vector3(player.position.x, 0f, player.position.z),
                new Vector3(pos.x, 0f, pos.z));
            if (dist <= stompRadius)
                player.GetComponent<PlayerHealth>()?.TakeDamage(stompDamage);
        }

        // Screen shake via CameraController or direct impulse
        TriggerScreenShake();
    }

    private void TriggerScreenShake()
    {
        // Reuse existing CameraController if it has a shake method.
        // Since CameraController doesn't have one yet, we do a lightweight
        // coroutine-based shake directly on the camera.
        Camera cam = Camera.main;
        if (cam != null)
            StartCoroutine(ShakeCamera(cam, shakeIntensity, shakeDuration));
    }

    private IEnumerator ShakeCamera(Camera cam, float intensity, float duration)
    {
        Vector3 originalPos = cam.transform.position;
        float   elapsed     = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = 1f - (elapsed / duration);
            float x = Random.Range(-1f, 1f) * intensity * t;
            float z = Random.Range(-1f, 1f) * intensity * t;
            cam.transform.position = originalPos + new Vector3(x, 0f, z);
            yield return null;
        }
        cam.transform.position = originalPos;
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────
    #region Foot Visual

    private GameObject CreateFootVisual()
    {
        GameObject go = new GameObject("Satan_Foot");
        go.transform.SetParent(null); // world-space

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite       = footSprite;
        sr.sortingOrder = 20;

        // Billboard so it faces camera
        go.AddComponent<Billboard>();

        float scale = footSpriteScale;
        go.transform.localScale = new Vector3(scale, scale, 1f);
        go.SetActive(false);
        return go;
    }

    #endregion

    // ─────────────────────────────────────────────────────────────────────
    #region Telegraph Circle Helpers (mirrors BossEnemy pattern)

    private LineRenderer CreateCircle(string goName, Color color)
    {
        GameObject go = new GameObject(goName);
        go.transform.SetParent(null);

        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace  = true;
        lr.loop           = true;
        lr.positionCount  = 48;
        lr.startWidth     = telegraphLineWidth;
        lr.endWidth       = telegraphLineWidth;
        lr.material       = new Material(Shader.Find("Sprites/Default"));
        lr.startColor     = color;
        lr.endColor       = color;
        lr.sortingOrder   = 30;
        return lr;
    }

    private void DrawCircle(LineRenderer lr, Vector3 center, float radius, float alpha)
    {
        if (lr == null) return;
        Color c = lr.startColor;
        c.a = alpha;
        lr.startColor = c;
        lr.endColor   = c;

        float groundY = 0.08f;
        for (int i = 0; i < lr.positionCount; i++)
        {
            float a = (i / (float)lr.positionCount) * Mathf.PI * 2f;
            lr.SetPosition(i, new Vector3(
                center.x + Mathf.Cos(a) * radius,
                groundY,
                center.z + Mathf.Sin(a) * radius));
        }
    }

    #endregion
}
