using System.Collections;
using UnityEngine;

/// <summary>
/// Looped background music: lobby, Arena 1 / Arena 2 combat, mini-boss (Arena 1), final boss (Satan in Arena 2).
/// Boss spawn one-shots: Resources/SFX/FirstBossSpawn, FinalBossSpawn (also wired by SetupAll / AudioSetup for clips).
/// Clips: SetupAll wires Assets/Resources/Music/*.mp3; missing refs load via Resources.Load at runtime.
/// </summary>
public class GameplayMusicController : MonoBehaviour
{
    public static GameplayMusicController Instance { get; private set; }

    [Header("Tracks")]
    [SerializeField] private AudioClip lobbyMusic;
    [SerializeField] private AudioClip arena1Music;
    [SerializeField] private AudioClip arena2Music;
    [SerializeField] private AudioClip boss1Music;
    [SerializeField] private AudioClip finalBossMusic;

    [Tooltip("Multiplier before master × music settings.")]
    [SerializeField] [Range(0f, 1f)] private float baseVolume = 0.55f;

    private AudioSource musicSource;
    private bool eventsBound;
    private WaveManager boundWaveManager;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        musicSource = GetComponent<AudioSource>();
        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
            musicSource.spatialBlend = 0f;
            musicSource.priority = 16;
            musicSource.ignoreListenerVolume = false;
        }

        EnsureClipsFromResources();
    }

    /// <summary>
    /// Fills missing references from Resources/Music (works even if SETUP EVERYTHING never wired clips).
    /// </summary>
    void EnsureClipsFromResources()
    {
        if (lobbyMusic == null) lobbyMusic      = Resources.Load<AudioClip>("Music/LobbyMusic");
        if (arena1Music == null) arena1Music    = Resources.Load<AudioClip>("Music/Arena1");
        if (arena2Music == null) arena2Music    = Resources.Load<AudioClip>("Music/Arena2");
        if (boss1Music == null) boss1Music      = Resources.Load<AudioClip>("Music/Boss1");
        if (finalBossMusic == null) finalBossMusic = Resources.Load<AudioClip>("Music/FinalBoss");
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        UnbindWaveEvents();
    }

    void Start() => StartCoroutine(BindWaveEventsWhenReady());

    void LateUpdate()
    {
        if (musicSource == null || !musicSource.isPlaying) return;
        musicSource.volume = baseVolume * EffectiveMusicMultiplier();
    }

    private static float EffectiveMusicMultiplier()
    {
        // SettingsManager statics stay 0 until Awake; inactive settings GO → Awake never runs → use PlayerPrefs.
        float music = PlayerPrefs.GetFloat(SettingsManager.KeyMusicVol, 0.6f);
        float master = PlayerPrefs.GetFloat(SettingsManager.KeyMasterVol, 0.8f);
        if (SettingsManager.Instance != null)
        {
            music = SettingsManager.MusicVolume;
            master = SettingsManager.MasterVolume;
        }

        return Mathf.Clamp01(music) * Mathf.Clamp01(master);
    }

    private IEnumerator BindWaveEventsWhenReady()
    {
        for (int i = 0; i < 30; i++)
        {
            WaveManager wm = GameManager.Instance?.WaveManager;
            if (wm != null)
            {
                BindWaveEvents(wm);
                yield break;
            }

            yield return null;
        }
    }

    private void BindWaveEvents(WaveManager wm)
    {
        if (eventsBound) return;
        eventsBound = true;
        boundWaveManager = wm;
        wm.OnBossSpawned += HandleBossSpawned;
        wm.OnBossKilled += HandleBossKilled;
    }

    private void UnbindWaveEvents()
    {
        if (!eventsBound) return;
        eventsBound = false;
        if (boundWaveManager != null)
        {
            boundWaveManager.OnBossSpawned -= HandleBossSpawned;
            boundWaveManager.OnBossKilled -= HandleBossKilled;
            boundWaveManager = null;
        }
    }

    private void HandleBossSpawned()
    {
        if (IsArena2Active())
        {
            GameAudio.Play2D("SFX/FinalBossSpawn", 0.95f);
            PlayClip(finalBossMusic);
        }
        else
        {
            GameAudio.Play2D("SFX/FirstBossSpawn", 0.95f);
            PlayClip(boss1Music);
        }
    }

    private void HandleBossKilled()
    {
        if (IsArena2Active())
            PlayClip(arena2Music);
        else
            PlayClip(arena1Music);
    }

    /// <summary>Arena 2 root is active (Nether Colosseum).</summary>
    public static bool IsArena2Active()
    {
        GameObject a2 = GameObject.Find("=== LEVEL (ProBuilder) Arena2 ===");
        return a2 != null && a2.activeInHierarchy;
    }

    public void PlayArena1() => PlayClip(arena1Music);

    public void PlayArena2() => PlayClip(arena2Music);

    public void PlayLobby()
    {
        EnsureClipsFromResources();
        if (musicSource == null || lobbyMusic == null) return;
        if (musicSource.clip == lobbyMusic && musicSource.isPlaying) return;

        musicSource.clip = lobbyMusic;
        musicSource.loop = true;
        musicSource.volume = baseVolume * EffectiveMusicMultiplier();
        musicSource.Play();
    }

    public void StopMusic()
    {
        if (musicSource == null) return;
        musicSource.Stop();
        musicSource.clip = null;
    }

    private void PlayClip(AudioClip clip)
    {
        EnsureClipsFromResources();
        if (musicSource == null || clip == null) return;
        if (musicSource.clip == clip && musicSource.isPlaying) return;

        musicSource.loop = true;
        musicSource.clip = clip;
        musicSource.volume = baseVolume * EffectiveMusicMultiplier();
        musicSource.Play();
    }
}
