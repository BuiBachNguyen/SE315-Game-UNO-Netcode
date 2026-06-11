using UnityEngine;

/// <summary>
/// Cầu nối: UI (GameEvents) → Server (NetworkGameManager ServerRpc).
/// Lắng nghe events do UI raise, forward lên server.
/// 
/// KHÔNG cần sửa các View scripts nhờ bridge này:
/// - CardView click → GameEvents.OnCardPlayed → Bridge → PlayCardServerRpc
/// - DrawPileView click → GameEvents.OnDrawCardRequested → Bridge → DrawCardServerRpc
/// - WildColorPicker chọn → GameEvents.OnColorSelected → Bridge → SelectColorServerRpc
/// - UnoButton click → GameEvents.OnUnoCalled → Bridge → CallUnoServerRpc
/// </summary>
public class NetworkEventBridge : MonoBehaviour
{
    private void OnEnable()
    {
        GameEvents.OnCardPlayed += HandleCardPlayed;
        GameEvents.OnDrawCardRequested += HandleDrawCardRequested;
        GameEvents.OnColorSelected += HandleColorSelected;
        GameEvents.OnUnoCalled += HandleUnoCalled;
        GameEvents.OnCatchUno += HandleCatchUno;
        GameEvents.OnNextRoundRequested += HandleNextRound;
        GameEvents.OnRematchRequested += HandleRematch;
    }

    private void OnDisable()
    {
        GameEvents.OnCardPlayed -= HandleCardPlayed;
        GameEvents.OnDrawCardRequested -= HandleDrawCardRequested;
        GameEvents.OnColorSelected -= HandleColorSelected;
        GameEvents.OnUnoCalled -= HandleUnoCalled;
        GameEvents.OnCatchUno -= HandleCatchUno;
        GameEvents.OnNextRoundRequested -= HandleNextRound;
        GameEvents.OnRematchRequested -= HandleRematch;
    }

    // ======== FORWARD TO SERVER ========

    private void HandleCardPlayed(Card card)
    {
        if (NetworkGameManager.Instance == null) return;

        // Chuyển Card → NetworkCard rồi gửi lên server
        NetworkCard netCard = NetworkCard.FromCard(card);
        NetworkGameManager.Instance.PlayCardServerRpc(netCard);
    }

    private void HandleDrawCardRequested()
    {
        if (NetworkGameManager.Instance == null) return;
        NetworkGameManager.Instance.DrawCardServerRpc();
    }

    private void HandleColorSelected(CardColor color)
    {
        if (NetworkGameManager.Instance == null) return;
        NetworkGameManager.Instance.SelectColorServerRpc((byte)color);
    }

    private void HandleUnoCalled()
    {
        if (NetworkGameManager.Instance == null) return;
        NetworkGameManager.Instance.CallUnoServerRpc();
    }

    private void HandleCatchUno(int opponentIndex)
    {
        if (NetworkGameManager.Instance == null) return;
        NetworkGameManager.Instance.CatchUnoServerRpc(opponentIndex);
    }

    private void HandleNextRound()
    {
        if (NetworkGameManager.Instance == null) return;
        NetworkGameManager.Instance.NextRoundServerRpc();
    }

    private void HandleRematch()
    {
        if (NetworkGameManager.Instance == null) return;
        NetworkGameManager.Instance.RematchServerRpc();
    }
}
