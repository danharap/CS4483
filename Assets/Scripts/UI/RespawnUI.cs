using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Full-screen "You Died" overlay with a Respawn button.
/// Shown when the player dies; clicking Respawn sends them back to the Lobby.
/// </summary>
public class RespawnUI : MonoBehaviour
{
    [SerializeField] private GameObject respawnPanel;
    [SerializeField] private Button     respawnButton;

    void Awake()
    {
        if (respawnPanel) respawnPanel.SetActive(false);

        respawnButton?.onClick.AddListener(() =>
        {
            GameAudio.PlayButtonClick();
            Hide();
            GameManager.Instance?.RespawnToLobby();
        });
    }

    public void Show()
    {
        if (respawnPanel)
        {
            respawnPanel.SetActive(true);
            respawnPanel.transform.SetAsLastSibling();
        }
    }

    public void Hide()
    {
        if (respawnPanel) respawnPanel.SetActive(false);
    }
}
