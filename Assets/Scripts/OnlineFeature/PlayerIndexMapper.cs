using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class PlayerIndexMapper : NetworkBehaviour
{
    public static PlayerIndexMapper Instance { get; private set; }

    public event Action PlayerNamesChanged;

    private NetworkList<ulong> playerClientIds;
    private NetworkList<FixedString64Bytes> playerNames;

    private readonly Dictionary<ulong, int> clientToIndex = new Dictionary<ulong, int>();
    private readonly Dictionary<int, ulong> indexToClient = new Dictionary<int, ulong>();
    private readonly Dictionary<ulong, FixedString64Bytes> pendingPlayerNames =
        new Dictionary<ulong, FixedString64Bytes>();

    private void Awake()
    {
        Instance = this;
        playerClientIds = new NetworkList<ulong>();
        playerNames = new NetworkList<FixedString64Bytes>();
    }

    public override void OnNetworkSpawn()
    {
        playerClientIds.OnListChanged += OnPlayerListChanged;
        playerNames.OnListChanged += OnPlayerNameListChanged;

        RebuildLocalCache();

        if (IsClient)
        {
            string localName = PlayerData.Instance != null
                ? PlayerData.Instance.PlayerName
                : string.Empty;

            SubmitPlayerNameServerRpc(localName);
        }
    }

    public override void OnNetworkDespawn()
    {
        playerClientIds.OnListChanged -= OnPlayerListChanged;
        playerNames.OnListChanged -= OnPlayerNameListChanged;
    }

    public override void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        base.OnDestroy();
    }

    public void AssignAllPlayers()
    {
        if (!IsServer)
            return;

        playerClientIds.Clear();
        playerNames.Clear();

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            playerClientIds.Add(client.ClientId);

            if (pendingPlayerNames.TryGetValue(client.ClientId, out FixedString64Bytes playerName))
                playerNames.Add(playerName);
            else
                playerNames.Add(CreateFallbackName(playerNames.Count));

            Debug.Log(
                $"[PlayerIndexMapper] Assigned clientId {client.ClientId} " +
                $"to playerIndex {playerClientIds.Count - 1}");
        }

        PlayerNamesChanged?.Invoke();
    }

    [ServerRpc(RequireOwnership = false)]
    private void SubmitPlayerNameServerRpc(
        string submittedName,
        ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        FixedString64Bytes playerName = SanitizePlayerName(submittedName, clientId);

        pendingPlayerNames[clientId] = playerName;

        int playerIndex = GetPlayerIndex(clientId);
        if (playerIndex >= 0 && playerIndex < playerNames.Count)
            playerNames[playerIndex] = playerName;
    }

    public int GetLocalPlayerIndex()
    {
        if (NetworkManager.Singleton == null)
            return -1;

        ulong localId = NetworkManager.Singleton.LocalClientId;
        return clientToIndex.TryGetValue(localId, out int index) ? index : -1;
    }

    public int GetPlayerIndex(ulong clientId)
    {
        return clientToIndex.TryGetValue(clientId, out int index) ? index : -1;
    }

    public ulong GetClientId(int playerIndex)
    {
        return indexToClient.TryGetValue(playerIndex, out ulong clientId)
            ? clientId
            : ulong.MaxValue;
    }

    public string GetPlayerName(int playerIndex)
    {
        if (playerIndex >= 0 && playerIndex < playerNames.Count)
            return playerNames[playerIndex].ToString();

        return $"Player {playerIndex + 1}";
    }

    public int PlayerCount => playerClientIds.Count;

    private void OnPlayerListChanged(NetworkListEvent<ulong> changeEvent)
    {
        RebuildLocalCache();
        PlayerNamesChanged?.Invoke();
    }

    private void OnPlayerNameListChanged(NetworkListEvent<FixedString64Bytes> changeEvent)
    {
        PlayerNamesChanged?.Invoke();
    }

    private void RebuildLocalCache()
    {
        clientToIndex.Clear();
        indexToClient.Clear();

        for (int i = 0; i < playerClientIds.Count; i++)
        {
            ulong clientId = playerClientIds[i];
            clientToIndex[clientId] = i;
            indexToClient[i] = clientId;
        }
    }

    private static FixedString64Bytes SanitizePlayerName(string submittedName, ulong clientId)
    {
        string playerName = string.IsNullOrWhiteSpace(submittedName)
            ? $"Player {clientId + 1}"
            : submittedName.Trim();

        if (playerName.Length > 20)
            playerName = playerName.Substring(0, 20);

        return new FixedString64Bytes(playerName);
    }

    private static FixedString64Bytes CreateFallbackName(int playerIndex)
    {
        return new FixedString64Bytes($"Player {playerIndex + 1}");
    }
}
