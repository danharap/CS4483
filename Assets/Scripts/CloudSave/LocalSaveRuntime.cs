using UnityEngine;

/// <summary>
/// Session + pending load for the local JSON account system.
/// </summary>
public static class LocalSaveRuntime
{
    const string PrefUserId = "LocalAccount_UserId";
    const string PrefEmail = "LocalAccount_Email";

    public static string ActiveUserId
    {
        get => PlayerPrefs.GetString(PrefUserId, "");
        set
        {
            if (string.IsNullOrEmpty(value)) PlayerPrefs.DeleteKey(PrefUserId);
            else PlayerPrefs.SetString(PrefUserId, value);
            PlayerPrefs.Save();
        }
    }

    public static string ActiveSaveId { get; set; }

    public static GameSaveDocument PendingHydrate { get; set; }

    public static void SetRememberedEmail(string email)
    {
        if (!string.IsNullOrEmpty(email)) PlayerPrefs.SetString(PrefEmail, email);
        PlayerPrefs.Save();
    }

    public static string LoadRememberedEmail() => PlayerPrefs.GetString(PrefEmail, "");

    public static bool IsSignedIn => !string.IsNullOrEmpty(ActiveUserId);

    public static void SignOut()
    {
        PlayerPrefs.DeleteKey(PrefUserId);
        PlayerPrefs.Save();
        ActiveSaveId = null;
        PendingHydrate = null;
    }

    public static bool TryConsumePendingHydrate(out GameSaveDocument doc)
    {
        if (PendingHydrate == null)
        {
            doc = null;
            return false;
        }
        doc = PendingHydrate;
        PendingHydrate = null;
        return true;
    }
}
