public class UnoCard
{
    public System.Guid Id { get; } = System.Guid.NewGuid();
    public CardColor Color;
    public CardType Type;
    public int Number;
}
