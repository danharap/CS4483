using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Drives the main menu scene.
/// NEW GAME  → loads MainScene and auto-starts the tutorial.
/// LOAD GAME → loads MainScene straight into the lobby (skips tutorial).
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    [Header("Scene Name")]
    [SerializeField] private string gameSceneName = "MainScene";

    [Header("UI References")]
    [SerializeField] private Button    newGameButton;
    [SerializeField] private Button    loadGameButton;
    [SerializeField] private Button    settingsButton;
    [SerializeField] private TMP_Text  titleText;
    [SerializeField] private TMP_Text  subtitleText;
    [SerializeField] private TMP_Text  bestRunText;
    [SerializeField] private TMP_Text  flavorText;
    [SerializeField] private TMP_Text  versionText;

    // Keep the old field name so SetupAll doesn't break if it still wires "playButton"
    [SerializeField] private Button    playButton;

    [Header("Title Pulse")]
    [SerializeField] private float pulseSpeed  = 1.2f;
    [SerializeField] private float pulseMin    = 0.7f;
    [SerializeField] private float pulseMax    = 1.0f;

    /// <summary>
    /// Static flag read by GameManager/TutorialRoomManager after MainScene loads.
    /// True  = new game → run tutorial immediately.
    /// False = load game → land in lobby.
    /// </summary>
    public static bool ShouldRunTutorial { get; private set; }

    /// <summary>Used when hydrating an account save so tutorial auto-start matches save data.</summary>
    public static void SetShouldRunTutorialForLoadedSave(bool runTutorial) => ShouldRunTutorial = runTutorial;

    private static readonly string[] LoreLines = {
        "\"Many warriors have entered the Fractured Grounds. None have broken the cycle.\"\n-- Watcher Nara",
        "\"The Shard of Chaos shattered more than walls. It shattered time.\"\n-- Final entry, Architect's Log",
        "\"Thorn was the greatest of us. Now it is the worst of us.\"\n-- Watcher Drak",
        "\"Survive the waves. Grow stronger. Face the Corrupted Champion.\nRepeat. Unless you can end it.\"",
        "\"The Grounds remember every warrior. Their echoes become your enemies.\"\n-- Watcher Vex",
    };

    private int loreIndex;

    void Awake()
    {
        if (GetComponent<MainMenuLocalAccountPanel>() == null)
            gameObject.AddComponent<MainMenuLocalAccountPanel>();

        // Self-heal: find buttons by their GameObject name if serialized refs were lost
        if (newGameButton  == null) newGameButton  = FindButtonByName("NewGameButton");
        if (loadGameButton == null) loadGameButton = FindButtonByName("LoadGameButton");
        if (settingsButton == null) settingsButton = FindButtonByName("SettingsButton");
        if (playButton     == null) playButton     = FindButtonByName("PlayButton");

        // Self-heal: find text elements by name
        if (titleText    == null) titleText    = FindTMPByName("Title");
        if (subtitleText == null) subtitleText = FindTMPByName("Subtitle");
        if (bestRunText  == null) bestRunText  = FindTMPByName("BestRunText");
        if (flavorText   == null) flavorText   = FindTMPByName("FlavorText");
        if (versionText  == null) versionText  = FindTMPByName("VersionText");
    }

    void Start()
    {
        WireMenuButton(newGameButton, OnNewGame);
        WireMenuButton(loadGameButton, OnLoadGame);
        WireMenuButton(settingsButton, OnSettings);
        WireMenuButton(playButton, OnNewGame);

        // Account gate panel unlocks these after a successful sign-in.
        SetMainMenuLocked(true);

        if (titleText)    titleText.text    = "WAVE GAME";
        if (subtitleText) subtitleText.text = "Top-Down Wave Survival";
        if (versionText)  versionText.text  = "CS4483 · Group 21";

        UpdateBestRun();
        RotateLore();
        StartCoroutine(PulseTitle());
        StartCoroutine(CycleLore());
    }

    static void WireMenuButton(Button b, UnityEngine.Events.UnityAction action)
    {
        if (b == null || action == null) return;
        b.onClick.AddListener(GameAudio.PlayButtonClick);
        b.onClick.AddListener(action);
        EventTrigger et = b.gameObject.GetComponent<EventTrigger>();
        if (et == null) et = b.gameObject.AddComponent<EventTrigger>();
        var hover = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        hover.callback.AddListener(_ => GameAudio.PlayButtonHover());
        et.triggers.Add(hover);
    }

    private Button FindButtonByName(string goName)
    {
        Transform t = transform.Find(goName);
        if (t != null) return t.GetComponent<Button>();
        // Wider search in case hierarchy differs
        foreach (Button b in GetComponentsInChildren<Button>(true))
            if (b.gameObject.name == goName) return b;
        return null;
    }

    private TMP_Text FindTMPByName(string goName)
    {
        foreach (TMP_Text t in GetComponentsInChildren<TMP_Text>(true))
            if (t.gameObject.name == goName) return t;
        return null;
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

    void OnNewGame()
    {
        LocalSaveRuntime.ActiveSaveId = null;
        LocalSaveRuntime.PendingHydrate = null;
        PlayerPrefs.SetInt("Meta_TutorialCompleted", 0);
        PlayerPrefs.Save();
        ShouldRunTutorial = true;
        SceneManager.LoadScene(gameSceneName);
    }

    void OnLoadGame()
    {
        LocalSaveRuntime.ActiveSaveId = null;
        LocalSaveRuntime.PendingHydrate = null;
        ShouldRunTutorial = false;
        SceneManager.LoadScene(gameSceneName);
    }

    /// <summary>Used after setting <see cref="LocalSaveRuntime.PendingHydrate"/>.</summary>
    public void LoadMainSceneFromAccountSave()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    public void SetMainMenuLocked(bool locked)
    {
        bool enabled = !locked;
        if (newGameButton != null) newGameButton.interactable = enabled;
        if (loadGameButton != null) loadGameButton.interactable = enabled;
        if (playButton != null) playButton.interactable = enabled;
    }

    void OnSettings()
    {
        SettingsManager sm = Object.FindFirstObjectByType<SettingsManager>(FindObjectsInactive.Include);
        if (sm != null) sm.Show();
        else
        {
            // Create SettingsManager on the fly if it wasn't placed in the scene.
            GameObject go = new GameObject("SettingsManager");
            sm = go.AddComponent<SettingsManager>();
            sm.Show();
        }
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
            float t = 0f;
            while (t < 0.4f)
            {
                if (flavorText) flavorText.alpha = Mathf.Lerp(1f, 0f, t / 0.4f);
                t += Time.deltaTime;
                yield return null;
            }
            RotateLore();
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
