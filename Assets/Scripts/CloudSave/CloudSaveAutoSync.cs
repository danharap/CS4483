using System.Collections;
using UnityEngine;

/// <summary>
/// Pushes the current MainScene state to the active cloud save row (best-effort).
/// </summary>
public static class CloudSaveAutoSync
{
    public static void TrySyncNow()
    {
        if (!CloudSaveRuntime.IsCloudConfigured || string.IsNullOrEmpty(CloudSaveRuntime.ActiveSaveId))
            return;
        if (GameManager.Instance == null) return;

        CloudCoroutineHost.EnsureExists();
        CloudCoroutineHost.Instance.Run(SyncRoutine());
    }

    static IEnumerator SyncRoutine()
    {
        string token = null;
        string err = null;
        yield return CloudAuthBroker.EnsureAccess((t, e) => { token = t; err = e; });
        if (!string.IsNullOrEmpty(err) || string.IsNullOrEmpty(token)) yield break;

        GameSaveDocument doc;
        try
        {
            doc = GameSaveCapture.CaptureCurrentOrThrow();
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[CloudSaveAutoSync] Capture failed: " + e.Message);
            yield break;
        }

        var api = new SupabaseApi(CloudSaveRuntime.Config.supabaseUrl, CloudSaveRuntime.Config.supabaseAnonKey);
        bool ok = false;
        string e2 = null;
        // Slot label unchanged on autosync — fetch not needed; keep existing label server-side by PATCH without slot_label?
        // PATCH includes slot_label — pass empty to skip? PostgREST updates all fields in body.
        // Send current label from last known — for simplicity use "Autosave" or read from cache.
        string label = PlayerPrefs.GetString("Cloud_ActiveSlotLabel", "Save");
        yield return api.UpdateGameSave(token, CloudSaveRuntime.ActiveSaveId, label, doc, (success, er) => { ok = success; e2 = er; });

        if (!ok)
            Debug.LogWarning("[CloudSaveAutoSync] Upload failed: " + e2);
    }
}
