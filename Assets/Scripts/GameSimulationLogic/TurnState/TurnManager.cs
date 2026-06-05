using UnityEngine;

/// <summary>
/// Quản lý lượt chơi, phase transitions, và game timer.
/// Chạy State Machine qua FixedUpdate: CheckForbidden → DrawPenalty → Playing → UnoCheck.
/// </summary>
public class TurnManager : MonoBehaviour
{
    [Header("Timing")]
    [SerializeField] private float playingPhaseTime = 30f;
    [SerializeField] private float unoCheckTime = 5f;
    [SerializeField] private float drawPenaltyTime = 10f;

    private Timer timer;
    private TurnState currentTurnState;
    private TurnData currentTurnData;
    private int currentDrawAmount = 0;
    private CardColor currentCardColor = CardColor.Red;
    private int direction = 1;
    private bool waitingForWildColor = false;

    public void Awake()
    {
        timer = new Timer(playingPhaseTime);
    }

    /// <summary>
    /// Reset cho round mới — gọi từ GameManager.StartRound().
    /// </summary>
    public void ResetForNewRound()
    {
        direction = 1;
        currentDrawAmount = 0;
        currentCardColor = CardColor.Red;
        waitingForWildColor = false;

        currentTurnData = new TurnData
        {
            PlayerId = 0,
            IsForbidden = false,
            prevCard = default
        };

        // Bắt đầu từ CheckForbidden phase
        SetTurnState(new CheckForbidden());
    }

    public void GoNextTurn()
    {
        currentTurnData.PlayerId = Mod(
            currentTurnData.PlayerId + direction,
            GameManager.Instance.GetPlayerCount());
        currentTurnData.IsForbidden = false;

        // Reset UNO call cho turn mới
        Player nextPlayer = GameManager.Instance.GetPlayer(currentTurnData.PlayerId);
        if (nextPlayer != null)
        {
            nextPlayer.HasCalledUno = false;
        }

        // Broadcast turn change
        GameManager.Instance.BroadcastTurnState();

        // Bắt đầu phase mới
        SetTurnState(new CheckForbidden());
    }

    /// <summary>
    /// Chuyển sang TurnState mới — gọi SetUpTurnState().
    /// </summary>
    public void SetTurnState(TurnState newState)
    {
        currentTurnState = newState;
        if (currentTurnState != null)
        {
            currentTurnState.SetUpTurnState();
        }
    }

    // ─── Direction ──────────────────────────────────────────────────────

    public void ReverseDirection()
    {
        direction *= -1;
    }

    public int GetDirection() => direction;

    public int PeekNextPlayer()
    {
        return Mod(currentTurnData.PlayerId + direction,
            GameManager.Instance.GetPlayerCount());
    }

    // ─── Draw Stacking ──────────────────────────────────────────────────

    public int GetCurrentDrawAmount() => currentDrawAmount;

    public void SetCurrentDrawAmount(int amount)
    {
        currentDrawAmount = amount;
    }

    public void ResetDrawAmount()
    {
        currentDrawAmount = 0;
        GameEvents.RaiseDrawStackChanged(0);
    }

    // ─── Card Color ─────────────────────────────────────────────────────

    public CardColor GetCurrentCardColor() => currentCardColor;

    public void SetCurrentCardColor(CardColor color)
    {
        currentCardColor = color;
    }

    // ─── Wild Color ─────────────────────────────────────────────────────

    public bool IsWaitingForWildColor() => waitingForWildColor;

    public void SetWaitingForWildColor(bool waiting)
    {
        waitingForWildColor = waiting;
    }

    /// <summary>
    /// Áp dụng màu đã chọn cho Wild card, sau đó tiếp tục flow.
    /// </summary>
    public void ApplyWildColor(CardColor color)
    {
        waitingForWildColor = false;
        currentCardColor = color;
        GameEvents.RaiseColorChanged(color);

        // Tiếp tục turn: advance dựa trên effects đã áp dụng
        GoNextTurn();
    }

    // ─── Turn Data ──────────────────────────────────────────────────────

    public TurnData GetCurrentTurnData() => currentTurnData;

    public void SetCurrentTurnData(TurnData turnData)
    {
        currentTurnData = turnData;
    }

    // ─── Timer ──────────────────────────────────────────────────────────

    public Timer GetTimer() => timer;
    public float GetPlayingPhaseTime() => playingPhaseTime;
    public float GetUnoCheckTime() => unoCheckTime;
    public float GetDrawPenaltyTime() => drawPenaltyTime;

    // ─── Update Loop ────────────────────────────────────────────────────

    private void FixedUpdate()
    {
        if (waitingForWildColor)
            return;

        timer.UpdateTimer(Time.fixedDeltaTime);

        // Broadcast timer cho UI
        if (timer.IsTimerOn())
        {
            GameEvents.RaiseTimerTick(timer.GetTimeRemaining());
        }

        if (currentTurnState != null)
        {
            currentTurnState.TurnUpdate();
        }
    }

    // ─── Utility ────────────────────────────────────────────────────────

    private static int Mod(int value, int mod)
    {
        int result = value % mod;
        return result < 0 ? result + mod : result;
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
    private float timeRemaining;
    private float totalTime;
    private bool isOn;

    public Timer(float totalTime)
    {
        this.totalTime = totalTime;
        timeRemaining = 0;
        isOn = false;
    }

    public void UpdateTimer(float deltaTime)
    {
        if (!isOn) return;

        timeRemaining -= deltaTime;
        if (timeRemaining <= 0)
        {
            isOn = false;
            timeRemaining = 0;
        }
    }

    public bool IsTimerOn() => isOn;
    public float GetTimeRemaining() => timeRemaining;

    public void TurnOn()
    {
        isOn = true;
        timeRemaining = totalTime;
    }

    public void TurnOnWithDuration(float duration)
    {
        isOn = true;
        timeRemaining = duration;
    }

    public bool IsTimeUp() => !isOn && timeRemaining <= 0;

    public void Stop()
    {
        isOn = false;
        timeRemaining = 0;
    }
}