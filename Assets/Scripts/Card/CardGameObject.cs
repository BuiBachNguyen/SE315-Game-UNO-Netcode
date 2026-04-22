using System.Collections.Generic;
using UnityEngine;

public class CardGameObject : MonoBehaviour
{
    [SerializeField] private CardColor color;
    [SerializeField] private CardType type;
    [SerializeField] private int number;
    [SerializeField] private List<ActionType> actionTypes;
    [SerializeField] private int drawAmount;
    public void SetColor(CardColor color)
    {
        this.color = color;
    }

    public void SetType(CardType type)
    {
            this.type = type;
    }

    public void SetNumber(int number)
    {
        this.number = number;
    }

    public void SetActionTypes(List<ActionType> actionTypes)
    {
        this.actionTypes = actionTypes;
    }

    public void SetDrawAmount(int drawAmount)
    {
        this.drawAmount = drawAmount;
    }

    public CardColor GetColor()
    {
        return color;
    }

    public CardType GetCardType()
    {
        return type;
    }

    public int GetNumber()
    {
        return number;
    }

    public List<ActionType> GetActionTypes()
    {
        return actionTypes;
    }

    public int GetDrawAmount()
    {
        return drawAmount;
    }

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
}

public struct CardData
{
    public CardColor color;
    public CardType type;
    public int number;
    public List<ActionType> actionTypes;
    public int drawAmount;
}