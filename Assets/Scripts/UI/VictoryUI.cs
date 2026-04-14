using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Full-screen victory overlay shown when the player defeats Satan (final boss).
/// Displays a dramatic message, stats, then returns the player to the Lobby
/// and resets their run to level 1.
/// </summary>
public class VictoryUI : MonoBehaviour
{
    [SerializeField] private GameObject victoryPanel;
    [SerializeField] private TMP_Text   headerText;
    [SerializeField] private TMP_Text   subText;
    [SerializeField] private TMP_Text   statsText;
    [SerializeField] private Button     returnButton;

    void Awake()
    {
        if (victoryPanel) victoryPanel.SetActive(false);

        returnButton?.onClick.AddListener(() =>
        {
            GameAudio.PlayButtonClick();
            Hide();
            GameManager.Instance?.RespawnToLobby();
        });
    }

    public void Show(int wavesCleared, int totalKills, float timeSurvived)
    {
        if (headerText) headerText.text = "VICTORY!";
        if (subText)    subText.text    = "Satan has been vanquished.\nYou have survived the arena.";

        if (statsText)
        {
            int m = Mathf.FloorToInt(timeSurvived / 60f);
            int s = Mathf.FloorToInt(timeSurvived % 60f);
            statsText.text = $"Waves Cleared: {wavesCleared}\nTotal Kills: {totalKills}\nTime: {m:00}:{s:00}";
        }

        if (victoryPanel)
        {
            victoryPanel.SetActive(true);
            victoryPanel.transform.SetAsLastSibling();
        }
    }

    public void Hide()
    {
        if (victoryPanel) victoryPanel.SetActive(false);
    }
}
