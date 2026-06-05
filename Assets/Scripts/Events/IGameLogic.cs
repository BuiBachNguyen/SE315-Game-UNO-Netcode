/// <summary>
/// Interface cho View layer truy vấn trạng thái game.
/// View chỉ cần biết 2 thứ: bài có đánh được không, và có phải lượt mình không.
/// </summary>
public interface IGameLogic
{
    bool IsValidPlay(CardGameObject card);
    bool IsLocalPlayersTurn();
}
