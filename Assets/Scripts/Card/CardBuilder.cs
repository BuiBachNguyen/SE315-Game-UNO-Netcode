using System.Collections.Generic;
using UnityEngine;

public enum CardColor
{
    Red,
    Green,
    Blue,
    Yellow,
    None
}

public enum CardType
{
    Number,
    Skip,
    Reverse,
    DrawTwo,
    Wild,
    WildDrawFour
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

    public static GameObject GetCard(Card cardData)
    {
        if (cardData != null)
        {
            GameObject cardObject = GetABlankCard(cardData);
            CardGameObject cgo = cardObject.GetComponent<CardGameObject>();
            buildCardColor(cardData.GetColor(), cgo);
            buildCardType(cardData.GetCardType(), cgo);
            buildCardNumber(cardData.GetNumber(), cgo);
            return cardObject;
        }
        return null;
    }
}
