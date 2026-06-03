using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class WaitingRoomChecker : NetworkBehaviour
{
    public int requiredPlayers = 3;

    bool isCountingDown = false;
    bool isGamePlaying = false;

    Coroutine countdownRoutine;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        NetworkManager.Singleton.OnClientConnectedCallback += OnPlayerJoined;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnPlayerLeft;
    }

    public override void OnNetworkDespawn()
    {
        if (!IsServer) return;

        NetworkManager.Singleton.OnClientConnectedCallback -= OnPlayerJoined;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnPlayerLeft;
    }

    void OnPlayerJoined(ulong clientId)
    {
        int count = NetworkManager.Singleton.ConnectedClientsList.Count;

        Debug.Log("Player joined: " + clientId);
        Debug.Log("Total player: " + count);

        // ?ang ch?i ? không cho vào tr?n hi?n t?i
        if (isGamePlaying)
        {
            Debug.Log("Game ?ang ch?i ? player ? waiting room");
            return;
        }

        if (!isCountingDown && count >= requiredPlayers)
        {
            countdownRoutine = StartCoroutine(StartCountdown());
        }
    }

    void OnPlayerLeft(ulong clientId)
    {
        int count = NetworkManager.Singleton.ConnectedClientsList.Count;

        Debug.Log("Player left: " + clientId);
        Debug.Log("Total player: " + count);

        // n?u ?ang countdown mà thi?u ng??i ? h?y
        if (isCountingDown && count < requiredPlayers)
        {
            CancelCountdown();
        }
    }

    IEnumerator StartCountdown()
    {
        isCountingDown = true;

        for (int i = 5; i > 0; i--)
        {
            UpdateCountdownClientRpc(i);
            yield return new WaitForSeconds(1f);
        }

        StartGame();
    }

    void CancelCountdown()
    {
        Debug.Log("Cancel Countdown");

        if (countdownRoutine != null)
            StopCoroutine(countdownRoutine);

        isCountingDown = false;

        UpdateCountdownClientRpc(-1); // hide UI
    }

    void StartGame()
    {
        isGamePlaying = true;
        isCountingDown = false;

        Debug.Log("START GAME");

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
