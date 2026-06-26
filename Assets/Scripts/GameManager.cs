using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour, IGameLogic
{
    private class Player
    {
        public int Index { get; }

        public Player(int index)
        {
            Index = index;
        }
    }
    public static GameManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    [Header("Players")]
    [SerializeField] private int playerCount = 4;
    [SerializeField] private int localPlayerIndex = 0;
    [SerializeField] private int startingHandSize = 7;

    [Header("Rules")]
    [SerializeField] private int matchPointTarget = 500;
    [SerializeField] private float aiTurnDelaySeconds = 0.6f;

    private readonly List<List<Card>> hands = new List<List<Card>>();
    private readonly List<Card> drawPile = new List<Card>();
    private readonly List<Card> discardPile = new List<Card>();
    private readonly Dictionary<int, int> matchScores = new Dictionary<int, int>();
    private readonly HashSet<int> unoCalledThisTurn = new HashSet<int>();

    private int currentPlayerIndex;
    private int direction = 1;
    private CardColor currentColor = CardColor.Red;

    private bool waitingForWildColor;
    private bool initialDealInProgress;
    private bool waitingForDrawnCardDecision;
    private Card pendingWildCard;
    private Card pendingDrawnCard;
    private int pendingWildPlayer = -1;

    private Coroutine aiTurnRoutine;

    private void OnEnable()
    {
        GameEvents.OnCardPlayed += HandleCardPlayed;
        GameEvents.OnDrawCardRequested += HandleDrawCardRequested;
        GameEvents.OnDrawnCardDeclined += HandleDrawnCardDeclined;
        GameEvents.OnColorSelected += HandleColorSelected;
        GameEvents.OnUnoCalled += HandleUnoCalled;
        GameEvents.OnCatchUno += HandleCatchUno;
        GameEvents.OnNextRoundRequested += HandleNextRoundRequested;
        GameEvents.OnRematchRequested += HandleRematchRequested;
        GameEvents.OnInitialDealCompleted += HandleInitialDealCompleted;
    }

    private void OnDisable()
    {
        GameEvents.OnCardPlayed -= HandleCardPlayed;
        GameEvents.OnDrawCardRequested -= HandleDrawCardRequested;
        GameEvents.OnDrawnCardDeclined -= HandleDrawnCardDeclined;
        GameEvents.OnColorSelected -= HandleColorSelected;
        GameEvents.OnUnoCalled -= HandleUnoCalled;
        GameEvents.OnCatchUno -= HandleCatchUno;
        GameEvents.OnNextRoundRequested -= HandleNextRoundRequested;
        GameEvents.OnRematchRequested -= HandleRematchRequested;
        GameEvents.OnInitialDealCompleted -= HandleInitialDealCompleted;
    }

    private void Start()
    {
        InitializeMatchScores();
        StartRound();
        SoundManager.Instance.ChangeBackGroundMusic("GameBackGroundMusic");
    }

    public bool IsValidPlay(Card card)
    {
        if (!IsLocalPlayersTurn())
        {
            return false;
        }

        return IsValidPlayForPlayer(card, localPlayerIndex);
    }

    private bool IsValidPlayForPlayer(Card card, int playerIndex)
    {
        if (card == null || waitingForWildColor)
        {
            return false;
        }

        if (waitingForDrawnCardDecision)
        {
            return IsSameCard(card, pendingDrawnCard);
        }

        Card top = GetTopDiscard();
        if (top == null)
        {
            return true;
        }

        if (card.Type == CardType.Wild)
        {
            return true;
        }

        if (card.Type == CardType.WildDrawFour)
        {
            return true;
        }

        if (card.Color == currentColor)
        {
            return true;
        }

        if (top.Type == CardType.Number && card.Type == CardType.Number && top.Number == card.Number)
        {
            return true;
        }

        // Chỉ áp dụng khớp type cho action cards (Skip/Reverse/DrawTwo), không phải Number
        if (card.Type != CardType.Number)
        {
            return top.Type == card.Type;
        }

        return false;
    }

    public bool IsLocalPlayersTurn()
    {
        return currentPlayerIndex == localPlayerIndex
            && !waitingForWildColor
            && !initialDealInProgress;
    }

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
        if (aiTurnRoutine != null)
        {
            StopCoroutine(aiTurnRoutine);
            aiTurnRoutine = null;
        }

        waitingForWildColor = false;
        initialDealInProgress = true;
        waitingForDrawnCardDecision = false;
        pendingWildCard = null;
        pendingDrawnCard = null;
        pendingWildPlayer = -1;
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
        BroadcastFullState();

        if (!GameEvents.RaiseInitialDealStarted(playerCount, startingHandSize))
        {
            HandleInitialDealCompleted();
        }
    }

    private void HandleInitialDealCompleted()
    {
        if (!initialDealInProgress)
        {
            return;
        }

        initialDealInProgress = false;
        BroadcastTurnState();
        TryRunAiTurn();
    }

    private void BuildDeck(List<Card> deck)
    {
        deck.Clear();
        CardColor[] colors = { CardColor.Red, CardColor.Green, CardColor.Blue, CardColor.Yellow };

        for (int c = 0; c < colors.Length; c++)
        {
            CardColor color = colors[c];
            deck.Add(CreateNumber(color, 0));

            for (int number = 1; number <= 9; number++)
            {
                deck.Add(CreateNumber(color, number));
                deck.Add(CreateNumber(color, number));
            }

            deck.Add(CreateAction(color, CardType.Skip));
            deck.Add(CreateAction(color, CardType.Skip));
            deck.Add(CreateAction(color, CardType.Reverse));
            deck.Add(CreateAction(color, CardType.Reverse));
            deck.Add(CreateAction(color, CardType.DrawTwo));
            deck.Add(CreateAction(color, CardType.DrawTwo));
        }

        for (int i = 0; i < 4; i++)
        {
            deck.Add(CreateWild(CardType.Wild));
            deck.Add(CreateWild(CardType.WildDrawFour));
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

        if (first == null)
        {
            return;
        }

        discardPile.Add(first);
        if (first.Type == CardType.Wild || first.Type == CardType.WildDrawFour)
        {
            currentColor = GetRandomColor();
        }
        else
        {
            currentColor = first.Color;
        }

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
                NotifyHandSizeChanged(currentPlayerIndex);
                MoveToNextPlayer(1);
                break;
        }
    }

    private void HandleCardPlayed(Card playedCard)
    {
        if (playedCard == null || waitingForWildColor || !IsLocalPlayersTurn())
        {
            return;
        }

        if (waitingForDrawnCardDecision && !IsSameCard(playedCard, pendingDrawnCard))
        {
            BroadcastLocalHand();
            return;
        }

        List<Card> localHand = hands[localPlayerIndex];
        Card handCard = FindAndRemoveCard(localHand, playedCard);
        if (handCard == null || !IsValidPlayForPlayer(handCard, localPlayerIndex))
        {
            if (handCard != null)
            {
                localHand.Add(handCard);
            }
            BroadcastLocalHand();
            return;
        }

        waitingForDrawnCardDecision = false;
        pendingDrawnCard = null;
        ProcessPlayedCard(localPlayerIndex, handCard);
    }

    private void ProcessPlayedCard(int playerIndex, Card playedCard)
    {
        discardPile.Add(playedCard);
        currentColor = playedCard.Type == CardType.Wild || playedCard.Type == CardType.WildDrawFour
            ? currentColor
            : playedCard.Color;

        GameEvents.RaiseDiscardChanged(playedCard);
        if (playedCard.Type != CardType.Wild && playedCard.Type != CardType.WildDrawFour)
        {
            GameEvents.RaiseColorChanged(currentColor);
        }

        NotifyHandSizeChanged(playerIndex);
        CheckUnoPrompt(playerIndex);

        if (hands[playerIndex].Count == 0)
        {
            EndRound(playerIndex);
            return;
        }

        if (playedCard.Type == CardType.Wild || playedCard.Type == CardType.WildDrawFour)
        {
            waitingForWildColor = true;
            pendingWildCard = playedCard;
            pendingWildPlayer = playerIndex;

            if (playerIndex == localPlayerIndex)
            {
                GameEvents.RaiseWildPlayed();
            }
            else
            {
                CardColor aiColor = PickBestColorForPlayer(playerIndex);
                ApplyWildColorAndAdvance(aiColor);
            }

            return;
        }

        AdvanceTurnByCardEffect(playedCard);
        BroadcastTurnState();
        TryRunAiTurn();
    }

    private void HandleDrawCardRequested()
    {
        if (!IsLocalPlayersTurn() || waitingForWildColor || waitingForDrawnCardDecision)
        {
            return;
        }

        Card drawn = DrawOneToHand(localPlayerIndex);
        if (drawn == null)
        {
            return;
        }

        bool playable = IsValidPlayForPlayer(drawn, localPlayerIndex);
        if (playable)
        {
            waitingForDrawnCardDecision = true;
            pendingDrawnCard = drawn;
        }

        BroadcastLocalHand();
        GameEvents.RaiseCardDrawn(drawn, playable);

        if (playable)
        {
            return;
        }
        else
        {
            MoveToNextPlayer(1);
            BroadcastTurnState();
            TryRunAiTurn();
        }
    }

    private void HandleDrawnCardDeclined()
    {
        if (!waitingForDrawnCardDecision || !IsLocalPlayersTurn())
        {
            return;
        }

        waitingForDrawnCardDecision = false;
        pendingDrawnCard = null;

        MoveToNextPlayer(1);
        BroadcastTurnState();
        TryRunAiTurn();
    }

    private void HandleColorSelected(CardColor color)
    {
        if (!waitingForWildColor || pendingWildPlayer != localPlayerIndex)
        {
            return;
        }

        ApplyWildColorAndAdvance(color);
    }

    private void ApplyWildColorAndAdvance(CardColor selectedColor)
    {
        waitingForWildColor = false;
        currentColor = selectedColor;
        GameEvents.RaiseColorChanged(currentColor);

        Card wildCard = pendingWildCard;
        pendingWildCard = null;
        pendingWildPlayer = -1;

        if (wildCard == null)
        {
            return;
        }

        AdvanceTurnByCardEffect(wildCard);
        BroadcastTurnState();
        TryRunAiTurn();
    }

    private void HandleUnoCalled()
    {
        if (hands[localPlayerIndex].Count == 1)
        {
            unoCalledThisTurn.Add(localPlayerIndex);
        }
    }

    private void HandleCatchUno(int opponentIndex)
    {
        if (opponentIndex < 0 || opponentIndex >= playerCount || opponentIndex == localPlayerIndex)
        {
            return;
        }

        if (hands[opponentIndex].Count != 1 || unoCalledThisTurn.Contains(opponentIndex))
        {
            return;
        }

        DrawCardsToPlayer(opponentIndex, 2);
        NotifyHandSizeChanged(opponentIndex);
    }

    private void HandleNextRoundRequested()
    {
        StartRound();
    }

    private void HandleRematchRequested()
    {
        InitializeMatchScores();
        StartRound();
    }

    private void AdvanceTurnByCardEffect(Card playedCard)
    {
        if (playedCard.Type == CardType.Reverse)
        {
            direction *= -1;
            GameEvents.RaiseDirectionChanged(direction);
            MoveToNextPlayer(1);
            return;
        }

        if (playedCard.Type == CardType.Skip)
        {
            MoveToNextPlayer(2);
            return;
        }

        if (playedCard.Type == CardType.DrawTwo)
        {
            int target = PeekNextPlayer();
            DrawCardsToPlayer(target, 2);
            NotifyHandSizeChanged(target);
            MoveToNextPlayer(2);
            return;
        }

        if (playedCard.Type == CardType.WildDrawFour)
        {
            int target = PeekNextPlayer();
            DrawCardsToPlayer(target, 4);
            NotifyHandSizeChanged(target);
            MoveToNextPlayer(2);
            return;
        }

        MoveToNextPlayer(1);
    }

    private void TryRunAiTurn()
    {
        if (currentPlayerIndex == localPlayerIndex || waitingForWildColor)
        {
            return;
        }

        if (aiTurnRoutine != null)
        {
            StopCoroutine(aiTurnRoutine);
        }

        aiTurnRoutine = StartCoroutine(RunAiTurnRoutine());
    }

    private IEnumerator RunAiTurnRoutine()
    {
        yield return new WaitForSeconds(aiTurnDelaySeconds);

        int aiIndex = currentPlayerIndex;
        List<Card> aiHand = hands[aiIndex];
        Card playable = FindFirstPlayable(aiHand, aiIndex);

        if (playable == null)
        {
            Card drawn = DrawOneToHand(aiIndex);
            NotifyHandSizeChanged(aiIndex);

            if (drawn != null && IsValidPlayForPlayer(drawn, aiIndex))
            {
                FindAndRemoveCard(aiHand, drawn);
                ProcessPlayedCard(aiIndex, drawn);
                aiTurnRoutine = null;
                yield break;
            }

            MoveToNextPlayer(1);
            BroadcastTurnState();
            TryRunAiTurn();
            aiTurnRoutine = null;
            yield break;
        }

        FindAndRemoveCard(aiHand, playable);
        ProcessPlayedCard(aiIndex, playable);
        aiTurnRoutine = null;
    }

    private Card FindFirstPlayable(List<Card> hand, int playerIndex)
    {
        for (int i = 0; i < hand.Count; i++)
        {
            if (IsValidPlayForPlayer(hand[i], playerIndex))
            {
                return hand[i];
            }
        }

        return null;
    }

    private void EndRound(int winnerIndex)
    {
        int roundScore = 0;
        Dictionary<int, int> breakdown = new Dictionary<int, int>();

        for (int i = 0; i < playerCount; i++)
        {
            int playerScore = 0;
            List<Card> hand = hands[i];
            for (int c = 0; c < hand.Count; c++)
            {
                playerScore += GetCardPoints(hand[c]);
            }

            breakdown[i] = playerScore;
            if (i != winnerIndex)
            {
                roundScore += playerScore;
            }
        }

        matchScores[winnerIndex] += roundScore;
        GameEvents.RaiseRoundEnd(winnerIndex, breakdown);

        if (matchScores[winnerIndex] >= matchPointTarget)
        {
            GameEvents.RaiseMatchEnd(winnerIndex, matchScores[winnerIndex]);
        }
    }

    private static int GetCardPoints(Card card)
    {
        if (card == null)
        {
            return 0;
        }

        if (card.Type == CardType.Number)
        {
            return card.Number;
        }

        if (card.Type == CardType.Wild || card.Type == CardType.WildDrawFour)
        {
            return 50;
        }

        return 20;
    }

    private void BroadcastFullState()
    {
        GameEvents.RaiseDeckCountChanged(drawPile.Count);
        GameEvents.RaiseDiscardChanged(GetTopDiscard());
        GameEvents.RaiseColorChanged(currentColor);
        GameEvents.RaiseDirectionChanged(direction);
        BroadcastLocalHand();

        for (int i = 0; i < playerCount; i++)
        {
            if (i == localPlayerIndex)
            {
                continue;
            }

            GameEvents.RaiseOpponentHandCountChanged(i, hands[i].Count);
        }
    }

    private void BroadcastLocalHand()
    {
        GameEvents.RaiseHandUpdated(new List<Card>(hands[localPlayerIndex]));
    }

    private void BroadcastTurnState()
    {
        unoCalledThisTurn.Clear();
        GameEvents.RaiseTurnChanged(currentPlayerIndex);
        BroadcastLocalHand();
    }

    private void NotifyHandSizeChanged(int playerIndex)
    {
        if (playerIndex == localPlayerIndex)
        {
            BroadcastLocalHand();
            return;
        }

        GameEvents.RaiseOpponentHandCountChanged(playerIndex, hands[playerIndex].Count);
    }

    private void CheckUnoPrompt(int playerIndex)
    {
        if (hands[playerIndex].Count != 1)
        {
            return;
        }

        if (playerIndex == localPlayerIndex)
        {
            GameEvents.RaiseUnoCallRequired();
        }
    }

    private int PeekNextPlayer()
    {
        return Mod(currentPlayerIndex + direction, playerCount);
    }

    private void MoveToNextPlayer(int steps)
    {
        currentPlayerIndex = Mod(currentPlayerIndex + (direction * steps), playerCount);
    }

    private Card DrawOneToHand(int playerIndex)
    {
        Card drawn = DrawOneFromDeck();
        if (drawn == null)
        {
            return null;
        }

        hands[playerIndex].Add(drawn);
        GameEvents.RaiseDeckCountChanged(drawPile.Count);
        return drawn;
    }

    private void DrawCardsToPlayer(int playerIndex, int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (DrawOneToHand(playerIndex) == null)
            {
                break;
            }
        }
    }

    private Card DrawOneFromDeck()
    {
        if (drawPile.Count == 0)
        {
            RefillDeckFromDiscard();
        }

        if (drawPile.Count == 0)
        {
            return null;
        }

        int last = drawPile.Count - 1;
        Card card = drawPile[last];
        drawPile.RemoveAt(last);
        return card;
    }

    private void RefillDeckFromDiscard()
    {
        if (discardPile.Count <= 1)
        {
            return;
        }

        Card top = discardPile[discardPile.Count - 1];
        discardPile.RemoveAt(discardPile.Count - 1);

        drawPile.AddRange(discardPile);
        discardPile.Clear();
        discardPile.Add(top);
        Shuffle(drawPile);
    }

    private Card GetTopDiscard()
    {
        if (discardPile.Count == 0)
        {
            return null;
        }

        return discardPile[discardPile.Count - 1];
    }

    private CardColor PickBestColorForPlayer(int playerIndex)
    {
        int red = 0;
        int green = 0;
        int blue = 0;
        int yellow = 0;

        List<Card> hand = hands[playerIndex];
        for (int i = 0; i < hand.Count; i++)
        {
            switch (hand[i].Color)
            {
                case CardColor.Red:
                    red++;
                    break;
                case CardColor.Green:
                    green++;
                    break;
                case CardColor.Blue:
                    blue++;
                    break;
                case CardColor.Yellow:
                    yellow++;
                    break;
            }
        }

        CardColor best = CardColor.Red;
        int bestValue = red;

        if (green > bestValue)
        {
            best = CardColor.Green;
            bestValue = green;
        }

        if (blue > bestValue)
        {
            best = CardColor.Blue;
            bestValue = blue;
        }

        if (yellow > bestValue)
        {
            best = CardColor.Yellow;
        }

        return best;
    }

    private static Card FindAndRemoveCard(List<Card> hand, Card target)
    {
        for (int i = 0; i < hand.Count; i++)
        {
            Card card = hand[i];
            if (IsSameCard(card, target))
            {
                hand.RemoveAt(i);
                return card;
            }
        }

        return null;
    }

    private static bool IsSameCard(Card a, Card b)
    {
        if (a == null || b == null)
        {
            return false;
        }

        return a.Id == b.Id;
    }

    private static Card CreateNumber(CardColor color, int number)
    {
        return new Card
        {
            Color = color,
            Type = CardType.Number,
            Number = number
        };
    }

    private static Card CreateAction(CardColor color, CardType type)
    {
        return new Card
        {
            Color = color,
            Type = type,
            Number = -1
        };
    }

    private static Card CreateWild(CardType type)
    {
        return new Card
        {
            Color = CardColor.None,
            Type = type,
            Number = -1
        };
    }

    private static CardColor GetRandomColor()
    {
        CardColor[] colors = { CardColor.Red, CardColor.Green, CardColor.Blue, CardColor.Yellow };
        return colors[Random.Range(0, colors.Length)];
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
