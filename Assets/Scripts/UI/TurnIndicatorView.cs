using UnityEngine;

public class TurnIndicatorView : MonoBehaviour
{
    [System.Serializable]
    public struct PlayerSlot
    {
        public GameObject highlightBorder;
    }

    [SerializeField] private PlayerSlot[] playerSlots;

    private void OnEnable()
    {
        GameEvents.OnTurnChanged += HandleTurnChanged;
    }

    private void OnDisable()
    {
        GameEvents.OnTurnChanged -= HandleTurnChanged;
    }

    private void HandleTurnChanged(int playerIndex)
    {
        if (playerSlots == null) return;

        for (int i = 0; i < playerSlots.Length; i++)
        {
            if (playerSlots[i].highlightBorder != null)
                playerSlots[i].highlightBorder.SetActive(i == playerIndex);
        }
    }
}
