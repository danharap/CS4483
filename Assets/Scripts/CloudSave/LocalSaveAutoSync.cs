using UnityEngine;

/// <summary>
/// Writes the active slot to local_accounts.json (best-effort, synchronous).
/// </summary>
public static class LocalSaveAutoSync
{
    public static void TrySyncNow()
    {
        if (!LocalSaveRuntime.IsSignedIn)
            return;
        if (GameManager.Instance == null) return;

        LocalAccountDatabase.SaveAccountProfileFromRuntime(LocalSaveRuntime.ActiveUserId);

        if (string.IsNullOrEmpty(LocalSaveRuntime.ActiveSaveId))
            return;

        GameSaveDocument doc;
        try
        {
            doc = GameSaveCapture.CaptureCurrentOrThrow();
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[LocalSaveAutoSync] Capture failed: " + e.Message);
            return;
        }

        string label = PlayerPrefs.GetString("Local_ActiveSlotLabel", "Save");
        if (!LocalAccountDatabase.TryUpdateSave(
                LocalSaveRuntime.ActiveUserId,
                LocalSaveRuntime.ActiveSaveId,
                label,
                doc,
                out string err))
        {
            Debug.LogWarning("[LocalSaveAutoSync] " + err);
        }
    }
}
