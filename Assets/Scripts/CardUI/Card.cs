public class UnoCard
{
    public System.Guid Id { get; } = System.Guid.NewGuid();
    public CardColor Color;
    public CardType Type;
    public int Number;

    // Bridge: tạo runtime card từ ScriptableObject Card definition.
    // Dùng khi build deck từ assets thay vì hard-code.
    public static UnoCard FromDefinition(Card definition)
    {
        return new UnoCard
        {
            Color = definition.GetColor(),
            Type  = definition.GetCardType(),
            Number = definition.GetNumber()
        };
    }
}
