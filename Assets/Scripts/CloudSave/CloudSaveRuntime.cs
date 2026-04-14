using System;
using UnityEngine;

/// <summary>
/// Cross-scene state for the active cloud save session.
/// </summary>
public static class CloudSaveRuntime
{
    public const string PREF_REFRESH = "Cloud_RefreshToken";
    public const string PREF_ACCESS = "Cloud_AccessToken";
    public const string PREF_ACCESS_EXPIRES = "Cloud_AccessExpiresUnix";
    public const string PREF_USER_EMAIL = "Cloud_UserEmail";

    public static CloudBackendConfig Config { get; private set; }
    public static bool IsCloudConfigured => Config != null && Config.IsConfigured;

    /// <summary>Supabase game_saves row id for the slot currently being played.</summary>
    public static string ActiveSaveId { get; set; }

    /// <summary>Applied once when MainScene loads.</summary>
    public static GameSaveDocument PendingHydrate { get; set; }

    public static void SetConfig(CloudBackendConfig c) => Config = c;

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

    public static void StoreTokens(string accessToken, int expiresInSeconds, string refreshToken)
    {
        if (!string.IsNullOrEmpty(accessToken))
            PlayerPrefs.SetString(PREF_ACCESS, accessToken);
        if (!string.IsNullOrEmpty(refreshToken))
            PlayerPrefs.SetString(PREF_REFRESH, refreshToken);
        long exp = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + Math.Max(0, expiresInSeconds - 60);
        PlayerPrefs.SetString(PREF_ACCESS_EXPIRES, exp.ToString());
        PlayerPrefs.Save();
    }

    public static string LoadAccessToken() => PlayerPrefs.GetString(PREF_ACCESS, "");
    public static string LoadRefreshToken() => PlayerPrefs.GetString(PREF_REFRESH, "");
    public static bool AccessTokenLikelyExpired()
    {
        if (!long.TryParse(PlayerPrefs.GetString(PREF_ACCESS_EXPIRES, "0"), out long exp))
            return true;
        return DateTimeOffset.UtcNow.ToUnixTimeSeconds() >= exp;
    }

    public static void ClearSession()
    {
        PlayerPrefs.DeleteKey(PREF_ACCESS);
        PlayerPrefs.DeleteKey(PREF_REFRESH);
        PlayerPrefs.DeleteKey(PREF_ACCESS_EXPIRES);
        PlayerPrefs.DeleteKey(PREF_USER_EMAIL);
        PlayerPrefs.Save();
        ActiveSaveId = null;
        PendingHydrate = null;
    }

    public static void SetUserEmail(string email)
    {
        if (!string.IsNullOrEmpty(email))
            PlayerPrefs.SetString(PREF_USER_EMAIL, email);
        PlayerPrefs.Save();
    }

    public static string LoadUserEmail() => PlayerPrefs.GetString(PREF_USER_EMAIL, "");
}
