using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Phase chính — player đánh bài hoặc rút bài.
/// Local player: chờ UI input (GameEvents).
/// AI: tự tìm lá hợp lệ và đánh.
/// Timer 30s → auto-draw nếu hết thời gian.
/// </summary>
public class PlayingPhase : TurnState
{
    private bool subscribedEvents;
    private bool turnProcessed;

    /// <summary>
    /// Thời gian AI "suy nghĩ" trước khi đánh bài (giây).
    /// Tránh turn nháy nhanh liên tục khi nhiều AI chơi liên tiếp.
    /// </summary>
    private const float AI_THINK_DELAY = 1.0f;
    private float aiThinkTimer;
    private bool isAiThinking;

    public override void SetUpTurnState()
    {
        TurnManager tm = GameManager.Instance.GetTurnManager();
        tm.GetTimer().TurnOnWithDuration(tm.GetPlayingPhaseTime());
        turnProcessed = false;
        isAiThinking = false;
        aiThinkTimer = 0f;

        int currentPlayerId = tm.GetCurrentTurnData().PlayerId;

        if (currentPlayerId == GameManager.Instance.GetLocalPlayerIndex())
        {
            // Local player → subscribe events chờ input
            GameEvents.OnCardPlayed += HandleCardPlayed;
            GameEvents.OnDrawCardRequested += HandleDrawRequested;
            subscribedEvents = true;
        }
        else
        {
            // AI turn → bắt đầu "suy nghĩ"
            isAiThinking = true;
            aiThinkTimer = AI_THINK_DELAY;
        }
    }

    public override void TurnUpdate()
    {
        if (turnProcessed)
            return;

        TurnManager tm = GameManager.Instance.GetTurnManager();
        int currentPlayerId = tm.GetCurrentTurnData().PlayerId;

        // AI turn — chờ delay xong mới đánh
        if (currentPlayerId != GameManager.Instance.GetLocalPlayerIndex())
        {
            if (isAiThinking)
            {
                aiThinkTimer -= Time.fixedDeltaTime;
                if (aiThinkTimer > 0f)
                    return; // Vẫn đang "suy nghĩ"
                isAiThinking = false;
            }
            HandleAiTurn();
            return;
        }

        // Local player — timer hết → auto draw
        if (tm.GetTimer().IsTimeUp())
        {
            AutoDraw();
        }
    }

    public override void ProcessNextPhase()
    {
        // Không dùng — transitions xảy ra trực tiếp
    }

    // ─── Local Player Input ─────────────────────────────────────────────

    private void HandleCardPlayed(CardGameObject card)
    {
        if (turnProcessed || card == null)
            return;

        TurnManager tm = GameManager.Instance.GetTurnManager();
        Player localPlayer = GameManager.Instance.GetPlayer(GameManager.Instance.GetLocalPlayerIndex());
        PlayedZone playedZone = GameManager.Instance.GetPlayedZone();

        // Validate
        if (!CardValidator.IsValidPlay(card, playedZone.GetTopCard(),
            tm.GetCurrentCardColor(), tm.GetCurrentDrawAmount()))
            return;

        // Remove card từ tay
        CardGameObject removed = localPlayer.FindAndRemoveCard(card);
        if (removed == null)
        {
            // Card không có trên tay — refresh UI
            GameManager.Instance.BroadcastLocalHand();
            return;
        }

        ProcessPlayedCard(localPlayer, removed);
    }

    private void HandleDrawRequested()
    {
        if (turnProcessed)
            return;

        TurnManager tm = GameManager.Instance.GetTurnManager();
        if (tm.IsWaitingForWildColor())
            return;

        Player localPlayer = GameManager.Instance.GetPlayer(GameManager.Instance.GetLocalPlayerIndex());
        DrawAndCheck(localPlayer);
    }

    // ─── AI Turn ────────────────────────────────────────────────────────

    private void HandleAiTurn()
    {
        TurnManager tm = GameManager.Instance.GetTurnManager();
        Player aiPlayer = GameManager.Instance.GetCurrentPlayer();
        PlayedZone playedZone = GameManager.Instance.GetPlayedZone();

        // Tìm lá hợp lệ đầu tiên (greedy AI)
        CardGameObject playable = FindFirstPlayable(aiPlayer, playedZone.GetTopCard(),
            tm.GetCurrentCardColor(), tm.GetCurrentDrawAmount());

        if (playable != null)
        {
            CardGameObject removed = aiPlayer.FindAndRemoveCard(playable);
            if (removed != null)
            {
                ProcessPlayedCard(aiPlayer, removed);
                return;
            }
        }

        // Không có lá hợp lệ → rút bài
        DrawAndCheck(aiPlayer);
    }

    // ─── Shared Logic ───────────────────────────────────────────────────

    private void ProcessPlayedCard(Player player, CardGameObject card)
    {
        turnProcessed = true;
        Cleanup();

        PlayedZone playedZone = GameManager.Instance.GetPlayedZone();
        TurnManager tm = GameManager.Instance.GetTurnManager();

        // Đặt lá bài vào PlayedZone
        playedZone.AddCard(card);
        GameEvents.RaiseDiscardChanged(card);

        // Áp dụng effects
        CardEffectResolver.ResolveEffects(card, tm);

        // Notify UI
        GameManager.Instance.NotifyHandSizeChanged(player.GetPlayerId());
        GameEvents.RaiseDeckCountChanged(GameManager.Instance.GetDeck().GetRemainingCount());

        // Kiểm tra thắng round
        if (player.GetHandCount() == 0)
        {
            GameManager.Instance.EndRound(player.GetPlayerId());
            return;
        }

        // Kiểm tra UNO (còn 1 lá)
        if (player.GetHandCount() == 1)
        {
            // AI tự động gọi UNO
            if (player.GetPlayerId() != GameManager.Instance.GetLocalPlayerIndex())
            {
                player.HasCalledUno = true;
                tm.GoNextTurn();
            }
            else
            {
                // Local player → vào UnoCheck phase
                GameEvents.RaiseUnoCallRequired();
                tm.SetTurnState(new UnoCheck());
            }
            return;
        }

        // Nếu đang chờ Wild color → CardEffectResolver đã handle
        if (tm.IsWaitingForWildColor())
            return;

        // Tiếp tục → GoNextTurn
        tm.GoNextTurn();
    }

    private void DrawAndCheck(Player player)
    {
        turnProcessed = true;
        Cleanup();

        List<CardGameObject> drawn = player.DrawCard(1);
        TurnManager tm = GameManager.Instance.GetTurnManager();
        PlayedZone playedZone = GameManager.Instance.GetPlayedZone();

        GameManager.Instance.NotifyHandSizeChanged(player.GetPlayerId());
        GameEvents.RaiseDeckCountChanged(GameManager.Instance.GetDeck().GetRemainingCount());

        if (drawn.Count > 0)
        {
            CardGameObject drawnCard = drawn[0];
            bool playable = CardValidator.IsValidPlay(drawnCard, playedZone.GetTopCard(),
                tm.GetCurrentCardColor(), tm.GetCurrentDrawAmount());

            // Nếu là local player → thông báo UI
            if (player.GetPlayerId() == GameManager.Instance.GetLocalPlayerIndex())
            {
                GameEvents.RaiseCardDrawn(drawnCard, playable);
            }

            // Nếu rút được lá chơi được — AI tự chơi, local player chờ popup
            if (playable && player.GetPlayerId() != GameManager.Instance.GetLocalPlayerIndex())
            {
                // AI chơi ngay lá vừa rút
                CardGameObject removed = player.FindAndRemoveCard(drawnCard);
                if (removed != null)
                {
                    turnProcessed = false;
                    ProcessPlayedCard(player, removed);
                    return;
                }
            }
        }

        // Không chơi được → chuyển lượt
        tm.GoNextTurn();
    }

    private void AutoDraw()
    {
        Player localPlayer = GameManager.Instance.GetPlayer(GameManager.Instance.GetLocalPlayerIndex());
        DrawAndCheck(localPlayer);
    }

    // ─── Helpers ────────────────────────────────────────────────────────

    private CardGameObject FindFirstPlayable(Player player, CardGameObject topCard,
        CardColor currentColor, int currentDrawAmount)
    {
        if (player == null) return null;

        foreach (var card in player.GetHandCards())
        {
            if (CardValidator.IsValidPlay(card, topCard, currentColor, currentDrawAmount))
                return card;
        }
        return null;
    }

    private void Cleanup()
    {
        if (subscribedEvents)
        {
            GameEvents.OnCardPlayed -= HandleCardPlayed;
            GameEvents.OnDrawCardRequested -= HandleDrawRequested;
            subscribedEvents = false;
        }
    }
}
