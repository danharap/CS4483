using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Runs coroutines for static cloud APIs (no scene dependency).
/// </summary>
public class CloudCoroutineHost : MonoBehaviour
{
    public static CloudCoroutineHost Instance { get; private set; }

    public static void EnsureExists()
    {
        if (Instance != null) return;
        var go = new GameObject("CloudCoroutineHost");
        DontDestroyOnLoad(go);
        Instance = go.AddComponent<CloudCoroutineHost>();
    }

    public void Run(IEnumerator routine)
    {
        StartCoroutine(routine);
    }

    public void Run(IEnumerator routine, Action onFinally)
    {
        StartCoroutine(Wrap(routine, onFinally));
    }

    private IEnumerator Wrap(IEnumerator inner, Action onFinally)
    {
        yield return StartCoroutine(inner);
        onFinally?.Invoke();
    }
}
