using UnityEngine;

/// <summary>
/// Shared 2D UI / stinger SFX via Resources paths (no extension). Respects master × SFX from SettingsManager / PlayerPrefs.
/// </summary>
public static class GameAudio
{
    static AudioSource sfx2D;

    static void Ensure2DSource()
    {
        if (sfx2D != null) return;
        var go = new GameObject("GameAudio_SFX2D");
        Object.DontDestroyOnLoad(go);
        sfx2D = go.AddComponent<AudioSource>();
        sfx2D.playOnAwake = false;
        sfx2D.loop = false;
        sfx2D.spatialBlend = 0f;
        sfx2D.priority = 32;
    }

    static float EffectiveSfxMultiplier()
    {
        float sfx = PlayerPrefs.GetFloat(SettingsManager.KeySFXVol, 1f);
        float master = PlayerPrefs.GetFloat(SettingsManager.KeyMasterVol, 0.8f);
        if (SettingsManager.Instance != null)
        {
            sfx = SettingsManager.SFXVolume;
            master = SettingsManager.MasterVolume;
        }

        return Mathf.Clamp01(sfx) * Mathf.Clamp01(master);
    }

    /// <param name="resourcesPath">e.g. "SFX/ButtonClick"</param>
    public static void Play2D(string resourcesPath, float volumeScale = 1f)
    {
        AudioClip clip = Resources.Load<AudioClip>(resourcesPath);
        if (clip == null) return;
        Ensure2DSource();
        float v = volumeScale * EffectiveSfxMultiplier();
        sfx2D.PlayOneShot(clip, Mathf.Clamp01(v));
    }

    public static void PlayButtonHover() => Play2D("SFX/ButtonHover", 0.4f);

    public static void PlayButtonClick() => Play2D("SFX/ButtonClick", 0.55f);

    public static void PlayGameStart() => Play2D("SFX/GameStart", 0.85f);
}
