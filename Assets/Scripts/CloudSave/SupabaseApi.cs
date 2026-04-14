using System;
using System.Collections;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Minimal Supabase Auth + PostgREST client for UnityWebRequest.
/// </summary>
public class SupabaseApi
{
    readonly string _url;
    readonly string _anonKey;

    public SupabaseApi(string supabaseUrl, string anonKey)
    {
        _url = supabaseUrl.TrimEnd('/');
        _anonKey = anonKey;
    }

    public IEnumerator SignUp(string email, string password, Action<SupabaseAuthResponse, string> done)
    {
        string body = JsonConvert.SerializeObject(new { email, password });
        using var req = new UnityWebRequest($"{_url}/auth/v1/signup", "POST");
        byte[] raw = Encoding.UTF8.GetBytes(body);
        req.uploadHandler = new UploadHandlerRaw(raw);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        req.SetRequestHeader("apikey", _anonKey);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            done(null, ExtractError(req));
            yield break;
        }

        var txt = req.downloadHandler.text;
        try
        {
            var auth = JsonConvert.DeserializeObject<SupabaseAuthResponse>(txt);
            done(auth, null);
        }
        catch (Exception e)
        {
            done(null, e.Message);
        }
    }

    public IEnumerator SignIn(string email, string password, Action<SupabaseAuthResponse, string> done)
    {
        string body = JsonConvert.SerializeObject(new { email, password });
        using var req = new UnityWebRequest($"{_url}/auth/v1/token?grant_type=password", "POST");
        byte[] raw = Encoding.UTF8.GetBytes(body);
        req.uploadHandler = new UploadHandlerRaw(raw);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        req.SetRequestHeader("apikey", _anonKey);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            done(null, ExtractError(req));
            yield break;
        }

        try
        {
            var auth = JsonConvert.DeserializeObject<SupabaseAuthResponse>(req.downloadHandler.text);
            done(auth, null);
        }
        catch (Exception e)
        {
            done(null, e.Message);
        }
    }

    public IEnumerator RefreshToken(string refreshToken, Action<SupabaseAuthResponse, string> done)
    {
        string body = JsonConvert.SerializeObject(new { refresh_token = refreshToken });
        using var req = new UnityWebRequest($"{_url}/auth/v1/token?grant_type=refresh_token", "POST");
        byte[] raw = Encoding.UTF8.GetBytes(body);
        req.uploadHandler = new UploadHandlerRaw(raw);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        req.SetRequestHeader("apikey", _anonKey);
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            done(null, ExtractError(req));
            yield break;
        }

        try
        {
            var auth = JsonConvert.DeserializeObject<SupabaseAuthResponse>(req.downloadHandler.text);
            done(auth, null);
        }
        catch (Exception e)
        {
            done(null, e.Message);
        }
    }

    public IEnumerator ListGameSaves(string accessToken, Action<GameSaveRowDto[], string> done)
    {
        using var req = UnityWebRequest.Get($"{_url}/rest/v1/game_saves?select=*&order=updated_at.desc");
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("apikey", _anonKey);
        req.SetRequestHeader("Authorization", $"Bearer {accessToken}");
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            done(null, ExtractError(req));
            yield break;
        }

        try
        {
            var rows = JsonConvert.DeserializeObject<GameSaveRowDto[]>(req.downloadHandler.text);
            done(rows ?? Array.Empty<GameSaveRowDto>(), null);
        }
        catch (Exception e)
        {
            done(null, e.Message);
        }
    }

    public IEnumerator CreateGameSave(string accessToken, string slotLabel, GameSaveDocument doc, Action<GameSaveRowDto, string> done)
    {
        var payloadObj = JObject.Parse(GameSaveSerializer.ToJson(doc));
        var row = new JObject
        {
            ["slot_label"] = slotLabel,
            ["payload"] = payloadObj,
            ["payload_version"] = doc.version
        };
        string body = row.ToString(Formatting.None);

        using var req = new UnityWebRequest($"{_url}/rest/v1/game_saves", "POST");
        byte[] raw = Encoding.UTF8.GetBytes(body);
        req.uploadHandler = new UploadHandlerRaw(raw);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        req.SetRequestHeader("apikey", _anonKey);
        req.SetRequestHeader("Authorization", $"Bearer {accessToken}");
        req.SetRequestHeader("Prefer", "return=representation");
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            done(null, ExtractError(req));
            yield break;
        }

        try
        {
            var arr = JsonConvert.DeserializeObject<GameSaveRowDto[]>(req.downloadHandler.text);
            if (arr != null && arr.Length > 0)
                done(arr[0], null);
            else
                done(null, "Empty response from create save.");
        }
        catch (Exception e)
        {
            done(null, e.Message);
        }
    }

    public IEnumerator UpdateGameSave(string accessToken, string rowId, string slotLabel, GameSaveDocument doc, Action<bool, string> done)
    {
        var payloadObj = JObject.Parse(GameSaveSerializer.ToJson(doc));
        var patch = new JObject
        {
            ["slot_label"] = slotLabel,
            ["payload"] = payloadObj,
            ["payload_version"] = doc.version
        };
        string body = patch.ToString(Formatting.None);

        using var req = new UnityWebRequest($"{_url}/rest/v1/game_saves?id=eq.{rowId}", "PATCH");
        byte[] raw = Encoding.UTF8.GetBytes(body);
        req.uploadHandler = new UploadHandlerRaw(raw);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Content-Type", "application/json");
        req.SetRequestHeader("apikey", _anonKey);
        req.SetRequestHeader("Authorization", $"Bearer {accessToken}");
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            done(false, ExtractError(req));
            yield break;
        }

        done(true, null);
    }

    public IEnumerator DeleteGameSave(string accessToken, string rowId, Action<bool, string> done)
    {
        using var req = UnityWebRequest.Delete($"{_url}/rest/v1/game_saves?id=eq.{rowId}");
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("apikey", _anonKey);
        req.SetRequestHeader("Authorization", $"Bearer {accessToken}");
        yield return req.SendWebRequest();

        if (req.result != UnityWebRequest.Result.Success)
        {
            done(false, ExtractError(req));
            yield break;
        }

        done(true, null);
    }

    static string ExtractError(UnityWebRequest req)
    {
        string body = req.downloadHandler?.text ?? "";
        if (string.IsNullOrEmpty(body)) return req.error ?? req.result.ToString();
        try
        {
            var jo = JObject.Parse(body);
            var msg = jo["msg"]?.ToString() ?? jo["message"]?.ToString() ?? jo["error_description"]?.ToString();
            if (!string.IsNullOrEmpty(msg)) return msg;
        }
        catch { /* ignore */ }
        return body.Length > 200 ? body.Substring(0, 200) : body;
    }
}

[Serializable]
public class SupabaseAuthResponse
{
    [JsonProperty("access_token")] public string AccessToken;
    [JsonProperty("refresh_token")] public string RefreshToken;
    [JsonProperty("expires_in")] public int ExpiresIn;
    [JsonProperty("user")] public SupabaseUserDto User;
}

[Serializable]
public class SupabaseUserDto
{
    [JsonProperty("id")] public string Id;
    [JsonProperty("email")] public string Email;
}

[Serializable]
public class GameSaveRowDto
{
    [JsonProperty("id")] public string Id;
    [JsonProperty("user_id")] public string UserId;
    [JsonProperty("slot_label")] public string SlotLabel;
    [JsonProperty("payload")] public JObject Payload;
    [JsonProperty("payload_version")] public int PayloadVersion;
    [JsonProperty("created_at")] public string CreatedAt;
    [JsonProperty("updated_at")] public string UpdatedAt;

    public bool TryGetDocument(out GameSaveDocument doc, out string err)
    {
        doc = null;
        err = null;
        if (Payload == null)
        {
            err = "Save payload missing.";
            return false;
        }
        return GameSaveSerializer.TryParse(Payload.ToString(Formatting.None), out doc, out err);
    }
}
