using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Card", menuName = "Card")]
public class Card : ScriptableObject
{
    [SerializeField] private CardColor color;
    [SerializeField] private CardType type;
    [SerializeField] private int number;
    [SerializeField] private List<ActionType> actionTypes;
    [SerializeField] private int drawAmount;

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
}
