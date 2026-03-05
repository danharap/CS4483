using System.Collections;
using UnityEngine;

/// <summary>
/// Gate door that blocks the boss arena pocket.
/// Opens (moves upward out of the way) when a boss wave starts.
/// Closes again when the boss is defeated (next wave begins).
/// </summary>
public class GateDoor : MonoBehaviour
{
    // ── Tunables ──────────────────────────────────────────────────────────
    [Header("Door Movement")]
    [SerializeField] private float openHeight    = 5f;   // how far to raise (Y)
    [SerializeField] private float openSpeed     = 3f;
    [SerializeField] private bool  startClosed   = true;

    // ── State ─────────────────────────────────────────────────────────────
    private Vector3 closedPosition;
    private Vector3 openPosition;
    private bool    isOpen;

    void Awake()
    {
        closedPosition = transform.position;
        openPosition   = closedPosition + Vector3.up * openHeight;
        if (!startClosed) Open();
    }

    void Start()
    {
        var wm = GameManager.Instance?.WaveManager;
        if (wm != null)
        {
            wm.OnBossSpawned += Open;
            wm.OnBossKilled  += Close;
        }
    }

    void OnDestroy()
    {
        var wm = GameManager.Instance?.WaveManager;
        if (wm != null)
        {
            wm.OnBossSpawned -= Open;
            wm.OnBossKilled  -= Close;
        }
    }

    public void Open()
    {
        if (isOpen) return;
        isOpen = true;
        StopAllCoroutines();
        StartCoroutine(MoveDoor(openPosition));
    }

    public void Close()
    {
        if (!isOpen) return;
        isOpen = false;
        StopAllCoroutines();
        StartCoroutine(MoveDoor(closedPosition));
    }

    private IEnumerator MoveDoor(Vector3 target)
    {
        while (Vector3.Distance(transform.position, target) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(
                transform.position, target, openSpeed * Time.deltaTime);
            yield return null;
        }
        transform.position = target;
    }
}
