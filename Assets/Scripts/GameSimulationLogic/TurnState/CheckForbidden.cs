using UnityEngine;

public class CheckForbidden : TurnState
{
    public override void ProcessNextPhase()
    {
        
    }

    public override void SetUpTurnState()
    {
        throw new System.NotImplementedException();
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
            //Go to next phase
        }
    }
}
