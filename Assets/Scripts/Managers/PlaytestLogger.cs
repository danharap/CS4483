using System.IO;
using UnityEngine;

/// <summary>
/// Tracks and logs key playtesting metrics to Console and a persistent log file.
/// </summary>
public class PlaytestLogger : MonoBehaviour
{
    // ── Timing markers ────────────────────────────────────────────────────
    private float sessionStart;
    private float timeToFirstKill = -1f;
    private float timeToFirstLevelUp = -1f;
    private bool killedFirstEnemy;
    private bool reachedFirstLevelUp;

    void Awake()
    {
        sessionStart = Time.realtimeSinceStartup;
    }

    public void RecordFirstKill()
    {
        if (killedFirstEnemy) return;
        killedFirstEnemy = true;
        timeToFirstKill = Time.realtimeSinceStartup - sessionStart;
        Debug.Log($"[Playtest] First Kill at {timeToFirstKill:F1}s");
    }

    public void RecordFirstLevelUp()
    {
        if (reachedFirstLevelUp) return;
        reachedFirstLevelUp = true;
        timeToFirstLevelUp = Time.realtimeSinceStartup - sessionStart;
        Debug.Log($"[Playtest] First Level Up at {timeToFirstLevelUp:F1}s");
    }

    public void LogSummary(float timeSurvived, int wavesCleared, int totalKills)
    {
        string summary =
            $"=== PLAYTEST SUMMARY ===\n" +
            $"Time Survived:       {timeSurvived:F1}s\n" +
            $"Waves Cleared:       {wavesCleared}\n" +
            $"Total Kills:         {totalKills}\n" +
            $"Time to First Kill:  {(timeToFirstKill < 0 ? "N/A" : timeToFirstKill.ToString("F1") + "s")}\n" +
            $"Time to First LvlUp: {(timeToFirstLevelUp < 0 ? "N/A" : timeToFirstLevelUp.ToString("F1") + "s")}\n" +
            $"========================";

        Debug.Log(summary);
        WriteToFile(summary);
    }

    private void WriteToFile(string content)
    {
        try
        {
            string path = Path.Combine(Application.persistentDataPath, "playtest_log.txt");
            File.AppendAllText(path, $"\n[{System.DateTime.Now}]\n{content}\n");
            Debug.Log($"[Playtest] Log written to: {path}");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[Playtest] Could not write log: {e.Message}");
        }
    }
}
