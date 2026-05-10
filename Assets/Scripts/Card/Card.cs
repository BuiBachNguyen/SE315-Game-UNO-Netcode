using UnityEngine;

[CreateAssetMenu(fileName = "New Card", menuName = "Card")]
public class Card : ScriptableObject
{
    [SerializeField] private CardColor color;
    [SerializeField] private CardType type;
    [SerializeField] private int number;

    public CardColor GetColor() => color;
    public CardType GetCardType() => type;
    public int GetNumber() => number;
}
