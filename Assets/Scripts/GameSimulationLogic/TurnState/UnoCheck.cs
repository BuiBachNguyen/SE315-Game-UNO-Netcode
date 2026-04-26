using UnityEngine;

public class UnoCheck : TurnState
{
    public override void ProcessNextPhase()
    {

    }

    public override void SetUpTurnState()
    {

    }

    public override void TurnUpdate()
    {
        // Turn On timer. If time is up and player hasn't clicked Uno:
        // Check if someone else clicked Uno (Challenge). If yes, current player draws 2 and GoNextTurn, else GoNextTurn directly.
        GameManager.Instance.GetTurnManager().GetTimer().TurnOn();
        if (GameManager.Instance.GetTurnManager().GetTimer().IsTimeUp())
        {
            //DrawCard(2);
            GameManager.Instance.GetTurnManager().SetCurrentDrawAmount(0);
            GameManager.Instance.GetTurnManager().GoNextTurn();
        }
        else
        {
            // Wait for input:
            // If current player clicks Uno in time and it's valid: GoNextTurn (Safe).
            // If someone else clicks Uno first (Challenge): Current player draws 2 and GoNextTurn.
        }
    }
}
