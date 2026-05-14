using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ScriptableObject map: CardGameObject data → Sprite.
/// Adapt cho hệ thống CardType.Number/Action + ActionType data-driven.
/// </summary>
[CreateAssetMenu(menuName = "UNO/Card Sprite Map", fileName = "CardSpriteMap")]
public class CardSpriteMap : ScriptableObject
{
    [System.Serializable]
    public struct ColorSpriteSet
    {
        [Tooltip("Chon mau tuong ung cho bo sprite nay.")]
        public CardColor color;

        [Tooltip("Sprites so 0-9 theo thu tu chi so 0..9.")]
        public Sprite[] numberSprites;

        public Sprite skip;
        public Sprite reverse;
        public Sprite drawTwo;
    }

    [Header("Per-color sprites")]
    [Tooltip("Moi mau can 1 ColorSpriteSet, numberSprites phai du 10 sprite (0..9).")]
    public ColorSpriteSet[] colorSets;

    [Header("Wild sprites")]
    public Sprite wild;
    public Sprite wildDrawFour;

    [Header("Back sprite")]
    public Sprite cardBack;

    /// <summary>
    /// Lấy sprite cho 1 lá bài dựa trên data của nó.
    /// Adapt cho hệ thống ActionType data-driven.
    /// </summary>
    public Sprite GetSprite(CardGameObject card)
    {
        if (card == null)
            return null;

        return GetSprite(card.GetColor(), card.GetCardType(), card.GetNumber(), card.GetActionTypes());
    }

    public Sprite GetSprite(CardColor color, CardType type, int number, List<ActionType> actions)
    {
        // Wild cards
        if (color == CardColor.Wild)
        {
            if (actions != null && actions.Contains(ActionType.Draw))
                return wildDrawFour;
            return wild;
        }

        ColorSpriteSet? set = FindColorSet(color);
        if (set == null)
            return null;

        ColorSpriteSet value = set.Value;

        // Number cards
        if (type == CardType.Number)
        {
            if (value.numberSprites == null || number < 0 || number >= value.numberSprites.Length)
                return null;
            return value.numberSprites[number];
        }

        // Action cards — map ActionType to sprites
        if (type == CardType.Action && actions != null)
        {
            if (actions.Contains(ActionType.Skip))
                return value.skip;
            if (actions.Contains(ActionType.Reverse))
                return value.reverse;
            if (actions.Contains(ActionType.Draw))
                return value.drawTwo;
        }

        return null;
    }

    public Sprite GetCardBack()
    {
        return cardBack;
    }

    private ColorSpriteSet? FindColorSet(CardColor color)
    {
        if (colorSets == null)
            return null;

        for (int i = 0; i < colorSets.Length; i++)
        {
            if (colorSets[i].color == color)
                return colorSets[i];
        }
        return null;
    }
}
