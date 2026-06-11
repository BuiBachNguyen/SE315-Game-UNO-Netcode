using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using System.Collections.Generic;
using Unity.Services.Core;
using System.Threading.Tasks;
using UnityEngine;

public class GameBootstrap : MonoBehaviour
{
    private async void Start()
    {
        await UnityServices.InitializeAsync();

        await AuthenticationService.Instance
            .SignInAnonymouslyAsync();

        await LeaveAllJoinedLobbies();

        Debug.Log(
            AuthenticationService.Instance.PlayerId);
    }
    public async Task LeaveAllJoinedLobbies()
    {
        try
        {
            List<string> joinedLobbies =
                await LobbyService.Instance.GetJoinedLobbiesAsync();

            foreach (string lobbyId in joinedLobbies)
            {
                await LobbyService.Instance.RemovePlayerAsync(
                    lobbyId,
                    AuthenticationService.Instance.PlayerId);

                Debug.Log($"Left Lobby: {lobbyId}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError(e);
        }
    }
}