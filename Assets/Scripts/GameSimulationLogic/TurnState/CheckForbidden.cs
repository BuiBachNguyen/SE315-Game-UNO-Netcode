/// <summary>
/// Phase đầu tiên mỗi turn — kiểm tra player có bị Skip không.
/// Nếu IsForbidden → skip player hiện tại, GoNextTurn.
/// Nếu không → chuyển sang DrawPenaltyPhase (nếu có pending draw) hoặc PlayingPhase.
/// </summary>
public class CheckForbidden : TurnState
{
    public override void SetUpTurnState()
    {
        // Không cần timer — xử lý ngay lập tức
    }

    public override void TurnUpdate()
    {
        TurnManager tm = GameManager.Instance.GetTurnManager();
        TurnData data = tm.GetCurrentTurnData();

        if (data.IsForbidden)
        {
            // Player bị Skip — reset flag và chuyển lượt
            data.IsForbidden = false;
            tm.SetCurrentTurnData(data);
            tm.GoNextTurn();
        }
        else
        {
            ProcessNextPhase();
        }
    }

    public override void ProcessNextPhase()
    {
        TurnManager tm = GameManager.Instance.GetTurnManager();

        if (tm.GetCurrentDrawAmount() > 0)
        {
            // Có pending draw stack → vào DrawPenaltyPhase
            tm.SetTurnState(new DrawPenaltyPhase());
        }
        else
        {
            // Bình thường → vào PlayingPhase
            tm.SetTurnState(new PlayingPhase());
        }
    }
}
