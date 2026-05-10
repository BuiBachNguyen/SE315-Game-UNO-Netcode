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

    public bool IsPlayableOn(CardGameObject topCard, CardColor currentColor)
    {
        if (this.type == topCard.GetCardType())
        {
            if (this.type == CardType.Number && this.number == topCard.GetNumber())
                return true;
            if (this.type == CardType.Action)
            {
                if (actionTypes.Count != topCard.GetActionTypes().Count)
                {
                    return false;
                }
                foreach (var action in this.actionTypes)
                {
                    if (!topCard.GetActionTypes().Contains(action))
                        return false;
                }
                return true;
            }
            return false;
        }
        if (this.color == currentColor || currentColor == CardColor.Wild)
            return true;
        return false;
    }

    public void PlayCard()
    {
        GameManager.Instance.GetTurnManager().SetCurrentCardColor(this.color);
        var turndata = GameManager.Instance.GetTurnManager().GetCurrentTurnData();
        if (this.type == CardType.Action)
        {
            foreach (var action in actionTypes)
            {
                switch (action)
                {
                    case ActionType.Skip:
                        turndata.IsForbidden = true;
                        break;
                    case ActionType.Reverse:
                        // Handle reverse logic in TurnManager
                        break;
                    case ActionType.Draw:
                        GameManager.Instance.GetTurnManager().SetCurrentDrawAmount(GameManager.Instance.GetTurnManager().GetCurrentDrawAmount() + this.drawAmount);
                        break;
                    case ActionType.ChangeColor:
                        // Handle color change logic, possibly by prompting the player to choose a color
                        break;
                }
            }
        }
        turndata.prevCard = this.GetCardData();
        GameManager.Instance.GetTurnManager().SetCurrentTurnData(turndata);
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