using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Game coordinator — singleton hub kết nối tất cả subsystems.
/// Orchestrate game flow: StartRound → Deal → Play → EndRound.
/// Implement IGameLogic cho View layer query trạng thái.
/// </summary>
public class GameManager : MonoBehaviour, IGameLogic
{
    private static GameManager __instance;

    [Header("Module References")]
    [SerializeField] private TurnManager turnManager;
    [SerializeField] private Deck deck;
    [SerializeField] private PlayedZone playedZone;
    [SerializeField] private CardHolder cardHolder;
    [SerializeField] private ScoringSystem scoringSystem;

    [Header("Players")]
    [SerializeField] private List<Player> players;
    [SerializeField] private int localPlayerIndex = 0;
    [SerializeField] private int startingHandSize = 7;

    public static GameManager Instance
    {
        get
        {
            if (__instance == null)
            {
                __instance = FindAnyObjectByType<GameManager>();
                if (__instance == null)
                {
                    GameObject obj = new GameObject("GameManager");
                    __instance = obj.AddComponent<GameManager>();
                }
                DontDestroyOnLoad(__instance.gameObject);
            }
            return __instance;
        }
    }

    public void Awake()
    {
        if (__instance != null && __instance != this)
        {
            Destroy(gameObject);
            return;
        }
        __instance = this;
        DontDestroyOnLoad(gameObject);

        ValidateReferences();
    }

    private void OnEnable()
    {
        GameEvents.OnCardPlayed += HandleCardPlayed;
        GameEvents.OnDrawCardRequested += HandleDrawCardRequested;
        GameEvents.OnColorSelected += HandleColorSelected;
        GameEvents.OnUnoCalled += HandleUnoCalled;
        GameEvents.OnCatchUno += HandleCatchUno;
        GameEvents.OnNextRoundRequested += HandleNextRoundRequested;
        GameEvents.OnRematchRequested += HandleRematchRequested;
    }

    private void OnDisable()
    {
        GameEvents.OnCardPlayed -= HandleCardPlayed;
        GameEvents.OnDrawCardRequested -= HandleDrawCardRequested;
        GameEvents.OnColorSelected -= HandleColorSelected;
        GameEvents.OnUnoCalled -= HandleUnoCalled;
        GameEvents.OnCatchUno -= HandleCatchUno;
        GameEvents.OnNextRoundRequested -= HandleNextRoundRequested;
        GameEvents.OnRematchRequested -= HandleRematchRequested;
    }

    private void Start()
    {
        if (scoringSystem != null)
        {
            scoringSystem.InitializeScores(players.Count);
        }
        StartRound();
    }

    // ─── IGameLogic ─────────────────────────────────────────────────────

    public bool IsValidPlay(CardGameObject card)
    {
        if (!IsLocalPlayersTurn())
            return false;

        return CardValidator.IsValidPlay(card, playedZone.GetTopCard(),
            turnManager.GetCurrentCardColor(), turnManager.GetCurrentDrawAmount());
    }

    public bool IsLocalPlayersTurn()
    {
        return turnManager.GetCurrentTurnData().PlayerId == localPlayerIndex
            && !turnManager.IsWaitingForWildColor();
    }

    // ─── Game Flow ──────────────────────────────────────────────────────

    public void StartRound()
    {
        // Reset tất cả players
        foreach (var player in players)
        {
            player.ResetForNewRound();
        }

        // Reset turn state
        turnManager.ResetForNewRound();

        // Deal cards
        DealCards(startingHandSize);

        // Start discard pile
        StartDiscardPile();

        // Broadcast full state cho UI
        BroadcastFullState();
    }

    private void DealCards(int cardsPerPlayer)
    {
        for (int c = 0; c < cardsPerPlayer; c++)
        {
            for (int p = 0; p < players.Count; p++)
            {
                players[p].DrawCard(1);
            }
        }
    }

    /// <summary>
    /// Lật lá đầu tiên làm discard pile — bỏ qua Wild cards (re-draw).
    /// </summary>
    private void StartDiscardPile()
    {
        List<CardGameObject> drawn = deck.DrawCard(1);
        while (drawn.Count > 0 && drawn[0].GetColor() == CardColor.Wild)
        {
            // Đưa Wild card trở lại deck (sẽ bị shuffle lại khi cần)
            drawn[0].gameObject.transform.SetParent(cardHolder.gameObject.transform);
            drawn = deck.DrawCard(1);
        }

        if (drawn.Count == 0)
        {
            Debug.LogError("Cannot start discard pile — deck is empty!");
            return;
        }

        CardGameObject firstCard = drawn[0];
        playedZone.AddCard(firstCard);
        turnManager.SetCurrentCardColor(firstCard.GetColor());

        // Áp dụng hiệu ứng lá bài đầu tiên
        if (firstCard.GetCardType() == CardType.Action)
        {
            CardEffectResolver.ResolveEffects(firstCard, turnManager);
        }
    }

    public void EndRound(int winnerIndex)
    {
        Dictionary<int, int> breakdown = scoringSystem.CalculateRoundScore(winnerIndex, players);
        GameEvents.RaiseRoundEnd(winnerIndex, breakdown);

        if (scoringSystem.IsMatchOver(winnerIndex))
        {
            GameEvents.RaiseMatchEnd(winnerIndex, scoringSystem.GetMatchScore(winnerIndex));
        }
    }

    // ─── Event Handlers ─────────────────────────────────────────────────

    private void HandleCardPlayed(CardGameObject playedCard)
    {
        // Delegate tới TurnManager/PlayingPhase xử lý
        // PlayingPhase sẽ validate và process card qua CardValidator + CardEffectResolver
    }

    private void HandleDrawCardRequested()
    {
        // Delegate tới TurnManager/PlayingPhase
    }

    private void HandleColorSelected(CardColor color)
    {
        turnManager.ApplyWildColor(color);
    }

    private void HandleUnoCalled()
    {
        Player localPlayer = GetPlayer(localPlayerIndex);
        if (localPlayer != null && localPlayer.GetHandCount() == 1)
        {
            localPlayer.HasCalledUno = true;
        }
    }

    private void HandleCatchUno(int opponentIndex)
    {
        if (opponentIndex < 0 || opponentIndex >= players.Count || opponentIndex == localPlayerIndex)
            return;

        Player opponent = players[opponentIndex];
        if (opponent.GetHandCount() != 1 || opponent.HasCalledUno)
            return;

        // Phạt: rút 2 lá
        opponent.DrawCard(2);
        NotifyHandSizeChanged(opponentIndex);
    }

    private void HandleNextRoundRequested()
    {
        StartRound();
    }

    private void HandleRematchRequested()
    {
        scoringSystem.ResetMatchScores();
        scoringSystem.InitializeScores(players.Count);
        StartRound();
    }

    // ─── Broadcasting ───────────────────────────────────────────────────

    public void BroadcastFullState()
    {
        GameEvents.RaiseDeckCountChanged(deck.GetRemainingCount());
        GameEvents.RaiseDiscardChanged(playedZone.GetTopCard());
        GameEvents.RaiseColorChanged(turnManager.GetCurrentCardColor());
        GameEvents.RaiseDirectionChanged(turnManager.GetDirection());
        GameEvents.RaiseDrawStackChanged(turnManager.GetCurrentDrawAmount());
        BroadcastTurnState();
        BroadcastLocalHand();

        for (int i = 0; i < players.Count; i++)
        {
            if (i == localPlayerIndex) continue;
            GameEvents.RaiseOpponentHandCountChanged(i, players[i].GetHandCount());
        }
    }

    public void BroadcastTurnState()
    {
        // Reset UNO call status mỗi turn mới
        foreach (var player in players)
        {
            player.HasCalledUno = false;
        }
        GameEvents.RaiseTurnChanged(turnManager.GetCurrentTurnData().PlayerId);
        BroadcastLocalHand();
    }

    public void BroadcastLocalHand()
    {
        Player localPlayer = GetPlayer(localPlayerIndex);
        if (localPlayer != null)
        {
            GameEvents.RaiseHandUpdated(new List<CardGameObject>(localPlayer.GetHandCards()));
        }
    }

    public void NotifyHandSizeChanged(int playerIndex)
    {
        if (playerIndex == localPlayerIndex)
        {
            BroadcastLocalHand();
            return;
        }
        GameEvents.RaiseOpponentHandCountChanged(playerIndex, players[playerIndex].GetHandCount());
    }

    // ─── Getters ────────────────────────────────────────────────────────

    public TurnManager GetTurnManager() => turnManager;
    public Deck GetDeck() => deck;
    public PlayedZone GetPlayedZone() => playedZone;
    public CardHolder GetCardHolder() => cardHolder;
    public ScoringSystem GetScoringSystem() => scoringSystem;
    public List<Player> GetPlayers() => players;
    public int GetLocalPlayerIndex() => localPlayerIndex;
    public int GetPlayerCount() => players.Count;

    public Player GetPlayer(int index)
    {
        if (index >= 0 && index < players.Count)
            return players[index];
        return null;
    }

    public Player GetCurrentPlayer()
    {
        return GetPlayer(turnManager.GetCurrentTurnData().PlayerId);
    }

    private void ValidateReferences()
    {
        if (playedZone == null) Debug.LogError("PlayedZone reference is not set in GameManager!");
        if (deck == null) Debug.LogError("Deck reference is not set in GameManager!");
        if (turnManager == null) Debug.LogError("TurnManager reference is not set in GameManager!");
        if (cardHolder == null) Debug.LogError("CardHolder reference is not set in GameManager!");
        if (scoringSystem == null) Debug.LogError("ScoringSystem reference is not set in GameManager!");
        if (players == null || players.Count == 0) Debug.LogError("No players assigned in GameManager!");
    }
}
