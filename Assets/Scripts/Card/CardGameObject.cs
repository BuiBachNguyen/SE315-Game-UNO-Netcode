using UnityEngine;

public class CardGameObject : MonoBehaviour
{
    [SerializeField] private CardColor color;
    [SerializeField] private CardType type;
    [SerializeField] private int number;

    public void SetColor(CardColor color) { this.color = color; }
    public void SetType(CardType type) { this.type = type; }
    public void SetNumber(int number) { this.number = number; }
    public CardColor GetColor() => color;
    public CardType GetCardType() => type;
    public int GetNumber() => number;

    public CardData GetCardData() => new CardData { color = color, type = type, number = number };

    public bool IsPlayableOn(CardGameObject topCard, CardColor currentColor)
    {
        if (type == CardType.Wild || type == CardType.WildDrawFour)
            return true;
        if (color == currentColor)
            return true;
        if (type == topCard.GetCardType())
        {
            if (type == CardType.Number)
                return number == topCard.GetNumber();
            return true;
        }
        return false;
    }

    public void PlayCard()
    {
        var tm = CardSystemManager.Instance.GetTurnManager();
        tm.SetCurrentCardColor(this.color);
        var turnData = tm.GetCurrentTurnData();
        switch (type)
        {
            case CardType.Skip:
                turnData.IsForbidden = true;
                break;
            case CardType.DrawTwo:
                tm.SetCurrentDrawAmount(tm.GetCurrentDrawAmount() + 2);
                break;
            case CardType.WildDrawFour:
                tm.SetCurrentDrawAmount(tm.GetCurrentDrawAmount() + 4);
                break;
        }
        turnData.prevCard = GetCardData();
        tm.SetCurrentTurnData(turnData);
    }
}

public struct CardData
{
    public CardColor color;
    public CardType type;
    public int number;
}
