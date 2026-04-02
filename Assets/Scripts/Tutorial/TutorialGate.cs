using UnityEngine;

/// <summary>
/// Simple gate controller for tutorial sections.
/// Closed = collider blocks hallway, Open = collider disabled and gate lifts up.
/// </summary>
public class TutorialGate : MonoBehaviour
{
    [SerializeField] private Collider blockingCollider;
    [SerializeField] private Transform visualRoot;
    [SerializeField] private float openRaiseY = 4f;
    [SerializeField] private bool startOpen;

    private Vector3 closedLocalPos;

    void Awake()
    {
        if (blockingCollider == null)
            blockingCollider = GetComponent<Collider>();
        if (visualRoot == null)
            visualRoot = transform;

        closedLocalPos = visualRoot.localPosition;

        if (startOpen) OpenGate();
        else CloseGate();
    }

    public void OpenGate()
    {
        if (blockingCollider != null) blockingCollider.enabled = false;
        if (visualRoot != null) visualRoot.localPosition = closedLocalPos + Vector3.up * openRaiseY;
        foreach (Renderer r in GetComponentsInChildren<Renderer>(true))
            r.enabled = false;
    }

    public void CloseGate()
    {
        if (blockingCollider != null) blockingCollider.enabled = true;
        if (visualRoot != null) visualRoot.localPosition = closedLocalPos;
        foreach (Renderer r in GetComponentsInChildren<Renderer>(true))
            r.enabled = true;
    }
}

