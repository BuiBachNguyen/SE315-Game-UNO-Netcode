using UnityEngine;

public class PlayingPhase : TurnState
{
    public override void ProcessNextPhase()
    {
        
    }

    public override void SetUpTurnState()
    {
        
    }

    public override void TurnUpdate()
    {
        GameManager.Instance.GetTurnManager().GetTimer().TurnOn();
        if(GameManager.Instance.GetTurnManager().GetTimer().IsTimeUp())
        {
            //Auto Draw, if current game draw amount =0 draw 1, else draw same amount as current draw amount,set amount to 0, then go next phase
            if (GameManager.Instance.GetTurnManager().GetCurrentDrawAmount() == 0)
            {
                GameManager.Instance.GetDeck().DrawCard(1);
            }
            else
            {
                GameManager.Instance.GetDeck().DrawCard(GameManager.Instance.GetTurnManager().GetCurrentDrawAmount());
            }
            GameManager.Instance.GetTurnManager().SetCurrentDrawAmount(0);
            GameManager.Instance.GetTurnManager().GoNextTurn();
        }
        else
        {
            //wait for player input
            //if player input, check valid and conduct effect update game data accordingly, then go to next phase
            //if player draw same logic as above, then go next turn skip the uno checking
            //if player remaining card is 1, go uno phase 
            //else go next turn
        }

    }
}
