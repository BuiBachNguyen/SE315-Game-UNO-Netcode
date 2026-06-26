using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.UI;

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

    [Header("Room Action UI")]
    [SerializeField] Button createRoomButton;
    [SerializeField] Button quickJoinButton;
    [SerializeField] Button refreshButton;
    [SerializeField] CanvasGroup roomListCanvasGroup;
    [SerializeField] GameObject loadingPanel;
    [SerializeField] TMP_Text loadingMessageText;
    [SerializeField] string creatingRoomMessage = "Dang tao phong...";
    [SerializeField] string joiningRoomMessage = "Dang vao phong...";
    [SerializeField] string findingRoomMessage = "Dang tim phong...";

    float searchDelay = 1f;
    float searchTimer = 0f;
    string currentSearchQuery = "";
    bool isSearching = false;
    bool isRoomOperationInProgress = false;

    async void Start()
    {
        SetLoading(false, "");

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

    void Update()
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
        try
        {
            await CreateRoomAsync();
        }
        catch (System.Exception exception)
        {
            Debug.LogException(exception);
        }
    }

    public async Task CreateRoomAsync()
    {
        if (!BeginRoomOperation(creatingRoomMessage))
            return;

        bool sceneHandoffStarted = false;

        try
        {
            await CreateRoomCoreAsync();
            sceneHandoffStarted = true;
        }
        finally
        {
            if (!sceneHandoffStarted)
                EndRoomOperation();
        }
    }

    async Task CreateRoomCoreAsync()
    {
        await UnityServiceInit.EnsureInitializedAsync();

        string randomId = UnityEngine.Random.Range(100000, 999999).ToString();
        string hostName = "Player";
        if (PlayerData.Instance != null && !string.IsNullOrEmpty(PlayerData.Instance.PlayerName))
            hostName = PlayerData.Instance.PlayerName;

        string roomName = $"RoomID: {randomId} - Host: {hostName}";

        // Create Relay first so clients never see a lobby without a join code.
        Allocation allocation = await RelayService.Instance.CreateAllocationAsync(MaxPlayers - 1);
        currentJoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

        UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.UseWebSockets = RelayConnectionType.StartsWith("ws");
        transport.SetRelayServerData(allocation.ToRelayServerData(RelayConnectionType));

        var options = new CreateLobbyOptions
        {
            IsPrivate = false,
            Data = new System.Collections.Generic.Dictionary<string, DataObject>
            {
                {
                    "joinCode",
                    new DataObject(DataObject.VisibilityOptions.Public, currentJoinCode)
                }
            }
        };

        Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(roomName, MaxPlayers, options);
        Debug.Log("Lobby code: " + lobby.LobbyCode);

        if (!NetworkManager.Singleton.StartHost())
        {
            await LobbyService.Instance.DeleteLobbyAsync(lobby.Id);
            throw new System.InvalidOperationException("Failed to start host.");
        }

        UnityServiceInit.StartLobbyHeartbeat(lobby.Id);
        NetworkManager.Singleton.SceneManager.LoadScene(
            "WaitingRoom",
            UnityEngine.SceneManagement.LoadSceneMode.Single);
    }

    public async void JoinRelay(string joinCode)
    {
        try
        {
            await JoinRelayAsync(joinCode);
        }
        catch (System.Exception exception)
        {
            Debug.LogException(exception);
        }
    }

    public async Task JoinRelayAsync(string joinCode)
    {
        if (!BeginRoomOperation(joiningRoomMessage))
            return;

        bool sceneHandoffStarted = false;

        try
        {
            await JoinRelayCoreAsync(joinCode);
            sceneHandoffStarted = true;
        }
        finally
        {
            if (!sceneHandoffStarted)
                EndRoomOperation();
        }
    }

    async Task JoinRelayCoreAsync(string joinCode)
    {
        await UnityServiceInit.EnsureInitializedAsync();

        JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

        UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.UseWebSockets = RelayConnectionType.StartsWith("ws");
        transport.SetRelayServerData(joinAllocation.ToRelayServerData(RelayConnectionType));

        if (!NetworkManager.Singleton.StartClient())
            throw new System.InvalidOperationException("Failed to start client.");

        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnectWhileJoining;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectWhileJoining;
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

    async Task RefreshRoomListAsync()
    {
        await UnityServiceInit.EnsureInitializedAsync();

        foreach (Transform child in roomContainer)
            Destroy(child.gameObject);

        QueryResponse response = await LobbyService.Instance.QueryLobbiesAsync();

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

    public async void JoinLobbyById(string lobbyId)
    {
        try
        {
            await JoinLobbyByIdAsync(lobbyId);
        }
        catch (System.Exception exception)
        {
            Debug.LogException(exception);
        }
    }

    public async Task JoinLobbyByIdAsync(string lobbyId)
    {
        if (!BeginRoomOperation(joiningRoomMessage))
            return;

        bool sceneHandoffStarted = false;

        try
        {
            await JoinLobbyByIdCoreAsync(lobbyId);
            sceneHandoffStarted = true;
        }
        finally
        {
            if (!sceneHandoffStarted)
                EndRoomOperation();
        }
    }

    async Task JoinLobbyByIdCoreAsync(string lobbyId)
    {
        await UnityServiceInit.EnsureInitializedAsync();

        Lobby lobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId);
        UnityServiceInit.TrackJoinedLobby(lobby.Id);

        string joinCode = lobby.Data["joinCode"].Value;
        await JoinRelayCoreAsync(joinCode);
    }

    public async void JoinLobbyRandomly()
    {
        if (!BeginRoomOperation(findingRoomMessage))
            return;

        bool sceneHandoffStarted = false;

        try
        {
            await UnityServiceInit.EnsureInitializedAsync();

            QueryResponse response = await LobbyService.Instance.QueryLobbiesAsync();
            Lobby crowdedestLobby = null;

            foreach (Lobby lobby in response.Results)
            {
                if (crowdedestLobby == null)
                    crowdedestLobby = lobby;
                else if (crowdedestLobby.Players.Count < lobby.Players.Count)
                    crowdedestLobby = lobby;
            }

            if (crowdedestLobby == null)
            {
                Debug.LogWarning("No public lobby is currently available.");
                return;
            }

            SetLoading(true, joiningRoomMessage);
            await JoinLobbyByIdCoreAsync(crowdedestLobby.Id);
            sceneHandoffStarted = true;
        }
        catch (System.Exception exception)
        {
            Debug.LogException(exception);
        }
        finally
        {
            if (!sceneHandoffStarted)
                EndRoomOperation();
        }
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
            QueryLobbiesOptions options = new QueryLobbiesOptions
            {
                Filters = new System.Collections.Generic.List<QueryFilter>
                {
                    new QueryFilter(
                        field: QueryFilter.FieldOptions.Name,
                        op: QueryFilter.OpOptions.CONTAINS,
                        value: searchQuery)
                }
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

    bool BeginRoomOperation(string loadingMessage)
    {
        if (isRoomOperationInProgress)
            return false;

        isRoomOperationInProgress = true;
        SetRoomActionInteractable(false);
        SetLoading(true, loadingMessage);
        return true;
    }

    void EndRoomOperation()
    {
        isRoomOperationInProgress = false;

        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnectWhileJoining;

        SetRoomActionInteractable(true);
        SetLoading(false, "");
    }

    void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnectWhileJoining;
    }

    void OnClientDisconnectWhileJoining(ulong clientId)
    {
        if (!isRoomOperationInProgress || NetworkManager.Singleton == null)
            return;

        if (clientId != NetworkManager.Singleton.LocalClientId)
            return;

        Debug.LogWarning("Disconnected while joining room.");
        EndRoomOperation();
    }

    void SetRoomActionInteractable(bool interactable)
    {
        if (createRoomButton != null)
            createRoomButton.interactable = interactable;

        if (quickJoinButton != null)
            quickJoinButton.interactable = interactable;

        if (refreshButton != null)
            refreshButton.interactable = interactable;

        if (roomListCanvasGroup != null)
        {
            roomListCanvasGroup.interactable = interactable;
            roomListCanvasGroup.blocksRaycasts = interactable;
        }
    }

    void SetLoading(bool visible, string message)
    {
        if (loadingPanel != null)
            loadingPanel.SetActive(visible);

        if (loadingMessageText != null)
            loadingMessageText.text = message;
    }
}
