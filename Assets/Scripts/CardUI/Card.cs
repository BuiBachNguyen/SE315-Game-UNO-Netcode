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

public class Card
{
    public System.Guid Id { get; } = System.Guid.NewGuid();
    public CardColor Color;
    public CardType Type;
    public int Number;
}
