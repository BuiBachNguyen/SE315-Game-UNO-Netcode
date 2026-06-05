using UnityEngine;

/// <summary>
/// Phase xử lý draw stacking.
/// Khi có pending draw amount:
///   - Nếu player có Draw card hợp lệ → chờ input (có thể stack thêm)
///   - Nếu không có / hết timer → player rút currentDrawAmount lá, reset, GoNextTurn
/// </summary>
public class DrawPenaltyPhase : TurnState
{
    private bool canStack;
    private bool subscribedEvents;

    public override void SetUpTurnState()
    {
        TurnManager tm = GameManager.Instance.GetTurnManager();

        // Kiểm tra player hiện tại có Draw card để stack không
        Player currentPlayer = GameManager.Instance.GetCurrentPlayer();
        canStack = HasPlayableDrawCard(currentPlayer, tm);

        if (canStack)
        {
            // Cho player thời gian quyết định
            tm.GetTimer().TurnOnWithDuration(tm.GetDrawPenaltyTime());

            int currentPlayerId = tm.GetCurrentTurnData().PlayerId;
            if (currentPlayerId == GameManager.Instance.GetLocalPlayerIndex())
            {
                // Local player → subscribe event chờ input
                GameEvents.OnCardPlayed += HandleCardPlayed;
                GameEvents.OnDrawCardRequested += HandleAcceptDraw;
                subscribedEvents = true;
            }
            // AI sẽ tự xử lý trong TurnUpdate
        }
        else
        {
            // Không có Draw card → chấp nhận rút ngay
            ApplyDrawPenalty();
        }
    }

    public override void TurnUpdate()
    {
        TurnManager tm = GameManager.Instance.GetTurnManager();

        if (!canStack)
            return;

        int currentPlayerId = tm.GetCurrentTurnData().PlayerId;

        // AI turn → tự động stack nếu có thể
        if (currentPlayerId != GameManager.Instance.GetLocalPlayerIndex())
        {
            Player aiPlayer = GameManager.Instance.GetCurrentPlayer();
            CardGameObject drawCard = FindPlayableDrawCard(aiPlayer, tm);

            if (drawCard != null)
            {
                // AI stack thêm
                ExecuteStackPlay(aiPlayer, drawCard);
            }
            else
            {
                ApplyDrawPenalty();
            }
            return;
        }

        // Timer hết → auto accept draw
        if (tm.GetTimer().IsTimeUp())
        {
            ApplyDrawPenalty();
        }
    }

    public override void ProcessNextPhase()
    {
        // Sau khi stack thêm → GoNextTurn (sẽ vào CheckForbidden → DrawPenalty cho người tiếp)
        Cleanup();
        GameManager.Instance.GetTurnManager().GoNextTurn();
    }

    private void HandleCardPlayed(CardGameObject card)
    {
        if (card == null || !card.IsDrawCard())
            return;

        TurnManager tm = GameManager.Instance.GetTurnManager();
        Player localPlayer = GameManager.Instance.GetPlayer(GameManager.Instance.GetLocalPlayerIndex());

        if (!CardValidator.IsValidPlay(card, GameManager.Instance.GetPlayedZone().GetTopCard(),
            tm.GetCurrentCardColor(), tm.GetCurrentDrawAmount()))
            return;

        ExecuteStackPlay(localPlayer, card);
    }

    private void HandleAcceptDraw()
    {
        ApplyDrawPenalty();
    }

    private void ExecuteStackPlay(Player player, CardGameObject drawCard)
    {
        // Remove card từ tay
        CardGameObject removed = player.FindAndRemoveCard(drawCard);
        if (removed == null) return;

        // Đánh lá bài → PlayedZone
        GameManager.Instance.GetPlayedZone().AddCard(removed);
        GameEvents.RaiseDiscardChanged(removed);

        // Áp dụng effects (sẽ tăng currentDrawAmount)
        CardEffectResolver.ResolveEffects(removed, GameManager.Instance.GetTurnManager());

        // Notify UI
        GameManager.Instance.NotifyHandSizeChanged(player.GetPlayerId());
        GameEvents.RaiseDeckCountChanged(GameManager.Instance.GetDeck().GetRemainingCount());

        // Nếu Wild card → chờ chọn màu (CardEffectResolver đã handle)
        if (GameManager.Instance.GetTurnManager().IsWaitingForWildColor())
        {
            Cleanup();
            return;
        }

        // Chuyển sang người tiếp theo
        ProcessNextPhase();
    }

    /// <summary>
    /// Player chấp nhận rút bài — rút currentDrawAmount lá, reset, GoNextTurn.
    /// </summary>
    private void ApplyDrawPenalty()
    {
        TurnManager tm = GameManager.Instance.GetTurnManager();
        Player currentPlayer = GameManager.Instance.GetCurrentPlayer();

        int drawAmount = tm.GetCurrentDrawAmount();
        if (drawAmount > 0 && currentPlayer != null)
        {
            currentPlayer.DrawCard(drawAmount);
            GameManager.Instance.NotifyHandSizeChanged(currentPlayer.GetPlayerId());
            GameEvents.RaiseDeckCountChanged(GameManager.Instance.GetDeck().GetRemainingCount());
        }

        tm.ResetDrawAmount();
        Cleanup();
        tm.GoNextTurn();
    }

    private bool HasPlayableDrawCard(Player player, TurnManager tm)
    {
        return FindPlayableDrawCard(player, tm) != null;
    }

    private CardGameObject FindPlayableDrawCard(Player player, TurnManager tm)
    {
        if (player == null) return null;

        CardGameObject topCard = GameManager.Instance.GetPlayedZone().GetTopCard();
        CardColor currentColor = tm.GetCurrentCardColor();
        int currentDraw = tm.GetCurrentDrawAmount();

        foreach (var card in player.GetHandCards())
        {
            if (card.IsDrawCard() && CardValidator.IsValidPlay(card, topCard, currentColor, currentDraw))
            {
                return card;
            }
        }
        return null;
    }

    private void Cleanup()
    {
        if (subscribedEvents)
        {
            GameEvents.OnCardPlayed -= HandleCardPlayed;
            GameEvents.OnDrawCardRequested -= HandleAcceptDraw;
            subscribedEvents = false;
        }
    }
}
