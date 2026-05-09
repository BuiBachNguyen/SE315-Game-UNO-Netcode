using UnityEngine;

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

    public Sprite GetSprite(CardColor color, CardType type, int number)
    {
        if (type == CardType.Wild)
        {
            return wild;
        }

        if (type == CardType.WildDrawFour)
        {
            return wildDrawFour;
        }

        ColorSpriteSet? set = FindColorSet(color);
        if (set == null)
        {
            return null;
        }

        ColorSpriteSet value = set.Value;
        switch (type)
        {
            case CardType.Skip:
                return value.skip;
            case CardType.Reverse:
                return value.reverse;
            case CardType.DrawTwo:
                return value.drawTwo;
            case CardType.Number:
            default:
                if (value.numberSprites == null || number < 0 || number >= value.numberSprites.Length)
                {
                    return null;
                }
                return value.numberSprites[number];
        }
    }

    public Sprite GetCardBack()
    {
        return cardBack;
    }

    private ColorSpriteSet? FindColorSet(CardColor color)
    {
        if (colorSets == null)
        {
            return null;
        }

        for (int i = 0; i < colorSets.Length; i++)
        {
            if (colorSets[i].color == color)
            {
                return colorSets[i];
            }
        }

        return null;
    }
}
