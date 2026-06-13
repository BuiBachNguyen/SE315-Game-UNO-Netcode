using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
#if UNITY_EDITOR
using Unity.Multiplayer.Playmode;
#endif

public class MultiplayerAutoJoinTest : MonoBehaviour
{
    const string TestRoomName = "MPPM Auto Test Room";

    [SerializeField] LobbyUI lobbyUI;
    [SerializeField] float clientRetryDelaySeconds = 1f;
    [SerializeField] int maxJoinAttempts = 30;

    async void Start()
    {
#if UNITY_EDITOR
        if (lobbyUI == null)
            lobbyUI = GetComponent<LobbyUI>();

        if (lobbyUI == null)
        {
            Debug.LogError("MultiplayerAutoJoinTest requires LobbyUI.");
            return;
        }

        try
        {
            await UnityServiceInit.EnsureInitializedAsync();

            if (CurrentPlayer.IsMainEditor)
                await lobbyUI.CreateRoomAsync(TestRoomName);
            else
                await FindAndJoinHostAsync();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
#endif
    }

#if UNITY_EDITOR
    async Task FindAndJoinHostAsync()
    {
        for (int attempt = 1; attempt <= maxJoinAttempts; attempt++)
        {
            QueryResponse response = await LobbyService.Instance.QueryLobbiesAsync();
            Lobby lobby = response.Results
                .Where(item =>
                    item.Name == TestRoomName &&
                    item.AvailableSlots > 0 &&
                    item.Data != null &&
                    item.Data.ContainsKey("joinCode"))
                .OrderByDescending(item => item.LastUpdated)
                .FirstOrDefault();

            if (lobby != null)
            {
                Debug.Log($"Auto joining test lobby {lobby.Id} on attempt {attempt}.");
                await lobbyUI.JoinLobbyByIdAsync(lobby.Id);
                return;
            }

            await Task.Delay(TimeSpan.FromSeconds(clientRetryDelaySeconds));
        }

        Debug.LogError($"Could not find {TestRoomName} after {maxJoinAttempts} attempts.");
    }
#endif
}
