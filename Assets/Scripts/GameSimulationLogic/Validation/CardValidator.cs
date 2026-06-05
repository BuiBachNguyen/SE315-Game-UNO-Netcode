/// <summary>
/// Validate xem 1 lá bài có hợp lệ để đánh không.
/// Tách riêng khỏi CardGameObject để giữ SRP — Card chỉ chứa data.
/// </summary>
public static class CardValidator
{
    /// <summary>
    /// Kiểm tra lá bài có hợp lệ để đánh không.
    /// Khi có draw stacking (currentDrawAmount > 0), chỉ Draw cards mới hợp lệ.
    /// </summary>
    public static bool IsValidPlay(CardGameObject card, CardGameObject topCard,
        CardColor currentColor, int currentDrawAmount)
    {
        if (card == null)
            return false;

        // Nếu chưa có lá nào trên bàn → bài nào cũng hợp lệ
        if (topCard == null)
            return true;

        // Draw stacking: khi có pending draw, chỉ Draw cards mới được đánh
        if (currentDrawAmount > 0)
        {
            return IsValidDrawStack(card, topCard, currentColor);
        }

        return IsNormalValidPlay(card, topCard, currentColor);
    }

    /// <summary>
    /// Kiểm tra bình thường (không có draw stacking).
    /// </summary>
    private static bool IsNormalValidPlay(CardGameObject card, CardGameObject topCard, CardColor currentColor)
    {
        // Wild cards luôn hợp lệ
        if (card.GetColor() == CardColor.Wild)
            return true;

        // Cùng màu với currentColor
        if (card.GetColor() == currentColor)
            return true;

        // Cùng type
        if (card.GetCardType() == topCard.GetCardType())
        {
            // Number: phải cùng số
            if (card.GetCardType() == CardType.Number)
                return card.GetNumber() == topCard.GetNumber();

            // Action: phải cùng bộ action types
            if (card.GetCardType() == CardType.Action)
                return HasMatchingActions(card, topCard);
        }

        return false;
    }

    /// <summary>
    /// Kiểm tra draw stacking: bất kỳ Draw card nào miễn là hợp lệ theo luật đánh bài.
    /// VD: +4 chọn Red → người sau có +2 Red → được stack.
    /// </summary>
    private static bool IsValidDrawStack(CardGameObject card, CardGameObject topCard, CardColor currentColor)
    {
        // Phải là Draw card
        if (!card.IsDrawCard())
            return false;

        // Wild Draw cards luôn stack được
        if (card.GetColor() == CardColor.Wild)
            return true;

        // Draw card cùng màu → stack được
        if (card.GetColor() == currentColor)
            return true;

        // Draw card cùng action type set (VD: cả hai đều là DrawTwo)
        if (topCard.IsDrawCard() && HasMatchingActions(card, topCard))
            return true;

        return false;
    }

    /// <summary>
    /// So sánh action type sets giữa 2 lá action card.
    /// </summary>
    private static bool HasMatchingActions(CardGameObject a, CardGameObject b)
    {
        var actionsA = a.GetActionTypes();
        var actionsB = b.GetActionTypes();

        if (actionsA == null || actionsB == null)
            return false;
        if (actionsA.Count != actionsB.Count)
            return false;

        foreach (var action in actionsA)
        {
            if (!actionsB.Contains(action))
                return false;
        }
        return true;
    }
}
