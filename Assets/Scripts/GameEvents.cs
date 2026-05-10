using System;
using System.Collections.Generic;

/// <summary>
/// Static event bus for Game Logic to notify UI without direct references.
/// </summary>
public static class GameEvents
{
    /// <summary>
    /// Fired when the local player's hand contents change.
    /// </summary>
    public static event Action<List<UnoCard>> OnHandUpdated;

    /// <summary>
    /// Fired when the top card of the discard pile changes.
    /// </summary>
    public static event Action<UnoCard> OnDiscardChanged;

    /// <summary>
    /// Fired when the active color changes, typically after a Wild is played.
    /// </summary>
    public static event Action<CardColor> OnColorChanged;

    /// <summary>
    /// Fired when the local player selects a color for a Wild card.
    /// </summary>
    public static event Action<CardColor> OnColorSelected;

    /// <summary>
    /// Fired when the turn moves to a different player.
    /// </summary>
    public static event Action<int> OnTurnChanged;

    /// <summary>
    /// Fired when play direction changes. Use +1 for clockwise and -1 for counter-clockwise.
    /// </summary>
    public static event Action<int> OnDirectionChanged;

    /// <summary>
    /// Fired when a Wild card is played and the UI should show a color picker.
    /// </summary>
    public static event Action OnWildPlayed;

    /// <summary>
    /// Fired when the local player has one card and must call UNO.
    /// </summary>
    public static event Action OnUnoCallRequired;

    /// <summary>
    /// Fired when an opponent's hand size changes.
    /// </summary>
    public static event Action<int, int> OnOpponentHandCountChanged;

    /// <summary>
    /// Fired when a round ends, providing the winner and score breakdown by player index.
    /// </summary>
    public static event Action<int, Dictionary<int, int>> OnRoundEnd;

    /// <summary>
    /// Fired when the match ends (e.g., reaching 500 points), providing winner and total score.
    /// </summary>
    public static event Action<int, int> OnMatchEnd;

    /// <summary>
    /// Fired when the local player calls UNO.
    /// </summary>
    public static event Action OnUnoCalled;

    /// <summary>
    /// Fired when the local player attempts to catch an opponent who did not call UNO.
    /// </summary>
    public static event Action<int> OnCatchUno;

    /// <summary>
    /// Fired when a card is played from the local player's hand.
    /// </summary>
    public static event Action<UnoCard> OnCardPlayed;

    /// <summary>
    /// Fired when the deck count changes (draw pile size).
    /// </summary>
    public static event Action<int> OnDeckCountChanged;

    /// <summary>
    /// Fired when the local player draws a card, with a flag indicating if it is playable.
    /// </summary>
    public static event Action<UnoCard, bool> OnCardDrawn;

    /// <summary>
    /// Fired when the local player requests to draw a card from the deck.
    /// </summary>
    public static event Action OnDrawCardRequested;

    /// <summary>
    /// Fired when the UI requests to start the next round.
    /// </summary>
    public static event Action OnNextRoundRequested;

    /// <summary>
    /// Fired when the UI requests a rematch after match end.
    /// </summary>
    public static event Action OnRematchRequested;

    public static void RaiseHandUpdated(List<UnoCard> cards)
    {
        OnHandUpdated?.Invoke(cards);
    }

    public static void RaiseDiscardChanged(UnoCard topCard)
    {
        OnDiscardChanged?.Invoke(topCard);
    }

    public static void RaiseColorChanged(CardColor color)
    {
        OnColorChanged?.Invoke(color);
    }

    public static void RaiseColorSelected(CardColor color)
    {
        OnColorSelected?.Invoke(color);
    }

    public static void RaiseTurnChanged(int playerIndex)
    {
        OnTurnChanged?.Invoke(playerIndex);
    }

    public static void RaiseDirectionChanged(int direction)
    {
        OnDirectionChanged?.Invoke(direction);
    }

    public static void RaiseWildPlayed()
    {
        OnWildPlayed?.Invoke();
    }

    public static void RaiseUnoCallRequired()
    {
        OnUnoCallRequired?.Invoke();
    }

    public static void RaiseOpponentHandCountChanged(int playerIndex, int cardCount)
    {
        OnOpponentHandCountChanged?.Invoke(playerIndex, cardCount);
    }

    public static void RaiseRoundEnd(int winnerIndex, Dictionary<int, int> scoreBreakdown)
    {
        OnRoundEnd?.Invoke(winnerIndex, scoreBreakdown);
    }

    public static void RaiseMatchEnd(int winnerIndex, int totalScore)
    {
        OnMatchEnd?.Invoke(winnerIndex, totalScore);
    }

    public static void RaiseUnoCalled()
    {
        OnUnoCalled?.Invoke();
    }

    public static void RaiseCatchUno(int opponentIndex)
    {
        OnCatchUno?.Invoke(opponentIndex);
    }

    public static void RaiseCardPlayed(UnoCard card)
    {
        OnCardPlayed?.Invoke(card);
    }

    public static void RaiseDeckCountChanged(int count)
    {
        OnDeckCountChanged?.Invoke(count);
    }

    public static void RaiseCardDrawn(UnoCard card, bool isPlayable)
    {
        OnCardDrawn?.Invoke(card, isPlayable);
    }

    public static void RaiseDrawCardRequested()
    {
        OnDrawCardRequested?.Invoke();
    }

    public static void RaiseNextRoundRequested()
    {
        OnNextRoundRequested?.Invoke();
    }

    public static void RaiseRematchRequested()
    {
        OnRematchRequested?.Invoke();
    }
}
