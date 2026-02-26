using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public class LobbyUI : MonoBehaviour
{
    string currentJoinCode;
    public Transform roomContainer;
    public GameObject roomItemPrefab;

    public async void CreateRoom()
    {
        // 1️⃣ tạo relay trước
        Allocation allocation =
            await RelayService.Instance
                .CreateAllocationAsync(6);

        currentJoinCode =
            await RelayService.Instance
                .GetJoinCodeAsync(
                    allocation.AllocationId);

        // 2️⃣ config transport
        var transport =
            NetworkManager.Singleton
            .GetComponent<UnityTransport>();

        transport.SetHostRelayData(
            allocation.RelayServer.IpV4,
            (ushort)allocation.RelayServer.Port,
            allocation.AllocationIdBytes,
            allocation.Key,
            allocation.ConnectionData);

        // 3️⃣ tạo lobby
        var options = new CreateLobbyOptions
        {
            IsPrivate = false,
            Data = new System.Collections.Generic
                .Dictionary<string, DataObject>
            {
                {
                    "joinCode",
                    new DataObject(
                        DataObject.VisibilityOptions.Public,
                        currentJoinCode)
                }
            }
        };

        Lobby lobby =
            await LobbyService.Instance
                .CreateLobbyAsync(
                    "Room ABC",
                    6,
                    options);

        Debug.Log("Lobby code: " +
            lobby.LobbyCode);

        // 4️⃣ start host
        NetworkManager.Singleton.StartHost();

        NetworkManager.Singleton.SceneManager
            .LoadScene(
            "WaitingRoom",
            UnityEngine.SceneManagement
            .LoadSceneMode.Single);
    }

    public async void JoinRelay(string joinCode)
    {
        var joinAllocation =
            await RelayService.Instance
                .JoinAllocationAsync(joinCode);

        var transport =
            NetworkManager.Singleton
            .GetComponent<UnityTransport>();

        transport.SetClientRelayData(
            joinAllocation.RelayServer.IpV4,
            (ushort)joinAllocation.RelayServer.Port,
            joinAllocation.AllocationIdBytes,
            joinAllocation.Key,
            joinAllocation.ConnectionData,
            joinAllocation.HostConnectionData);

        NetworkManager.Singleton.StartClient();

        NetworkManager.Singleton.SceneManager
            .LoadScene(
            "WaitingRoom",
            UnityEngine.SceneManagement
            .LoadSceneMode.Single);
    }



    public async void RefreshRoomList()
    {
        foreach (Transform child in roomContainer)
            Destroy(child.gameObject);

        QueryResponse response =
            await LobbyService.Instance
                .QueryLobbiesAsync();

        foreach (Lobby lobby in response.Results)
        {
            GameObject obj =
                Instantiate( roomItemPrefab, roomContainer);

            RoomItemUI item =
                obj.GetComponent<RoomItemUI>();

            item.Setup(
                lobby.Name,
                lobby.Players.Count,
                lobby.MaxPlayers,
                lobby.Id,
                this);
        }
    }


    public async void JoinLobbyById(string lobbyId)
    {
        Lobby lobby =
            await LobbyService.Instance
                .JoinLobbyByIdAsync(lobbyId);

        string joinCode =
            lobby.Data["joinCode"].Value;

        JoinRelay(joinCode);
    }

    public async void JoinLobbyRandomly()
    {
        QueryResponse response =
            await LobbyService.Instance
        .QueryLobbiesAsync();

        Lobby crowedestLobby = null;

        foreach (Lobby lobby in response.Results)
        {
            if(crowedestLobby  == null)
                crowedestLobby = lobby;
            else if(crowedestLobby.Players.Count < lobby.Players.Count)
                crowedestLobby = lobby;
        }

        JoinLobbyById(crowedestLobby.Id);

    }    
}
