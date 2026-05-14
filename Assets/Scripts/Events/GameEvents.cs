using System;
using System.Collections.Generic;

/// <summary>
/// Static event bus cho Game Logic ↔ UI giao tiếp mà không cần direct references.
/// Events chia 2 chiều:
///   - Logic → View: notifications (hand updated, turn changed, etc.)
///   - View → Logic: inputs (card played, draw requested, etc.)
/// </summary>
public static class GameEvents
{
    // ─── Logic → View (Notifications) ───────────────────────────────────

    /// <summary>Bài trên tay local player thay đổi.</summary>
    public static event Action<List<CardGameObject>> OnHandUpdated;

    /// <summary>Lá bài trên đỉnh discard pile thay đổi.</summary>
    public static event Action<CardGameObject> OnDiscardChanged;

    /// <summary>Màu hiện tại thay đổi (sau Wild hoặc lá bài mới).</summary>
    public static event Action<CardColor> OnColorChanged;

    /// <summary>Lượt chuyển sang player khác.</summary>
    public static event Action<int> OnTurnChanged;

    /// <summary>Hướng chơi thay đổi. +1 = thuận, -1 = ngược.</summary>
    public static event Action<int> OnDirectionChanged;

    /// <summary>Wild card được local player chơi — hiện color picker.</summary>
    public static event Action OnWildPlayed;

    /// <summary>Local player còn 1 lá — cần gọi UNO.</summary>
    public static event Action OnUnoCallRequired;

    /// <summary>Số bài đối thủ thay đổi. (playerIndex, cardCount).</summary>
    public static event Action<int, int> OnOpponentHandCountChanged;

    /// <summary>Round kết thúc. (winnerIndex, scoreBreakdownByPlayer).</summary>
    public static event Action<int, Dictionary<int, int>> OnRoundEnd;

    /// <summary>Match kết thúc (đạt target score). (winnerIndex, totalScore).</summary>
    public static event Action<int, int> OnMatchEnd;

    /// <summary>Số lá trong draw pile thay đổi.</summary>
    public static event Action<int> OnDeckCountChanged;

    /// <summary>Local player rút được bài. (card, isPlayable).</summary>
    public static event Action<CardGameObject, bool> OnCardDrawn;

    /// <summary>Draw stack amount thay đổi — UI hiển thị tổng phải rút.</summary>
    public static event Action<int> OnDrawStackChanged;

    /// <summary>Timer tick — UI hiển thị thời gian còn lại.</summary>
    public static event Action<float> OnTimerTick;

    // ─── View → Logic (Inputs) ──────────────────────────────────────────

    /// <summary>Local player đánh 1 lá bài.</summary>
    public static event Action<CardGameObject> OnCardPlayed;

    /// <summary>Local player yêu cầu rút bài từ deck.</summary>
    public static event Action OnDrawCardRequested;

    /// <summary>Local player chọn màu cho Wild card.</summary>
    public static event Action<CardColor> OnColorSelected;

    /// <summary>Local player gọi UNO.</summary>
    public static event Action OnUnoCalled;

    /// <summary>Local player bắt đối thủ quên gọi UNO. (opponentIndex).</summary>
    public static event Action<int> OnCatchUno;

    /// <summary>UI yêu cầu bắt đầu round mới.</summary>
    public static event Action OnNextRoundRequested;

    /// <summary>UI yêu cầu rematch sau khi match kết thúc.</summary>
    public static event Action OnRematchRequested;

    // ─── Raise Methods ──────────────────────────────────────────────────

    public static void RaiseHandUpdated(List<CardGameObject> cards)
    {
        OnHandUpdated?.Invoke(cards);
    }

    public static void RaiseDiscardChanged(CardGameObject topCard)
    {
        OnDiscardChanged?.Invoke(topCard);
    }

    public static void RaiseColorChanged(CardColor color)
    {
        OnColorChanged?.Invoke(color);
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

    public static void RaiseDeckCountChanged(int count)
    {
        OnDeckCountChanged?.Invoke(count);
    }

    public static void RaiseCardDrawn(CardGameObject card, bool isPlayable)
    {
        OnCardDrawn?.Invoke(card, isPlayable);
    }

    public static void RaiseDrawStackChanged(int amount)
    {
        OnDrawStackChanged?.Invoke(amount);
    }

    public static void RaiseTimerTick(float timeRemaining)
    {
        OnTimerTick?.Invoke(timeRemaining);
    }

    public static void RaiseCardPlayed(CardGameObject card)
    {
        OnCardPlayed?.Invoke(card);
    }

    public static void RaiseDrawCardRequested()
    {
        OnDrawCardRequested?.Invoke();
    }

    public static void RaiseColorSelected(CardColor color)
    {
        OnColorSelected?.Invoke(color);
    }

    public static void RaiseUnoCalled()
    {
        OnUnoCalled?.Invoke();
    }

    public static void RaiseCatchUno(int opponentIndex)
    {
        OnCatchUno?.Invoke(opponentIndex);
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
