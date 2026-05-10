using UnityEngine;

public class CheckForbidden : TurnState
{
    public override void ProcessNextPhase()
    {
        
    }

    public override void SetUpTurnState()
    {
    }

    public override void TurnUpdate()
    {
        if (CardSystemManager.Instance.GetTurnManager().GetCurrentTurnData().IsForbidden)
        {
            //wait for anim to finish
            CardSystemManager.Instance.GetTurnManager().GoNextTurn();
        }
        else
        {
            ProcessNextPhase();
        }
    }
}
