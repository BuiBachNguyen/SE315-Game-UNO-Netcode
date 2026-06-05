using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Xử lý hiệu ứng khi 1 lá bài được đánh.
/// Tách từ CardGameObject.PlayCard() để giữ SRP — Card không biết về GameManager.
/// </summary>
public static class CardEffectResolver
{
    /// <summary>
    /// Áp dụng tất cả hiệu ứng của lá bài lên TurnManager.
    /// </summary>
    public static void ResolveEffects(CardGameObject card, TurnManager turnManager)
    {
        if (card == null || turnManager == null)
            return;

        // Cập nhật màu hiện tại
        if (card.GetColor() != CardColor.Wild)
        {
            turnManager.SetCurrentCardColor(card.GetColor());
            GameEvents.RaiseColorChanged(card.GetColor());
        }

        // Nếu là Number card → không có hiệu ứng đặc biệt
        if (card.GetCardType() != CardType.Action)
            return;

        List<ActionType> actions = card.GetActionTypes();
        if (actions == null)
            return;

        TurnData turnData = turnManager.GetCurrentTurnData();
        bool hasColorChange = false;

        foreach (ActionType action in actions)
        {
            switch (action)
            {
                case ActionType.Skip:
                    turnData.IsForbidden = true;
                    break;

                case ActionType.Reverse:
                    turnManager.ReverseDirection();
                    GameEvents.RaiseDirectionChanged(turnManager.GetDirection());
                    break;

                case ActionType.Draw:
                    int newAmount = turnManager.GetCurrentDrawAmount() + card.GetDrawAmount();
                    turnManager.SetCurrentDrawAmount(newAmount);
                    GameEvents.RaiseDrawStackChanged(newAmount);
                    break;

                case ActionType.ChangeColor:
                    hasColorChange = true;
                    break;
            }
        }

        // Cập nhật prevCard
        turnData.prevCard = card.GetCardData();
        turnManager.SetCurrentTurnData(turnData);

        // Nếu Wild card (ChangeColor) → cần chờ player chọn màu
        if (hasColorChange)
        {
            turnManager.SetWaitingForWildColor(true);

            int currentPlayerId = turnManager.GetCurrentTurnData().PlayerId;
            if (currentPlayerId == GameManager.Instance.GetLocalPlayerIndex())
            {
                // Local player → hiện color picker UI
                GameEvents.RaiseWildPlayed();
            }
            else
            {
                // AI → tự chọn màu tốt nhất
                CardColor bestColor = PickBestColorForPlayer(
                    GameManager.Instance.GetPlayer(currentPlayerId));
                turnManager.ApplyWildColor(bestColor);
            }
        }
    }

    /// <summary>
    /// AI chọn màu nhiều nhất trên tay.
    /// </summary>
    private static CardColor PickBestColorForPlayer(Player player)
    {
        if (player == null)
            return GetRandomColor();

        int red = 0, green = 0, blue = 0, yellow = 0;

        foreach (var card in player.GetHandCards())
        {
            switch (card.GetColor())
            {
                case CardColor.Red: red++; break;
                case CardColor.Green: green++; break;
                case CardColor.Blue: blue++; break;
                case CardColor.Yellow: yellow++; break;
            }
        }

        CardColor best = CardColor.Red;
        int bestValue = red;

        if (green > bestValue) { best = CardColor.Green; bestValue = green; }
        if (blue > bestValue) { best = CardColor.Blue; bestValue = blue; }
        if (yellow > bestValue) { best = CardColor.Yellow; }

        return best;
    }

    private static CardColor GetRandomColor()
    {
        CardColor[] colors = { CardColor.Red, CardColor.Green, CardColor.Blue, CardColor.Yellow };
        return colors[Random.Range(0, colors.Length)];
    }
}
