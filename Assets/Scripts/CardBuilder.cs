using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public enum CardColor
{
    Red,
    Green,
    Blue,
    Yellow,
    Wild
}

public enum CardType
{
    Number,
    Action
}

public enum ActionType
{
    Skip,
    Draw,
    Reverse,
    ChangeColor
}

public static class CardBuilder
{


    public static GameObject GetABlankCard(Card cardData)
    {
        GameObject cardObject = new GameObject(cardData.name);
        cardObject.AddComponent<CardGameObject>();
        return cardObject;
    }
    public static bool buildCardColor(CardColor color, CardGameObject cardGameObject)
    {
        if (cardGameObject != null)
        {
            cardGameObject.SetColor(color);
            return true;
        }
        return false;
    }
    public static bool buildCardType(CardType type, CardGameObject cardGameObject)
    {
        if (cardGameObject != null)
        {
            cardGameObject.SetType(type);
            return true;
        }
        return false;
    }

    public static bool buildCardNumber(int number, CardGameObject cardGameObject)
    {
        if (cardGameObject != null)
        {
            if (cardGameObject.GetCardType() == CardType.Number)
            {
                cardGameObject.SetNumber(number);
                return true;
            }
            cardGameObject.SetNumber(-1);
        }
        return false;
    }

    public static bool buildCardActions(List<ActionType> action, CardGameObject cardGameObject)
    {
        if (cardGameObject != null)
        {
            if (cardGameObject.GetCardType() == CardType.Action)
            {
                cardGameObject.SetActionTypes(action);
                return true;
            }
        }
        return false;
    }

    public static bool buildCardDrawAmount(int amount, CardGameObject cardGameObject)
    {
     
        if (cardGameObject != null)
        {
            if (cardGameObject.GetActionTypes() == null)
            {
                cardGameObject.SetDrawAmount(-1);
                return false;
            }
            if (cardGameObject.GetActionTypes().Contains(ActionType.Draw))
            {
                cardGameObject.SetDrawAmount(amount);
                return true;
            }
            cardGameObject.SetDrawAmount(-1);
        }
        return false;
    }

    public static GameObject GetCard(Card cardData)
    {
        if (cardData != null)
        {
            GameObject cardObject = GetABlankCard(cardData);
            buildCardColor(cardData.GetColor(), cardObject.GetComponent<CardGameObject>());
            buildCardType(cardData.GetCardType(), cardObject.GetComponent<CardGameObject>());
            buildCardNumber(cardData.GetNumber(), cardObject.GetComponent<CardGameObject>());
            buildCardActions(cardData.GetActionTypes(), cardObject.GetComponent<CardGameObject>());
            buildCardDrawAmount(cardData.GetDrawAmount(), cardObject.GetComponent<CardGameObject>());
            return cardObject;
        }
        return null;
    }
}

