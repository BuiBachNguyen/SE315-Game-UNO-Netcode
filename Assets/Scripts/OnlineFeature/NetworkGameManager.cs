using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Server-authoritative game manager cho mode online.
/// Host giữ toàn bộ state, client chỉ nhận kết quả qua ClientRpc.
/// Implement IGameLogic để DrawPileView + HandView hoạt động.
/// </summary>
public class NetworkGameManager : NetworkBehaviour, IGameLogic
{
    const string LoadingGameMessage = "Loading game...";
    const string DealingCardsMessage = "Dealing cards...";

    public static NetworkGameManager Instance { get; private set; }

    [Header("Rules")]
    [SerializeField] private int startingHandSize = 7;
    [SerializeField] private int matchPointTarget = 500;

    // ================================================================
    // SERVER-ONLY STATE (chỉ server đọc/ghi, client không thấy)
    // ================================================================
    private readonly List<List<Card>> hands = new List<List<Card>>();
    private readonly List<Card> drawPile = new List<Card>();
    private readonly List<Card> discardPile = new List<Card>();
    private readonly Dictionary<int, int> matchScores = new Dictionary<int, int>();
    private readonly HashSet<int> unoCalledThisTurn = new HashSet<int>();

    private int currentPlayerIndex;
    private int direction = 1;
    private CardColor currentColor = CardColor.Red;
    private bool waitingForWildColor;
    private bool roundEnded;
    private bool matchEnded;
    private bool waitingForInitialDeal;
    private bool waitingForDrawnCardDecision;
    private Card pendingWildCard;
    private Card pendingDrawnCard;
    private int pendingWildPlayer = -1;
    private int pendingDrawnPlayer = -1;
    private int playerCount;
    private bool gameStarted;

    // ================================================================
    // NETWORK VARIABLES (tự sync tới tất cả clients)
    // ================================================================
    // Client dùng những biến này để biết trạng thái game mà không cần
    // server gửi ClientRpc mỗi lần thay đổi
    private NetworkVariable<int> netCurrentPlayer = new NetworkVariable<int>();
    private NetworkVariable<int> netDirection = new NetworkVariable<int>(1);
    private NetworkVariable<byte> netCurrentColor = new NetworkVariable<byte>();
    private NetworkVariable<int> netDrawPileCount = new NetworkVariable<int>();
    private NetworkVariable<bool> netWaitingForWildColor = new NetworkVariable<bool>();

    // ================================================================
    // LOCAL CLIENT STATE
    // ================================================================
    // Mỗi client tự giữ bài của mình (nhận từ server qua ClientRpc)
    private readonly List<Card> localHand = new List<Card>();
    private int localPlayerIndex = -1;
    private Card localTopDiscard;
    private bool localInitialDealInProgress;
    private bool localWaitingForDrawnCardDecision;
    private Card localPendingDrawnCard;
    private readonly HashSet<ulong> initialDealReadyClients = new HashSet<ulong>();

    private void Awake()
    {
        Instance = this;
    }

    public override void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        base.OnDestroy();
    }

    // ================================================================
    // LIFECYCLE — Khi NetworkObject được spawn
    // ================================================================

    public override void OnNetworkSpawn()
    {
        if (IsClient)
            SceneLoadingOverlay.Show(LoadingGameMessage);

        if (IsServer)
        {
            // The host spawns this object before every client has finished loading
            // GameScene. Wait so the initial hand ClientRpc is not sent too early.
            NetworkManager.SceneManager.OnLoadEventCompleted += HandleLoadEventCompleted;
        }

        // Cả server và client đều lắng nghe NetworkVariable thay đổi
        netCurrentPlayer.OnValueChanged += OnCurrentPlayerChanged;
        netDirection.OnValueChanged += OnDirectionChanged;
        netCurrentColor.OnValueChanged += OnColorChanged;
        netDrawPileCount.OnValueChanged += OnDrawPileCountChanged;
        netWaitingForWildColor.OnValueChanged += OnWaitingForWildColorChanged;
        GameEvents.OnInitialDealCompleted += HandleInitialDealCompleted;
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer && NetworkManager != null && NetworkManager.SceneManager != null)
        {
            NetworkManager.SceneManager.OnLoadEventCompleted -= HandleLoadEventCompleted;
        }

        netCurrentPlayer.OnValueChanged -= OnCurrentPlayerChanged;
        netDirection.OnValueChanged -= OnDirectionChanged;
        netCurrentColor.OnValueChanged -= OnColorChanged;
        netDrawPileCount.OnValueChanged -= OnDrawPileCountChanged;
        netWaitingForWildColor.OnValueChanged -= OnWaitingForWildColorChanged;
        GameEvents.OnInitialDealCompleted -= HandleInitialDealCompleted;

        SceneLoadingOverlay.Hide();
    }

    private void HandleLoadEventCompleted(
        string sceneName,
        LoadSceneMode loadSceneMode,
        List<ulong> clientsCompleted,
        List<ulong> clientsTimedOut)
    {
        if (sceneName != gameObject.scene.name)
            return;

        if (clientsTimedOut.Count > 0)
        {
            Debug.LogWarning(
                $"[NetworkGameManager] {clientsTimedOut.Count} client(s) timed out loading {sceneName}.");
        }

        StartGameOnce();
    }

    private void StartGameOnce()
    {
        if (gameStarted)
            return;

        gameStarted = true;
        StartCoroutine(WaitAndStartGame());
    }

    /// <summary>
    /// Đợi PlayerIndexMapper có đủ player rồi bắt đầu.
    /// </summary>
    private IEnumerator WaitAndStartGame()
    {
        while (PlayerIndexMapper.Instance == null ||
               !PlayerIndexMapper.Instance.IsSpawned)
        {
            yield return null;
        }

        // Gán playerIndex cho tất cả clients
        PlayerIndexMapper.Instance.AssignAllPlayers();
        playerCount = PlayerIndexMapper.Instance.PlayerCount;

        // Đợi thêm 1 frame để NetworkList sync
        yield return null;

        if (playerCount <= 0)
        {
            Debug.LogError("[NetworkGameManager] Cannot start round because no players were mapped.");
            gameStarted = false;
            yield break;
        }

        Debug.Log($"[NetworkGameManager] Starting round for {playerCount} players after all clients loaded GameScene.");
        InitializeMatchScores();
        StartRound();
    }

    // ================================================================
    // IGameLogic IMPLEMENTATION — Cho HandView + DrawPileView dùng
    // ================================================================

    /// <summary>
    /// HandView gọi khi cần kiểm tra bài nào đánh được (highlight).
    /// Chạy trên CLIENT — dùng local state.
    /// </summary>
    public bool IsValidPlay(Card card)
    {
        if (!IsLocalPlayersTurn()) return false;
        return IsValidPlayCheck(card);
    }

    public void RefreshLocalHand()
    {
        if (localHand.Count > 0)
            GameEvents.RaiseHandUpdated(new List<Card>(localHand));
    }

    /// <summary>
    /// DrawPileView gọi để enable/disable nút bốc bài.
    /// Chạy trên CLIENT — so sánh với NetworkVariable.
    /// </summary>
    public bool IsLocalPlayersTurn()
    {
        if (localPlayerIndex < 0 && PlayerIndexMapper.Instance != null)
            localPlayerIndex = PlayerIndexMapper.Instance.GetLocalPlayerIndex();

        if (localPlayerIndex < 0) return false;
        return netCurrentPlayer.Value == localPlayerIndex
            && !netWaitingForWildColor.Value
            && !localInitialDealInProgress;
    }

    // ================================================================
    // VALIDATE LOGIC (dùng cả server và client)
    // ================================================================

    private bool IsValidPlayCheck(Card card)
    {
        if (card == null || waitingForWildColor) return false;

        if (IsServer && waitingForDrawnCardDecision)
            return IsSameCard(card, pendingDrawnCard);

        if (!IsServer && localWaitingForDrawnCardDecision)
            return IsSameCard(card, localPendingDrawnCard);

        Card top = IsServer ? GetTopDiscard() : localTopDiscard;
        if (top == null) return true;

        // Wild luôn đánh được
        if (card.Type == CardType.Wild || card.Type == CardType.WildDrawFour)
            return true;

        // Khớp màu
        if (card.Color == currentColor) return true;

        // Khớp số (chỉ Number cards)
        if (top.Type == CardType.Number && card.Type == CardType.Number && top.Number == card.Number)
            return true;

        // Khớp loại (action cards: Skip/Reverse/DrawTwo)
        if (card.Type != CardType.Number)
            return top.Type == card.Type;

        return false;
    }

    // ================================================================
    // SERVER RPCs — Client gọi lên Server
    // ================================================================

    /// <summary>
    /// Client gọi khi đánh 1 bài. Server validate rồi xử lý.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void PlayCardServerRpc(NetworkCard netCard, ServerRpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;
        int playerIndex = PlayerIndexMapper.Instance.GetPlayerIndex(senderId);

        // Validate: đúng lượt không?
        if (waitingForInitialDeal || playerIndex != currentPlayerIndex || waitingForWildColor)
        {
            RejectPlayClientRpc(CreateTargetParams(senderId));
            return;
        }

        if (waitingForDrawnCardDecision &&
            (playerIndex != pendingDrawnPlayer || !IsSameCard(netCard, pendingDrawnCard)))
        {
            RejectPlayClientRpc(CreateTargetParams(senderId));
            return;
        }

        // Tìm bài trong hand server
        Card card = FindCardInHand(hands[playerIndex], netCard);
        if (card == null || !IsValidPlayCheck(card))
        {
            RejectPlayClientRpc(CreateTargetParams(senderId));
            return;
        }

        // OK — xử lý bài đánh
        hands[playerIndex].Remove(card);
        ClearPendingDrawnCardDecision();
        ProcessPlayedCard(playerIndex, card);
    }

    /// <summary>
    /// Client gọi khi muốn bốc bài.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void DrawCardServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;
        int playerIndex = PlayerIndexMapper.Instance.GetPlayerIndex(senderId);

        if (waitingForInitialDeal ||
            playerIndex != currentPlayerIndex ||
            waitingForWildColor ||
            waitingForDrawnCardDecision)
        {
            return;
        }

        Card drawn = DrawOneToHand(playerIndex);
        if (drawn == null) return;

        bool playable = IsValidPlayCheck(drawn);

        // Gửi bài vừa bốc CHỈ cho client đó
        NetworkCard netDrawn = NetworkCard.FromCard(drawn);
        CardDrawnClientRpc(netDrawn, playable, CreateTargetParams(senderId));

        // Cập nhật hand count cho tất cả
        BroadcastOpponentHandCount(playerIndex);

        if (playable)
        {
            waitingForDrawnCardDecision = true;
            pendingDrawnCard = drawn;
            pendingDrawnPlayer = playerIndex;
        }
        else
        {
            MoveToNextPlayer(1);
            SyncTurnState();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void DeclineDrawnCardServerRpc(ServerRpcParams rpcParams = default)
    {
        if (!waitingForDrawnCardDecision)
            return;

        ulong senderId = rpcParams.Receive.SenderClientId;
        int playerIndex = PlayerIndexMapper.Instance.GetPlayerIndex(senderId);
        if (playerIndex != currentPlayerIndex || playerIndex != pendingDrawnPlayer)
            return;

        ClearPendingDrawnCardDecision();
        MoveToNextPlayer(1);
        SyncTurnState();
    }

    /// <summary>
    /// Client gọi khi chọn màu cho Wild card.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void SelectColorServerRpc(byte colorByte, ServerRpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;
        int playerIndex = PlayerIndexMapper.Instance.GetPlayerIndex(senderId);

        if (!waitingForWildColor || pendingWildPlayer != playerIndex) return;

        ApplyWildColorAndAdvance((CardColor)colorByte);
    }

    /// <summary>
    /// Client gọi khi hô UNO.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void CallUnoServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong senderId = rpcParams.Receive.SenderClientId;
        int playerIndex = PlayerIndexMapper.Instance.GetPlayerIndex(senderId);

        if (hands[playerIndex].Count == 1)
        {
            unoCalledThisTurn.Add(playerIndex);
        }
    }

    /// <summary>
    /// Client gọi khi bắt đối thủ quên hô UNO.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void CatchUnoServerRpc(int targetPlayerIndex, ServerRpcParams rpcParams = default)
    {
        if (targetPlayerIndex < 0 || targetPlayerIndex >= playerCount) return;
        if (hands[targetPlayerIndex].Count != 1) return;
        if (unoCalledThisTurn.Contains(targetPlayerIndex)) return;

        // Phạt: bốc 2 bài
        DrawCardsToPlayer(targetPlayerIndex, 2);
        BroadcastOpponentHandCount(targetPlayerIndex);

        // Gửi hand mới cho player bị phạt
        SendHandToClient(targetPlayerIndex);
    }

    /// <summary>
    /// Client gọi khi muốn round tiếp theo.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void NextRoundServerRpc()
    {
        if (!roundEnded || matchEnded)
            return;

        StartRound();
    }

    /// <summary>
    /// Client gọi khi muốn rematch.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void RematchServerRpc()
    {
        if (!matchEnded)
            return;

        InitializeMatchScores();
        StartRound();
    }

    // ================================================================
    // CLIENT RPCs — Server gửi xuống Client(s)
    // ================================================================

    /// <summary>
    /// Gửi hand riêng cho 1 client (chỉ client đó nhận được bài).
    /// </summary>
    [ClientRpc]
    private void SendHandClientRpc(NetworkCard[] cards, ClientRpcParams rpcParams = default)
    {
        // Client nhận bài của mình
        localHand.Clear();
        foreach (var nc in cards)
        {
            localHand.Add(nc.ToCard());
        }

        // Lưu localPlayerIndex nếu chưa có
        if (localPlayerIndex < 0)
        {
            localPlayerIndex = PlayerIndexMapper.Instance.GetLocalPlayerIndex();
        }

        // Raise event → HandView tự cập nhật UI
        GameEvents.RaiseHandUpdated(new List<Card>(localHand));
    }

    /// <summary>
    /// Starts the presentation only after every client has received its final initial hand.
    /// </summary>
    [ClientRpc]
    private void BeginInitialDealClientRpc(int cardsPerPlayer, int roundPlayerCount)
    {
        localInitialDealInProgress = true;
        SceneLoadingOverlay.SetMessage(DealingCardsMessage);

        if (!GameEvents.RaiseInitialDealStarted(roundPlayerCount, cardsPerPlayer))
        {
            HandleInitialDealCompleted();
        }
    }

    private void HandleInitialDealCompleted()
    {
        if (!localInitialDealInProgress || !IsClient)
        {
            return;
        }

        localInitialDealInProgress = false;
        SceneLoadingOverlay.Hide();
        InitialDealReadyServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void InitialDealReadyServerRpc(ServerRpcParams rpcParams = default)
    {
        if (!waitingForInitialDeal)
        {
            return;
        }

        ulong senderId = rpcParams.Receive.SenderClientId;
        if (PlayerIndexMapper.Instance.GetPlayerIndex(senderId) < 0)
        {
            return;
        }

        initialDealReadyClients.Add(senderId);
        if (initialDealReadyClients.Count >= playerCount)
        {
            FinishInitialDeal();
        }
    }

    /// <summary>
    /// Thông báo cho tất cả clients: có bài vừa được đánh.
    /// </summary>
    [ClientRpc]
    private void CardPlayedClientRpc(int playerIndex, NetworkCard netCard)
    {
        Card card = netCard.ToCard();
        localTopDiscard = card;

        if (IsSameCard(card, localPendingDrawnCard))
        {
            localWaitingForDrawnCardDecision = false;
            localPendingDrawnCard = null;
        }

        // Cập nhật discard pile trên UI
        GameEvents.RaiseDiscardChanged(card);

        // Nếu không phải Wild → cập nhật màu ngay
        if (card.Type != CardType.Wild && card.Type != CardType.WildDrawFour)
        {
            currentColor = card.Color;
            GameEvents.RaiseColorChanged(currentColor);
        }

        RefreshLocalHand();
    }

    /// <summary>
    /// Cập nhật số bài đối thủ (tất cả clients nhận).
    /// </summary>
    [ClientRpc]
    private void OpponentHandCountClientRpc(int playerIndex, int count)
    {
        // Nếu đây là local player → bỏ qua (HandView đã xử lý)
        if (playerIndex == localPlayerIndex) return;

        GameEvents.RaiseOpponentHandCountChanged(playerIndex, count);
    }

    /// <summary>
    /// Gửi bài vừa bốc CHỈ cho client bốc bài.
    /// </summary>
    [ClientRpc]
    private void CardDrawnClientRpc(NetworkCard netCard, bool isPlayable, ClientRpcParams rpcParams = default)
    {
        Card card = netCard.ToCard();
        localHand.Add(card);

        localWaitingForDrawnCardDecision = isPlayable;
        localPendingDrawnCard = isPlayable ? card : null;

        GameEvents.RaiseHandUpdated(new List<Card>(localHand));
        GameEvents.RaiseCardDrawn(card, isPlayable);
    }

    /// <summary>
    /// Yêu cầu 1 client mở color picker (Wild card).
    /// </summary>
    [ClientRpc]
    private void RequestColorSelectionClientRpc(ClientRpcParams rpcParams = default)
    {
        GameEvents.RaiseWildPlayed();
    }

    /// <summary>
    /// Yêu cầu 1 client hiện prompt UNO.
    /// </summary>
    [ClientRpc]
    private void ShowUnoPromptClientRpc(ClientRpcParams rpcParams = default)
    {
        GameEvents.RaiseUnoCallRequired();
    }

    /// <summary>
    /// Server từ chối bài đánh (không hợp lệ).
    /// </summary>
    [ClientRpc]
    private void RejectPlayClientRpc(ClientRpcParams rpcParams = default)
    {
        // Client nhận: refresh lại hand (bài không bị mất)
        GameEvents.RaiseHandUpdated(new List<Card>(localHand));
    }

    /// <summary>
    /// Round kết thúc — gửi kết quả cho tất cả.
    /// </summary>
    [ClientRpc]
    private void RoundEndClientRpc(int winnerIndex, int[] playerIndices, int[] scores)
    {
        var breakdown = new Dictionary<int, int>();
        for (int i = 0; i < playerIndices.Length; i++)
        {
            breakdown[playerIndices[i]] = scores[i];
        }

        GameEvents.RaiseRoundEnd(winnerIndex, breakdown);
    }

    /// <summary>
    /// Match kết thúc (đạt 500 điểm).
    /// </summary>
    [ClientRpc]
    private void MatchEndClientRpc(int winnerIndex, int totalScore)
    {
        GameEvents.RaiseMatchEnd(winnerIndex, totalScore);
    }

    /// <summary>
    /// Cập nhật màu hiện tại (sau Wild).
    /// </summary>
    [ClientRpc]
    private void ColorChangedClientRpc(byte colorByte)
    {
        currentColor = (CardColor)colorByte;
        GameEvents.RaiseColorChanged(currentColor);
        RefreshLocalHand();
    }

    [ClientRpc]
    private void SyncPresentationClientRpc(
        int playerIndex,
        int playDirection,
        byte colorByte,
        int deckCount)
    {
        currentColor = (CardColor)colorByte;
        GameEvents.RaiseTurnChanged(playerIndex);
        GameEvents.RaiseDirectionChanged(playDirection);
        GameEvents.RaiseColorChanged(currentColor);
        GameEvents.RaiseDeckCountChanged(deckCount);
    }

    // ================================================================
    // NETWORK VARIABLE CALLBACKS — Khi state thay đổi
    // ================================================================

    private void OnCurrentPlayerChanged(int oldVal, int newVal)
    {
        unoCalledThisTurn.Clear(); // local clear

        if (newVal != localPlayerIndex)
        {
            localWaitingForDrawnCardDecision = false;
            localPendingDrawnCard = null;
        }

        GameEvents.RaiseTurnChanged(newVal);

        // Refresh hand highlight (playable state có thể thay đổi)
        if (localHand.Count > 0)
        {
            GameEvents.RaiseHandUpdated(new List<Card>(localHand));
        }
    }

    private void OnDirectionChanged(int oldVal, int newVal)
    {
        GameEvents.RaiseDirectionChanged(newVal);
    }

    private void OnColorChanged(byte oldVal, byte newVal)
    {
        currentColor = (CardColor)newVal;
        RefreshLocalHand();
    }

    private void OnDrawPileCountChanged(int oldVal, int newVal)
    {
        GameEvents.RaiseDeckCountChanged(newVal);
    }

    private void OnWaitingForWildColorChanged(bool oldVal, bool newVal)
    {
        if (localHand.Count > 0)
            GameEvents.RaiseHandUpdated(new List<Card>(localHand));
    }

    // ================================================================
    // SERVER — GAME LOGIC (copy từ GameManager, bỏ AI logic)
    // ================================================================

    private void InitializeMatchScores()
    {
        matchScores.Clear();
        for (int i = 0; i < playerCount; i++)
        {
            matchScores[i] = 0;
        }
    }

    private void StartRound()
    {
        waitingForWildColor = false;
        waitingForInitialDeal = true;
        waitingForDrawnCardDecision = false;
        initialDealReadyClients.Clear();
        roundEnded = false;
        matchEnded = false;
        netWaitingForWildColor.Value = false;
        pendingWildCard = null;
        pendingDrawnCard = null;
        pendingWildPlayer = -1;
        pendingDrawnPlayer = -1;
        unoCalledThisTurn.Clear();
        direction = 1;
        currentPlayerIndex = 0;
        currentColor = CardColor.Red;

        hands.Clear();
        drawPile.Clear();
        discardPile.Clear();

        for (int i = 0; i < playerCount; i++)
        {
            hands.Add(new List<Card>());
        }

        BuildDeck(drawPile);
        Shuffle(drawPile);
        DealCards();
        StartDiscardPile();

        Debug.Log(
            $"[NetworkGameManager] Dealt {startingHandSize} cards to {playerCount} players. " +
            $"Draw pile: {drawPile.Count}, discard pile: {discardPile.Count}.");

        // Sync state xuống tất cả clients
        SyncFullState();
        BeginInitialDealClientRpc(startingHandSize, playerCount);

        // Gửi bài riêng cho từng client
        for (int i = 0; i < playerCount; i++)
        {
            SendHandToClient(i);
            BroadcastOpponentHandCount(i);
        }

    }

    private void FinishInitialDeal()
    {
        if (!waitingForInitialDeal)
        {
            return;
        }

        waitingForInitialDeal = false;
        netCurrentPlayer.Value = currentPlayerIndex;

        SyncPresentationClientRpc(
            currentPlayerIndex,
            direction,
            (byte)currentColor,
            drawPile.Count);
    }

    private void ProcessPlayedCard(int playerIndex, Card playedCard)
    {
        discardPile.Add(playedCard);
        if (playedCard.Type != CardType.Wild && playedCard.Type != CardType.WildDrawFour)
        {
            currentColor = playedCard.Color;
        }

        // Thông báo tất cả clients
        NetworkCard netCard = NetworkCard.FromCard(playedCard);
        CardPlayedClientRpc(playerIndex, netCard);

        // Cập nhật hand count
        BroadcastOpponentHandCount(playerIndex);

        // Gửi hand mới cho player đánh bài
        SendHandToClient(playerIndex);

        // Kiểm tra UNO
        CheckUnoPrompt(playerIndex);

        // Kiểm tra thắng
        if (hands[playerIndex].Count == 0)
        {
            EndRound(playerIndex);
            return;
        }

        // Wild card → yêu cầu chọn màu
        if (playedCard.Type == CardType.Wild || playedCard.Type == CardType.WildDrawFour)
        {
            waitingForWildColor = true;
            netWaitingForWildColor.Value = true;
            pendingWildCard = playedCard;
            pendingWildPlayer = playerIndex;

            // Gửi yêu cầu chọn màu CHỈ cho player đánh bài
            ulong clientId = PlayerIndexMapper.Instance.GetClientId(playerIndex);
            RequestColorSelectionClientRpc(CreateTargetParams(clientId));
            return;
        }

        // Áp dụng hiệu ứng bài
        AdvanceTurnByCardEffect(playedCard);
        SyncTurnState();
    }

    private void ApplyWildColorAndAdvance(CardColor selectedColor)
    {
        waitingForWildColor = false;
        netWaitingForWildColor.Value = false;
        currentColor = selectedColor;

        // Thông báo màu mới cho tất cả
        ColorChangedClientRpc((byte)currentColor);
        netCurrentColor.Value = (byte)currentColor;

        Card wildCard = pendingWildCard;
        pendingWildCard = null;
        pendingWildPlayer = -1;

        if (wildCard == null) return;

        AdvanceTurnByCardEffect(wildCard);
        SyncTurnState();
    }

    private void AdvanceTurnByCardEffect(Card playedCard)
    {
        switch (playedCard.Type)
        {
            case CardType.Reverse:
                direction *= -1;
                netDirection.Value = direction;
                MoveToNextPlayer(1);
                break;

            case CardType.Skip:
                MoveToNextPlayer(2); // bỏ qua 1 người
                break;

            case CardType.DrawTwo:
                int target2 = PeekNextPlayer();
                DrawCardsToPlayer(target2, 2);
                BroadcastOpponentHandCount(target2);
                SendHandToClient(target2);
                MoveToNextPlayer(2);
                break;

            case CardType.WildDrawFour:
                int target4 = PeekNextPlayer();
                DrawCardsToPlayer(target4, 4);
                BroadcastOpponentHandCount(target4);
                SendHandToClient(target4);
                MoveToNextPlayer(2);
                break;

            default:
                MoveToNextPlayer(1);
                break;
        }
    }

    private void CheckUnoPrompt(int playerIndex)
    {
        if (hands[playerIndex].Count != 1) return;

        ulong clientId = PlayerIndexMapper.Instance.GetClientId(playerIndex);
        ShowUnoPromptClientRpc(CreateTargetParams(clientId));
    }

    private void EndRound(int winnerIndex)
    {
        roundEnded = true;

        int roundScore = 0;
        var playerIndices = new List<int>();
        var scores = new List<int>();

        for (int i = 0; i < playerCount; i++)
        {
            int playerScore = 0;
            foreach (var card in hands[i])
            {
                playerScore += GetCardPoints(card);
            }

            playerIndices.Add(i);
            scores.Add(playerScore);

            if (i != winnerIndex)
            {
                roundScore += playerScore;
            }
        }

        matchScores[winnerIndex] += roundScore;

        // ClientRpc không nhận Dictionary → gửi 2 arrays
        RoundEndClientRpc(winnerIndex, playerIndices.ToArray(), scores.ToArray());

        if (matchScores[winnerIndex] >= matchPointTarget)
        {
            matchEnded = true;
            MatchEndClientRpc(winnerIndex, matchScores[winnerIndex]);
        }
    }

    // ================================================================
    // SERVER — SYNC HELPERS
    // ================================================================

    private void SyncFullState()
    {
        netDrawPileCount.Value = drawPile.Count;
        netCurrentPlayer.Value = waitingForInitialDeal ? -1 : currentPlayerIndex;
        netDirection.Value = direction;
        netCurrentColor.Value = (byte)currentColor;

        // Gửi discard top
        Card top = GetTopDiscard();
        if (top != null)
        {
            CardPlayedClientRpc(-1, NetworkCard.FromCard(top)); // -1 = initial
        }
    }

    private void SyncTurnState()
    {
        unoCalledThisTurn.Clear();
        netCurrentPlayer.Value = currentPlayerIndex;
        netDrawPileCount.Value = drawPile.Count;
    }

    private void SendHandToClient(int playerIndex)
    {
        ulong clientId = PlayerIndexMapper.Instance.GetClientId(playerIndex);

        var netCards = new NetworkCard[hands[playerIndex].Count];
        for (int i = 0; i < hands[playerIndex].Count; i++)
        {
            netCards[i] = NetworkCard.FromCard(hands[playerIndex][i]);
        }

        SendHandClientRpc(netCards, CreateTargetParams(clientId));
        Debug.Log(
            $"[NetworkGameManager] Sent {netCards.Length} cards to player {playerIndex}, clientId {clientId}.");
    }

    private void BroadcastOpponentHandCount(int playerIndex)
    {
        OpponentHandCountClientRpc(playerIndex, hands[playerIndex].Count);
    }

    /// <summary>
    /// Tạo ClientRpcParams để gửi ClientRpc CHỈ cho 1 client cụ thể.
    /// </summary>
    private ClientRpcParams CreateTargetParams(ulong clientId)
    {
        return new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { clientId }
            }
        };
    }

    // ================================================================
    // SERVER — DECK/CARD OPERATIONS (giống GameManager)
    // ================================================================

    private void BuildDeck(List<Card> deck)
    {
        deck.Clear();
        CardColor[] colors = { CardColor.Red, CardColor.Green, CardColor.Blue, CardColor.Yellow };

        foreach (var color in colors)
        {
            deck.Add(new Card { Color = color, Type = CardType.Number, Number = 0 });

            for (int n = 1; n <= 9; n++)
            {
                deck.Add(new Card { Color = color, Type = CardType.Number, Number = n });
                deck.Add(new Card { Color = color, Type = CardType.Number, Number = n });
            }

            deck.Add(new Card { Color = color, Type = CardType.Skip, Number = -1 });
            deck.Add(new Card { Color = color, Type = CardType.Skip, Number = -1 });
            deck.Add(new Card { Color = color, Type = CardType.Reverse, Number = -1 });
            deck.Add(new Card { Color = color, Type = CardType.Reverse, Number = -1 });
            deck.Add(new Card { Color = color, Type = CardType.DrawTwo, Number = -1 });
            deck.Add(new Card { Color = color, Type = CardType.DrawTwo, Number = -1 });
        }

        for (int i = 0; i < 4; i++)
        {
            deck.Add(new Card { Color = CardColor.None, Type = CardType.Wild, Number = -1 });
            deck.Add(new Card { Color = CardColor.None, Type = CardType.WildDrawFour, Number = -1 });
        }
    }

    private void DealCards()
    {
        for (int c = 0; c < startingHandSize; c++)
        {
            for (int p = 0; p < playerCount; p++)
            {
                DrawOneToHand(p);
            }
        }
    }

    private void StartDiscardPile()
    {
        Card first = DrawOneFromDeck();
        while (first != null && (first.Type == CardType.Wild || first.Type == CardType.WildDrawFour))
        {
            drawPile.Add(first);
            first = DrawOneFromDeck();
        }

        if (first == null) return;

        discardPile.Add(first);
        currentColor = first.Color;

        // Áp dụng hiệu ứng bài đầu tiên
        switch (first.Type)
        {
            case CardType.Skip:
                MoveToNextPlayer(1);
                break;
            case CardType.Reverse:
                direction *= -1;
                break;
            case CardType.DrawTwo:
                DrawCardsToPlayer(currentPlayerIndex, 2);
                MoveToNextPlayer(1);
                break;
        }
    }

    private Card DrawOneToHand(int playerIndex)
    {
        Card drawn = DrawOneFromDeck();
        if (drawn == null) return null;
        hands[playerIndex].Add(drawn);
        return drawn;
    }

    private void DrawCardsToPlayer(int playerIndex, int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (DrawOneToHand(playerIndex) == null) break;
        }
    }

    private Card DrawOneFromDeck()
    {
        if (drawPile.Count == 0) RefillDeckFromDiscard();
        if (drawPile.Count == 0) return null;

        int last = drawPile.Count - 1;
        Card card = drawPile[last];
        drawPile.RemoveAt(last);
        return card;
    }

    private void RefillDeckFromDiscard()
    {
        if (discardPile.Count <= 1) return;

        Card top = discardPile[discardPile.Count - 1];
        discardPile.RemoveAt(discardPile.Count - 1);

        drawPile.AddRange(discardPile);
        discardPile.Clear();
        discardPile.Add(top);
        Shuffle(drawPile);
    }

    private Card GetTopDiscard()
    {
        return discardPile.Count == 0 ? null : discardPile[discardPile.Count - 1];
    }

    private int PeekNextPlayer()
    {
        return Mod(currentPlayerIndex + direction, playerCount);
    }

    private void MoveToNextPlayer(int steps)
    {
        currentPlayerIndex = Mod(currentPlayerIndex + (direction * steps), playerCount);
    }

    /// <summary>
    /// Tìm Card trong hand server dựa trên NetworkCard.Id
    /// </summary>
    private Card FindCardInHand(List<Card> hand, NetworkCard netCard)
    {
        foreach (var card in hand)
        {
            if (card.NetworkId == netCard.Id)
                return card;
        }
        return null;
    }

    private void ClearPendingDrawnCardDecision()
    {
        waitingForDrawnCardDecision = false;
        pendingDrawnCard = null;
        pendingDrawnPlayer = -1;
    }

    private static bool IsSameCard(NetworkCard netCard, Card card)
    {
        return card != null && netCard.Id == card.NetworkId;
    }

    private static bool IsSameCard(Card first, Card second)
    {
        return first != null && second != null && first.NetworkId == second.NetworkId;
    }

    private static int GetCardPoints(Card card)
    {
        if (card == null) return 0;
        if (card.Type == CardType.Number) return card.Number;
        if (card.Type == CardType.Wild || card.Type == CardType.WildDrawFour) return 50;
        return 20; // Skip, Reverse, DrawTwo
    }

    private static void Shuffle(List<Card> cards)
    {
        for (int i = cards.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            Card temp = cards[i];
            cards[i] = cards[j];
            cards[j] = temp;
        }
    }

    private static int Mod(int value, int mod)
    {
        int result = value % mod;
        return result < 0 ? result + mod : result;
    }
}
