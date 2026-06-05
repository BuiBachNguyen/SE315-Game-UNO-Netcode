/// <summary>
/// Phase kiểm tra UNO — khi player còn 1 lá bài.
/// Timer 5 giây:
///   - Player gọi UNO đúng lúc → safe → GoNextTurn
///   - Ai đó catch (bấm nút UNO trước) → player rút 2 lá phạt → GoNextTurn
///   - Hết timer mà chưa gọi → rút 2 lá phạt → GoNextTurn
/// </summary>
public class UnoCheck : TurnState
{
    private bool resolved;
    private bool subscribedEvents;
    private int targetPlayerId;

    public override void SetUpTurnState()
    {
        TurnManager tm = GameManager.Instance.GetTurnManager();
        tm.GetTimer().TurnOnWithDuration(tm.GetUnoCheckTime());
        resolved = false;
        targetPlayerId = tm.GetCurrentTurnData().PlayerId;

        // Subscribe events
        GameEvents.OnUnoCalled += HandleUnoCalled;
        GameEvents.OnCatchUno += HandleCatchUno;
        subscribedEvents = true;
    }

    public override void TurnUpdate()
    {
        if (resolved)
            return;

        TurnManager tm = GameManager.Instance.GetTurnManager();

        if (tm.GetTimer().IsTimeUp())
        {
            // Hết giờ mà chưa gọi UNO → phạt
            Player target = GameManager.Instance.GetPlayer(targetPlayerId);
            if (target != null && !target.HasCalledUno)
            {
                ApplyPenalty(target);
            }

            FinishPhase();
        }
    }

    public override void ProcessNextPhase()
    {
        Cleanup();
        GameManager.Instance.GetTurnManager().GoNextTurn();
    }

    private void HandleUnoCalled()
    {
        if (resolved) return;

        Player target = GameManager.Instance.GetPlayer(targetPlayerId);
        if (target != null && target.GetHandCount() == 1)
        {
            target.HasCalledUno = true;
            FinishPhase();
        }
    }

    private void HandleCatchUno(int catcherIndex)
    {
        if (resolved) return;

        Player target = GameManager.Instance.GetPlayer(targetPlayerId);
        if (target == null || target.HasCalledUno)
            return;

        // Bị bắt → phạt rút 2 lá
        ApplyPenalty(target);
        FinishPhase();
    }

    private void ApplyPenalty(Player player)
    {
        player.DrawCard(2);
        GameManager.Instance.NotifyHandSizeChanged(player.GetPlayerId());
        GameEvents.RaiseDeckCountChanged(GameManager.Instance.GetDeck().GetRemainingCount());
    }

    private void FinishPhase()
    {
        resolved = true;
        ProcessNextPhase();
    }

    private void Cleanup()
    {
        if (subscribedEvents)
        {
            GameEvents.OnUnoCalled -= HandleUnoCalled;
            GameEvents.OnCatchUno -= HandleCatchUno;
            subscribedEvents = false;
        }
    }
}
