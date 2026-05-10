using UnityEngine;

public class PlayingPhase : TurnState
{
    public override void ProcessNextPhase()
    {
        
    }

    public override void SetUpTurnState()
    {
        CardSystemManager.Instance.GetTurnManager().GetTimer().TurnOn();
    }

    public override void TurnUpdate()
    {
        
        if(CardSystemManager.Instance.GetTurnManager().GetTimer().IsTimeUp())
        {
            //Auto Draw, if current game draw amount =0 draw 1, else draw same amount as current draw amount,set amount to 0, then go next phase
            if (CardSystemManager.Instance.GetTurnManager().GetCurrentDrawAmount() == 0)
            {
                CardSystemManager.Instance.GetDeck().DrawCard(1);
            }
            else
            {
                CardSystemManager.Instance.GetDeck().DrawCard(CardSystemManager.Instance.GetTurnManager().GetCurrentDrawAmount());
            }
            CardSystemManager.Instance.GetTurnManager().SetCurrentDrawAmount(0);
            CardSystemManager.Instance.GetTurnManager().GoNextTurn();
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
