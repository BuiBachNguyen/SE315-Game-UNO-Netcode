using Unity.Netcode;
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
    private static bool CanSendRpc()
    {
        return NetworkManager.Singleton != null
            && NetworkManager.Singleton.IsListening
            && NetworkGameManager.Instance != null
            && NetworkGameManager.Instance.IsSpawned;
    }

    private void OnEnable()
    {
        GameEvents.OnCardPlayed += HandleCardPlayed;
        GameEvents.OnDrawCardRequested += HandleDrawCardRequested;
        GameEvents.OnDrawnCardDeclined += HandleDrawnCardDeclined;
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
        GameEvents.OnDrawnCardDeclined -= HandleDrawnCardDeclined;
        GameEvents.OnColorSelected -= HandleColorSelected;
        GameEvents.OnUnoCalled -= HandleUnoCalled;
        GameEvents.OnCatchUno -= HandleCatchUno;
        GameEvents.OnNextRoundRequested -= HandleNextRound;
        GameEvents.OnRematchRequested -= HandleRematch;
    }

    // ======== FORWARD TO SERVER ========

    private void HandleCardPlayed(Card card)
    {
        if (!CanSendRpc()) return;

        // Chuyển Card → NetworkCard rồi gửi lên server
        NetworkCard netCard = NetworkCard.FromCard(card);
        NetworkGameManager.Instance.PlayCardServerRpc(netCard);
    }

    private void HandleDrawCardRequested()
    {
        if (!CanSendRpc()) return;
        NetworkGameManager.Instance.DrawCardServerRpc();
    }

    private void HandleDrawnCardDeclined()
    {
        if (!CanSendRpc()) return;
        NetworkGameManager.Instance.DeclineDrawnCardServerRpc();
    }

    private void HandleColorSelected(CardColor color)
    {
        if (!CanSendRpc()) return;
        NetworkGameManager.Instance.SelectColorServerRpc((byte)color);
    }

    private void HandleUnoCalled()
    {
        if (!CanSendRpc()) return;
        NetworkGameManager.Instance.CallUnoServerRpc();
    }

    private void HandleCatchUno(int opponentIndex)
    {
        if (!CanSendRpc()) return;
        NetworkGameManager.Instance.CatchUnoServerRpc(opponentIndex);
    }

    private void HandleNextRound()
    {
        if (!CanSendRpc()) return;
        NetworkGameManager.Instance.NextRoundServerRpc();
    }

    private void HandleRematch()
    {
        if (!CanSendRpc()) return;
        NetworkGameManager.Instance.RematchServerRpc();
    }
}
