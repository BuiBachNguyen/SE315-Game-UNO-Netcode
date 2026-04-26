using System.Threading;
using UnityEngine;


public class TurnManager : MonoBehaviour
{
    [SerializeField] private float waitingTime = 30f;
    private Timer timer;
    private TurnState currentTurnState;
    private TurnData currentTurnData;
    private int CurrentDrawAmount=0;

    public void Awake()
    {
        timer = new Timer(waitingTime);
        currentTurnState = new CheckForbidden();
        currentTurnState.SetUpTurnState();
    }

    public void GoNextTurn()
    {
        currentTurnData.PlayerId = (currentTurnData.PlayerId + 1) % 4; // Assuming 4 player
        currentTurnState = new CheckForbidden();
        currentTurnState.SetUpTurnState();
    }

    public Timer GetTimer()
    {
        return timer;
    }
    

    public TurnData GetCurrentTurnData()
    {
        return currentTurnData;
    }

    public void SetCurrentTurnData(TurnData turnData)
    {
        currentTurnData = turnData;
    }

    public void SetCurrentDrawAmount(int amount)
    {
        CurrentDrawAmount = amount;
    }

    public int GetCurrentDrawAmount()
    {
        return CurrentDrawAmount;
    }

    void Start()
    {
       
    }


    void FixedUpdate()
    {
        timer.UpdateTimer(Time.fixedDeltaTime);
        currentTurnState.TurnUpdate();
    }
}

public struct TurnData
{
    public int PlayerId;
    public CardData prevCard;
    public bool IsForbidden;
}

public abstract class TurnState
{
    public abstract void SetUpTurnState();
    public abstract void ProcessNextPhase();
    public abstract void TurnUpdate();
}

public class Timer
{
    float timeRemaining;
    float totalTime;
    bool On;
    public Timer(float totalTime)
    {
        this.totalTime = totalTime;
        timeRemaining = 0;
        On = false;
    }

    public void UpdateTimer(float deltaTime)
    {
        timeRemaining -= deltaTime;
        if (timeRemaining <= 0)
        {
            On = false;
            timeRemaining = 0;
        }
    }   

    public bool IsTimerOn()
    {
        return On;
    }

    public void TurnOn()
    {
        On = true;
        timeRemaining = totalTime;
    }

    public bool IsTimeUp()
    {
        return timeRemaining <= 0;
    }
}