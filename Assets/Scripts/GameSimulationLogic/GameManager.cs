using System.Threading;
using UnityEngine;


public class GameManager : MonoBehaviour
{
    [SerializeField] private float waitingTime = 30f;
    private Timer timer;
    private static GameManager __instance;
    private TurnState currentTurnState;
    private TurnData currentTurnData;
    public static GameManager Instance
    {
        get
        {
            if (__instance == null)
            {
                __instance = FindAnyObjectByType<GameManager>();
                if (__instance == null)
                {
                    GameObject obj = new GameObject("GameManager");
                    __instance = obj.AddComponent<GameManager>();
                }
                DontDestroyOnLoad(__instance.gameObject);
            }
            return __instance;
        }
    }

    private void Awake()
    {
        if (__instance != null && __instance != this)
        {
            Destroy(gameObject);
            return;
        }
        __instance = this;
        timer=new Timer(waitingTime);
        DontDestroyOnLoad(gameObject);
    }

    public void GoNextTurn()
    {
        currentTurnData.PlayerId = (currentTurnData.PlayerId + 1) % 4; // Assuming 4 player
    }

    public Timer GetTimer()
    {
        return timer;
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
}