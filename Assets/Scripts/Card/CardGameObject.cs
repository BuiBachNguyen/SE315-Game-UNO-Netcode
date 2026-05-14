using System;
using System.Collections.Generic;
using UnityEngine;

public class CardGameObject : MonoBehaviour
{
    [SerializeField] private CardColor color;
    [SerializeField] private CardType type;
    [SerializeField] private int number;
    [SerializeField] private List<ActionType> actionTypes;
    [SerializeField] private int drawAmount;

    /// <summary>
    /// Identity duy nhất cho mỗi lá bài runtime — phân biệt 2 lá cùng loại.
    /// </summary>
    public Guid Id { get; } = Guid.NewGuid();

    public void SetColor(CardColor color) { this.color = color; }
    public void SetType(CardType type) { this.type = type; }
    public void SetNumber(int number) { this.number = number; }
    public void SetActionTypes(List<ActionType> actionTypes) { this.actionTypes = actionTypes; }
    public void SetDrawAmount(int drawAmount) { this.drawAmount = drawAmount; }

    public CardColor GetColor() => color;
    public CardType GetCardType() => type;
    public int GetNumber() => number;
    public List<ActionType> GetActionTypes() => actionTypes;
    public int GetDrawAmount() => drawAmount;

    public CardData GetCardData()
    {
        return new CardData
        {
            color = this.color,
            type = this.type,
            number = this.number,
            actionTypes = this.actionTypes,
            drawAmount = this.drawAmount
        };
    }

    /// <summary>
    /// Kiểm tra lá bài này có thể đánh lên topCard với currentColor hiện tại không.
    /// </summary>
    public bool IsPlayableOn(CardGameObject topCard, CardColor currentColor)
    {
        // Wild cards luôn hợp lệ
        if (this.color == CardColor.Wild)
            return true;

        // Cùng màu với currentColor (bao gồm cả trường hợp Wild đã chọn màu)
        if (this.color == currentColor)
            return true;

        // Cùng type
        if (this.type == topCard.GetCardType())
        {
            // Number cards: phải cùng số
            if (this.type == CardType.Number)
                return this.number == topCard.GetNumber();

            // Action cards: phải cùng action types
            if (this.type == CardType.Action)
            {
                if (actionTypes == null || topCard.GetActionTypes() == null)
                    return false;
                if (actionTypes.Count != topCard.GetActionTypes().Count)
                    return false;
                foreach (var action in this.actionTypes)
                {
                    if (!topCard.GetActionTypes().Contains(action))
                        return false;
                }
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Kiểm tra lá bài này có phải là Draw card không (có ActionType.Draw).
    /// Dùng cho draw stacking logic.
    /// </summary>
    public bool IsDrawCard()
    {
        return type == CardType.Action
            && actionTypes != null
            && actionTypes.Contains(ActionType.Draw);
    }

    /// <summary>
    /// Tính điểm lá bài theo luật UNO:
    /// Number: face value (0-9), Action: 20, Wild: 50.
    /// </summary>
    public int GetScoreValue()
    {
        if (type == CardType.Number)
            return Mathf.Max(0, number);

        if (color == CardColor.Wild)
            return 50;

        // Action cards (Skip, Reverse, Draw2)
        return 20;
    }
}

public struct CardData
{
    public CardColor color;
    public CardType type;
    public int number;
    public List<ActionType> actionTypes;
    public int drawAmount;
}