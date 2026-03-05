using UnityEngine;

/// <summary>
/// Persists across scenes (DontDestroyOnLoad). Saves and loads the best run
/// stats using PlayerPrefs. Access via HighScoreManager.Instance anywhere.
/// </summary>
public class HighScoreManager : MonoBehaviour
{
    public static HighScoreManager Instance { get; private set; }

    private const string KEY_WAVES  = "BestWaves";
    private const string KEY_TIME   = "BestTime";
    private const string KEY_KILLS  = "BestKills";

    // ── Best Run Stats ─────────────────────────────────────────────────────
    public int   BestWaves  { get; private set; }
    public float BestTime   { get; private set; }
    public int   BestKills  { get; private set; }

    // Set to true this session if a record was broken — GameOverUI reads this
    public bool NewBestWaves  { get; private set; }
    public bool NewBestTime   { get; private set; }
    public bool NewBestKills  { get; private set; }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Load();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // ── Load from PlayerPrefs ─────────────────────────────────────────────

    private void Load()
    {
        BestWaves = PlayerPrefs.GetInt(KEY_WAVES, 0);
        BestTime  = PlayerPrefs.GetFloat(KEY_TIME, 0f);
        BestKills = PlayerPrefs.GetInt(KEY_KILLS, 0);
    }

    // ── Called at end of run ──────────────────────────────────────────────

    public void SubmitRun(int waves, float time, int kills)
    {
        NewBestWaves = NewBestTime = NewBestKills = false;

        if (waves > BestWaves)
        {
            BestWaves = waves;
            PlayerPrefs.SetInt(KEY_WAVES, BestWaves);
            NewBestWaves = true;
        }
        if (time > BestTime)
        {
            BestTime = time;
            PlayerPrefs.SetFloat(KEY_TIME, BestTime);
            NewBestTime = true;
        }
        if (kills > BestKills)
        {
            BestKills = kills;
            PlayerPrefs.SetInt(KEY_KILLS, BestKills);
            NewBestKills = true;
        }

        PlayerPrefs.Save();
    }

    public bool HasAnyRecord() => BestWaves > 0 || BestTime > 0f || BestKills > 0;

    public static string FormatTime(float seconds)
    {
        int m = Mathf.FloorToInt(seconds / 60f);
        int s = Mathf.FloorToInt(seconds % 60f);
        return $"{m:00}:{s:00}";
    }
}
