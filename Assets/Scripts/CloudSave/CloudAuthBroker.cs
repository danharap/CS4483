using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Refreshes Supabase access tokens when needed.
/// </summary>
public static class CloudAuthBroker
{
    public static IEnumerator EnsureAccess(Action<string, string> done)
    {
        if (CloudSaveRuntime.Config == null || !CloudSaveRuntime.Config.IsConfigured)
        {
            done(null, "Cloud backend not configured.");
            yield break;
        }

        string existing = CloudSaveRuntime.LoadAccessToken();
        if (!string.IsNullOrEmpty(existing) && !CloudSaveRuntime.AccessTokenLikelyExpired())
        {
            done(existing, null);
            yield break;
        }

        string refresh = CloudSaveRuntime.LoadRefreshToken();
        if (string.IsNullOrEmpty(refresh))
        {
            done(null, "Session expired. Please sign in again.");
            yield break;
        }

        var api = new SupabaseApi(CloudSaveRuntime.Config.supabaseUrl, CloudSaveRuntime.Config.supabaseAnonKey);
        SupabaseAuthResponse auth = null;
        string err = null;
        yield return api.RefreshToken(refresh, (a, e) => { auth = a; err = e; });

        if (!string.IsNullOrEmpty(err) || auth == null || string.IsNullOrEmpty(auth.AccessToken))
        {
            CloudSaveRuntime.ClearSession();
            done(null, string.IsNullOrEmpty(err) ? "Session refresh failed." : err);
            yield break;
        }

        CloudSaveRuntime.StoreTokens(auth.AccessToken, auth.ExpiresIn, auth.RefreshToken ?? refresh);
        done(auth.AccessToken, null);
    }
}
