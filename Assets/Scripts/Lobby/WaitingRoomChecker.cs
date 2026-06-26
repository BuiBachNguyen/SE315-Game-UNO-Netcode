using System.Collections;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class WaitingRoomChecker : NetworkBehaviour
{
    const int LeaveRoomNetworkFlushDelayMs = 150;

    [Min(1)]
    public int requiredPlayers = 4;

    [Header("UI References")]
    [SerializeField] Button startButton;
    [SerializeField] Button leaveButton;
    [SerializeField] CanvasGroup startButtonCanvasGroup;
    [SerializeField] float disabledStartAlpha = 0.4f;

    [Header("Scenes")]
    [SerializeField] string gameSceneName = "GameScene";
    [SerializeField] string lobbySceneName = "LobbyScene";

    Coroutine countdownRoutine;
    bool isGamePlaying;
    bool isCountingDown;
    bool canStartGame;
    bool isLeavingRoom;

    void Awake()
    {
        if (startButton != null)
        {
            startButton.onClick.AddListener(StartGameButton);

            if (startButtonCanvasGroup == null)
            {
                startButtonCanvasGroup = startButton.GetComponent<CanvasGroup>();
                if (startButtonCanvasGroup == null)
                    startButtonCanvasGroup = startButton.gameObject.AddComponent<CanvasGroup>();
            }
        }

        if (leaveButton != null)
            leaveButton.onClick.AddListener(LeaveRoom);

        SetStartButtonVisible(false);
        UpdateStartButtonState(false);
    }

    public override void OnNetworkSpawn()
    {
        SetStartButtonVisible(IsServer);
        UpdateStartButtonState(false);

        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientDisconnectCallback += OnAnyClientDisconnected;

        if (!IsServer)
            return;

        NetworkManager.Singleton.OnClientConnectedCallback += OnPlayerJoined;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnPlayerLeft;

        CheckRoomPlayerCount();
    }

    public override void OnNetworkDespawn()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnAnyClientDisconnected;

        if (!IsServer) return;

        NetworkManager.Singleton.OnClientConnectedCallback -= OnPlayerJoined;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnPlayerLeft;
    }

    public void StartGameButton()
    {
        if (!IsServer || !canStartGame || isGamePlaying)
            return;

        TryStartGame();
    }

    public async void LeaveRoom()
    {
        await LeaveRoomAsync();
    }

    void OnPlayerJoined(ulong clientId)
    {
        Debug.Log("Player joined: " + clientId);
        CheckRoomPlayerCount();
    }

    void OnPlayerLeft(ulong clientId)
    {
        Debug.Log("Player left: " + clientId);
        CheckRoomPlayerCount();
    }

    void CheckRoomPlayerCount()
    {
        if (!IsServer || NetworkManager.Singleton == null)
            return;

        int currentPlayers = NetworkManager.Singleton.ConnectedClientsList.Count;

        Debug.Log($"Room players: {currentPlayers}/{requiredPlayers}");

        if (isGamePlaying)
            return;

        bool hasEnoughPlayers = currentPlayers >= requiredPlayers;
        if (!hasEnoughPlayers && isCountingDown)
        {
            CancelCountdown();
            return;
        }

        if (!isCountingDown)
            UpdateStartButtonStateClientRpc(hasEnoughPlayers);
    }

    async Task LeaveRoomAsync()
    {
        isLeavingRoom = true;

        ChatManager.Instance?.SendLocalLeaveRoomMessage();
        await Task.Delay(LeaveRoomNetworkFlushDelayMs);

        if (IsServer)
        {
            ForceClientsLeaveRoomClientRpc();
            await Task.Delay(LeaveRoomNetworkFlushDelayMs);
        }

        try
        {
            await UnityServiceInit.LeaveCurrentLobbyAsync();
        }
        catch (System.Exception exception)
        {
            Debug.LogException(exception);
        }

        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.Shutdown();

        SceneManager.LoadScene(lobbySceneName);
    }

    [ClientRpc]
    void ForceClientsLeaveRoomClientRpc()
    {
        if (IsServer)
            return;

        isLeavingRoom = true;

        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.Shutdown();

        SceneManager.LoadScene(lobbySceneName);
    }

    void OnAnyClientDisconnected(ulong clientId)
    {
        if (isLeavingRoom || IsServer || NetworkManager.Singleton == null)
            return;

        if (clientId != NetworkManager.ServerClientId)
            return;

        NetworkManager.Singleton.Shutdown();
        SceneManager.LoadScene(lobbySceneName);
    }

    [ServerRpc(RequireOwnership = false)]
    void RequestStartGameServerRpc(ServerRpcParams rpcParams = default)
    {
        if (rpcParams.Receive.SenderClientId != NetworkManager.ServerClientId)
            return;

        TryStartGame();
    }

    void TryStartGame()
    {
        if (!IsServer || isGamePlaying || isCountingDown || NetworkManager.Singleton == null)
            return;

        int currentPlayers = NetworkManager.Singleton.ConnectedClientsList.Count;
        if (currentPlayers < requiredPlayers)
        {
            UpdateStartButtonStateClientRpc(false);
            return;
        }

        countdownRoutine = StartCoroutine(StartCountdown());
    }

    IEnumerator StartCountdown()
    {
        isCountingDown = true;
        UpdateStartButtonStateClientRpc(false);

        for (int i = 5; i > 0; i--)
        {
            if (NetworkManager.Singleton == null ||
                NetworkManager.Singleton.ConnectedClientsList.Count < requiredPlayers)
            {
                FinishCancelledCountdown();
                yield break;
            }

            UpdateCountdownClientRpc(i);
            yield return new WaitForSeconds(1f);
        }

        if (NetworkManager.Singleton != null &&
            NetworkManager.Singleton.ConnectedClientsList.Count >= requiredPlayers)
        {
            StartGame();
        }
        else
        {
            FinishCancelledCountdown();
        }
    }

    void CancelCountdown()
    {
        if (!IsServer)
            return;

        if (countdownRoutine != null)
        {
            StopCoroutine(countdownRoutine);
            countdownRoutine = null;
        }

        FinishCancelledCountdown();
    }

    void FinishCancelledCountdown()
    {
        isCountingDown = false;
        countdownRoutine = null;

        UpdateCountdownClientRpc(-1);

        bool hasEnoughPlayers = NetworkManager.Singleton != null &&
            NetworkManager.Singleton.ConnectedClientsList.Count >= requiredPlayers;

        UpdateStartButtonStateClientRpc(hasEnoughPlayers);
    }

    void StartGame()
    {
        isGamePlaying = true;
        isCountingDown = false;
        countdownRoutine = null;

        int currentPlayers = NetworkManager.Singleton.ConnectedClientsList.Count;
        Debug.Log($"START GAME with {currentPlayers} players");

        NetworkManager.Singleton.SceneManager.LoadScene(
            gameSceneName,
            LoadSceneMode.Single);
    }

    [ClientRpc]
    void UpdateStartButtonStateClientRpc(bool enabled)
    {
        UpdateStartButtonState(enabled);
    }

    [ClientRpc]
    void UpdateCountdownClientRpc(int time)
    {
        if (WaitingoomUI.Instance == null)
            return;

        if (time <= 0)
            WaitingoomUI.Instance.HideCountdown();
        else
            WaitingoomUI.Instance.UpdateCountdownText(time);
    }

    void UpdateStartButtonState(bool enabled)
    {
        bool isHost = IsServer;
        canStartGame = enabled && isHost;

        if (startButton != null)
            startButton.interactable = canStartGame;

        if (startButtonCanvasGroup != null)
        {
            startButtonCanvasGroup.alpha = canStartGame ? 1f : disabledStartAlpha;
            startButtonCanvasGroup.interactable = canStartGame;
            startButtonCanvasGroup.blocksRaycasts = canStartGame;
        }
    }

    void SetStartButtonVisible(bool visible)
    {
        if (startButton != null)
            startButton.gameObject.SetActive(visible);
    }
}
