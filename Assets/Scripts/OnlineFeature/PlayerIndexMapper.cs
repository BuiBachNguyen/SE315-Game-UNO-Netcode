using Unity.Netcode;
using Unity.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Map clientId (ulong) ↔ playerIndex (0-3).
/// Server gán index theo thứ tự join, sync tới tất cả clients.
/// </summary>
public class PlayerIndexMapper : NetworkBehaviour
{
    public static PlayerIndexMapper Instance { get; private set; }

    // ======== SYNC DATA ========
    // NetworkList tự đồng bộ tới tất cả clients
    // Mỗi phần tử chứa 1 clientId, index trong list = playerIndex
    private NetworkList<ulong> playerClientIds;

    // ======== LOCAL CACHE ========
    // Để tra cứu nhanh trên mỗi client
    private readonly Dictionary<ulong, int> clientToIndex = new Dictionary<ulong, int>();
    private readonly Dictionary<int, ulong> indexToClient = new Dictionary<int, ulong>();

    private void Awake()
    {
        Instance = this;
        playerClientIds = new NetworkList<ulong>();
    }

    public override void OnNetworkSpawn()
    {
        // Lắng nghe khi list thay đổi (server thêm player mới)
        playerClientIds.OnListChanged += OnPlayerListChanged;

        // Nếu list đã có data (client join muộn), rebuild cache
        RebuildLocalCache();
    }

    public override void OnNetworkDespawn()
    {
        playerClientIds.OnListChanged -= OnPlayerListChanged;
    }

    // ======== SERVER: GÁN PLAYER INDEX ========

    /// <summary>
    /// Server gọi khi tất cả player đã join lobby.
    /// Gán playerIndex 0, 1, 2, 3 theo thứ tự trong danh sách connected clients.
    /// </summary>
    public void AssignAllPlayers()
    {
        if (!IsServer) return;

        playerClientIds.Clear();

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            playerClientIds.Add(client.ClientId);
            Debug.Log($"[PlayerIndexMapper] Assigned clientId {client.ClientId} → playerIndex {playerClientIds.Count - 1}");
        }
    }

    // ======== QUERY METHODS (dùng trên mọi client) ========

    /// <summary>
    /// Trả về playerIndex (0-3) của client hiện tại.
    /// VD: Client này là player 2.
    /// </summary>
    public int GetLocalPlayerIndex()
    {
        ulong localId = NetworkManager.Singleton.LocalClientId;
        if (clientToIndex.TryGetValue(localId, out int index))
            return index;

        Debug.LogWarning($"[PlayerIndexMapper] LocalClientId {localId} chưa được map!");
        return -1;
    }

    /// <summary>
    /// Từ clientId → playerIndex.
    /// </summary>
    public int GetPlayerIndex(ulong clientId)
    {
        if (clientToIndex.TryGetValue(clientId, out int index))
            return index;
        return -1;
    }

    /// <summary>
    /// Từ playerIndex → clientId.
    /// </summary>
    public ulong GetClientId(int playerIndex)
    {
        if (indexToClient.TryGetValue(playerIndex, out ulong clientId))
            return clientId;
        return ulong.MaxValue;
    }

    /// <summary>
    /// Tổng số player đã được map.
    /// </summary>
    public int PlayerCount => playerClientIds.Count;

    // ======== INTERNAL ========

    private void OnPlayerListChanged(NetworkListEvent<ulong> changeEvent)
    {
        RebuildLocalCache();
    }

    private void RebuildLocalCache()
    {
        clientToIndex.Clear();
        indexToClient.Clear();

        for (int i = 0; i < playerClientIds.Count; i++)
        {
            ulong cid = playerClientIds[i];
            clientToIndex[cid] = i;
            indexToClient[i] = cid;
        }
    }
}
