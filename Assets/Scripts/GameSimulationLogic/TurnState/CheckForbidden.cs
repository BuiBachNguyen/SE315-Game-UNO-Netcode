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
        if (GameManager.Instance.GetTurnManager().GetCurrentTurnData().IsForbidden)
        {
            //wait for anim to finish
            GameManager.Instance.GetTurnManager().GoNextTurn();
        }
        else
        {
            ProcessNextPhase();
        }
    }
}
