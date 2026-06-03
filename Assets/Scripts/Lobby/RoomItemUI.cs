using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RoomItemUI : MonoBehaviour
{
    public TMP_Text roomNameText;
    public TMP_Text playerCountText;
    public Button joinButton;

    string lobbyId;
    LobbyUI lobbyUI;

    public void Setup(string name,
        int playerCount,
        int maxPlayer,
        string id,
        LobbyUI ui)
    {
        roomNameText.text = name;
        playerCountText.text =
            playerCount + "/" + maxPlayer;

        lobbyId = id;
        lobbyUI = ui;

        joinButton.onClick
            .AddListener(OnClickJoin);
    }

    void OnClickJoin()
    {
        lobbyUI.JoinLobbyById(lobbyId);
    }
}
