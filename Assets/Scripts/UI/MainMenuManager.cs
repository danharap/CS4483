using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Drives the main menu scene. Shows the game title, best run stats,
/// lore flavor text, and a Play button. Attach to a persistent GameObject
/// in the MainMenu scene.
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    [Header("Scene Name")]
    [SerializeField] private string gameSceneName = "MainScene";

    [Header("UI References")]
    [SerializeField] private Button    playButton;
    [SerializeField] private TMP_Text  titleText;
    [SerializeField] private TMP_Text  subtitleText;
    [SerializeField] private TMP_Text  bestRunText;
    [SerializeField] private TMP_Text  flavorText;
    [SerializeField] private TMP_Text  versionText;

    [Header("Title Pulse")]
    [SerializeField] private float pulseSpeed  = 1.2f;
    [SerializeField] private float pulseMin    = 0.7f;
    [SerializeField] private float pulseMax    = 1.0f;

    // Rotating lore lines shown in the flavor text box
    private static readonly string[] LoreLines = {
        "\"Many warriors have entered the Fractured Grounds. None have broken the cycle.\"\n— Watcher Nara",
        "\"The Shard of Chaos shattered more than walls. It shattered time.\"\n— Final entry, Architect's Log",
        "\"Thorn was the greatest of us. Now it is the worst of us.\"\n— Watcher Drak",
        "\"Survive the waves. Grow stronger. Face the Corrupted Champion.\nRepeat. Unless you can end it.\"",
        "\"The Grounds remember every warrior. Their echoes become your enemies.\"\n— Watcher Vex",
    };

    private int loreIndex;

    void Start()
    {
        playButton?.onClick.AddListener(StartGame);

        if (titleText)    titleText.text    = "TEMP TITLE";
        if (subtitleText) subtitleText.text = "Top-Down Wave Survival";
        if (versionText)  versionText.text  = "CS4483 · Group 21 · Graybox Prototype";

        UpdateBestRun();
        RotateLore();
        StartCoroutine(PulseTitle());
        StartCoroutine(CycleLore());
    }

    void UpdateBestRun()
    {
        if (bestRunText == null) return;
        var hs = HighScoreManager.Instance;
        if (hs == null || !hs.HasAnyRecord())
        {
            bestRunText.text = "No runs recorded yet.\nBe the first to enter the Grounds.";
            return;
        }
        bestRunText.text =
            $"BEST RUN\n" +
            $"Waves: <color=#FFD700>{hs.BestWaves}</color>   " +
            $"Time: <color=#FFD700>{HighScoreManager.FormatTime(hs.BestTime)}</color>   " +
            $"Kills: <color=#FFD700>{hs.BestKills}</color>";
    }

    void StartGame()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    void RotateLore()
    {
        if (flavorText == null) return;
        flavorText.text = LoreLines[loreIndex % LoreLines.Length];
    }

    private IEnumerator CycleLore()
    {
        while (true)
        {
            yield return new WaitForSeconds(6f);
            loreIndex = (loreIndex + 1) % LoreLines.Length;
            // Fade out
            float t = 0f;
            while (t < 0.4f)
            {
                if (flavorText) flavorText.alpha = Mathf.Lerp(1f, 0f, t / 0.4f);
                t += Time.deltaTime;
                yield return null;
            }
            RotateLore();
            // Fade in
            t = 0f;
            while (t < 0.4f)
            {
                if (flavorText) flavorText.alpha = Mathf.Lerp(0f, 1f, t / 0.4f);
                t += Time.deltaTime;
                yield return null;
            }
            if (flavorText) flavorText.alpha = 1f;
        }
    }

    private IEnumerator PulseTitle()
    {
        while (true)
        {
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * pulseSpeed;
                float a = Mathf.Lerp(pulseMin, pulseMax, Mathf.PingPong(t, 1f));
                if (titleText) titleText.alpha = a;
                yield return null;
            }
        }
    }
}
