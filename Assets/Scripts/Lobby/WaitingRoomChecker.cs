using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class WaitingRoomChecker : NetworkBehaviour
{
    [Min(1)]
    public int requiredPlayers = 4;

    bool isCountingDown;
    bool isGamePlaying;

    Coroutine countdownRoutine;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        NetworkManager.Singleton.OnClientConnectedCallback += OnPlayerJoined;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnPlayerLeft;

        CheckRoomPlayerCount();
    }

    public override void OnNetworkDespawn()
    {
        if (!IsServer) return;

        NetworkManager.Singleton.OnClientConnectedCallback -= OnPlayerJoined;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnPlayerLeft;
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

        if (currentPlayers >= requiredPlayers)
        {
            if (!isCountingDown)
                countdownRoutine = StartCoroutine(StartCountdown());
        }
        else if (isCountingDown)
        {
            CancelCountdown();
        }
    }

    IEnumerator StartCountdown()
    {
        isCountingDown = true;

        for (int i = 5; i > 0; i--)
        {
            if (NetworkManager.Singleton.ConnectedClientsList.Count < requiredPlayers)
            {
                FinishCancelledCountdown();
                yield break;
            }

            UpdateCountdownClientRpc(i);
            yield return new WaitForSeconds(1f);
        }

        if (NetworkManager.Singleton.ConnectedClientsList.Count >= requiredPlayers)
            StartGame();
        else
            FinishCancelledCountdown();
    }

    void CancelCountdown()
    {
        Debug.Log("Cancel Countdown");

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

        UpdateCountdownClientRpc(-1); // hide UI
    }

    void StartGame()
    {
        isGamePlaying = true;
        isCountingDown = false;
        countdownRoutine = null;

        int currentPlayers = NetworkManager.Singleton.ConnectedClientsList.Count;
        Debug.Log($"START GAME with {currentPlayers} players");

        NetworkManager.Singleton.SceneManager.LoadScene(
            "GameScene",
            LoadSceneMode.Single);
    }

    [ClientRpc]
    void UpdateCountdownClientRpc(int time)
    {
        if (WaitingoomUI.Instance == null) return;

        if (time <= 0)
            WaitingoomUI.Instance.HideCountdown();
        else
            WaitingoomUI.Instance.UpdateCountdownText(time);
    }
}
