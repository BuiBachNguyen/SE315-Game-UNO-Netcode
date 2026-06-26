using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using System.Threading.Tasks;
using UnityEngine;

public class LobbyUI : MonoBehaviour
{
    const int MaxPlayers = 4;
    const string RelayConnectionType =
#if UNITY_WEBGL && !UNITY_EDITOR
        "wss";
#else
        "dtls";
#endif

    string currentJoinCode;
    public Transform roomContainer;
    public GameObject roomItemPrefab;

    private float searchDelay = 1f;
    private float searchTimer = 0f;
    private string currentSearchQuery = "";
    private bool isSearching = false;

    private async void Start()
    {
        try
        {
            await UnityServiceInit.EnsureInitializedAsync();
            await RefreshRoomListAsync();
        }
        catch (System.Exception exception)
        {
            Debug.LogException(exception);
        }
    }

    private void Update()
    {
        if (isSearching)
        {
            searchTimer -= Time.deltaTime;
            if (searchTimer <= 0f)
            {
                isSearching = false;
                SearchRoom(currentSearchQuery);
            }
        }
    }

    public void OnSearchInputValueChanged(string newText)
    {
        currentSearchQuery = newText;
        searchTimer = searchDelay;
        isSearching = true;
    }

    public async void CreateRoom()
    {
        await CreateRoomAsync();
    }

    public async Task CreateRoomAsync()
    {
        await UnityServiceInit.EnsureInitializedAsync();

        string randomId = UnityEngine.Random.Range(100000, 999999).ToString();
        string hostName = "Player";
        if (PlayerData.Instance != null && !string.IsNullOrEmpty(PlayerData.Instance.PlayerName))
        {
            hostName = PlayerData.Instance.PlayerName;
        }
        string roomName = $"RoomID: {randomId} - Host: {hostName}";

        // 1️⃣ tạo relay trước
        Allocation allocation =
            await RelayService.Instance
                .CreateAllocationAsync(MaxPlayers - 1);

        currentJoinCode =
            await RelayService.Instance
                .GetJoinCodeAsync(
                    allocation.AllocationId);

        // 2️⃣ config transport
        var transport =
            NetworkManager.Singleton
            .GetComponent<UnityTransport>();

        transport.UseWebSockets =
            RelayConnectionType.StartsWith("ws");

        transport.SetRelayServerData(
            allocation.ToRelayServerData(
                RelayConnectionType));

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
                    roomName,
                    MaxPlayers,
                    options);

        UnityServiceInit.StartLobbyHeartbeat(lobby.Id);

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
        await JoinRelayAsync(joinCode);
    }

    public async Task JoinRelayAsync(string joinCode)
    {
        await UnityServiceInit.EnsureInitializedAsync();

        var joinAllocation =
            await RelayService.Instance
                .JoinAllocationAsync(joinCode);

        var transport =
            NetworkManager.Singleton
            .GetComponent<UnityTransport>();

        transport.UseWebSockets =
            RelayConnectionType.StartsWith("ws");

        transport.SetRelayServerData(
            joinAllocation.ToRelayServerData(
                RelayConnectionType));

        NetworkManager.Singleton.StartClient();
    }



    public async void RefreshRoomList()
    {
        try
        {
            await RefreshRoomListAsync();
        }
        catch (System.Exception exception)
        {
            Debug.LogException(exception);
        }
    }

    private async Task RefreshRoomListAsync()
    {
        await UnityServiceInit.EnsureInitializedAsync();

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
        await JoinLobbyByIdAsync(lobbyId);
    }

    public async Task JoinLobbyByIdAsync(string lobbyId)
    {
        await UnityServiceInit.EnsureInitializedAsync();

        Lobby lobby =
            await LobbyService.Instance
                .JoinLobbyByIdAsync(lobbyId);

        UnityServiceInit.TrackJoinedLobby(lobby.Id);

        string joinCode =
            lobby.Data["joinCode"].Value;

        await JoinRelayAsync(joinCode);
    }

    public async void JoinLobbyRandomly()
    {
        await UnityServiceInit.EnsureInitializedAsync();

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

        if (crowedestLobby == null)
        {
            Debug.LogWarning("No public lobby is currently available.");
            return;
        }

        JoinLobbyById(crowedestLobby.Id);

    }    

    public async void SearchRoom(string searchQuery)
    {
        await UnityServiceInit.EnsureInitializedAsync();

        if (string.IsNullOrEmpty(searchQuery))
        {
            RefreshRoomList();
            return;
        }

        foreach (Transform child in roomContainer)
            Destroy(child.gameObject);

        try
        {
            QueryLobbiesOptions options = new QueryLobbiesOptions();
            options.Filters = new System.Collections.Generic.List<QueryFilter>
            {
                new QueryFilter(
                    field: QueryFilter.FieldOptions.Name,
                    op: QueryFilter.OpOptions.CONTAINS,
                    value: searchQuery)
            };

            QueryResponse response = await LobbyService.Instance.QueryLobbiesAsync(options);

            foreach (Lobby lobby in response.Results)
            {
                GameObject obj = Instantiate(roomItemPrefab, roomContainer);
                RoomItemUI item = obj.GetComponent<RoomItemUI>();
                item.Setup(
                    lobby.Name,
                    lobby.Players.Count,
                    lobby.MaxPlayers,
                    lobby.Id,
                    this);
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError(e);
        }
    }
}
