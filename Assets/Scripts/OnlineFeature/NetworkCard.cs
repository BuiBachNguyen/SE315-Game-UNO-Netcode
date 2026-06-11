using Unity.Netcode;
using System;

/// <summary>
/// Phiên bản serialize được của Card, dùng để gửi qua Netcode.
/// Guid không serialize được → dùng int hashcode thay thế.
/// </summary>
public struct NetworkCard : INetworkSerializable, IEquatable<NetworkCard>
{
    public byte ColorByte;   // (byte)CardColor
    public byte TypeByte;    // (byte)CardType
    public sbyte Number;     // -1 cho non-number cards
    public int Id;           // Guid.GetHashCode() — đủ unique trong 1 game

    // ======== CHUYỂN ĐỔI ========

    /// <summary>
    /// Tạo NetworkCard từ Card local.
    /// </summary>
    public static NetworkCard FromCard(Card card)
    {
        return new NetworkCard
        {
            ColorByte = (byte)card.Color,
            TypeByte = (byte)card.Type,
            Number = (sbyte)card.Number,
            Id = card.Id.GetHashCode()
        };
    }

    /// <summary>
    /// Tạo Card local từ NetworkCard (khi client nhận từ server).
    /// Lưu ý: Card mới sẽ có Guid khác, nhưng Color/Type/Number giống.
    /// </summary>
    public Card ToCard()
    {
        return new Card
        {
            Color = (CardColor)ColorByte,
            Type = (CardType)TypeByte,
            Number = Number
        };
    }

    // ======== NETCODE SERIALIZE ========

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref ColorByte);
        serializer.SerializeValue(ref TypeByte);
        serializer.SerializeValue(ref Number);
        serializer.SerializeValue(ref Id);
    }

    // ======== SO SÁNH ========

    public bool Equals(NetworkCard other)
    {
        return Id == other.Id;
    }

    public override bool Equals(object obj)
    {
        return obj is NetworkCard other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Id;
    }
}
