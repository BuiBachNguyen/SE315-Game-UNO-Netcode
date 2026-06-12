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
        if (playerSlots == null)
        {
            return;
        }

        int displayIndex = GetDisplayIndex(playerIndex);

        for (int i = 0; i < playerSlots.Length; i++)
        {
            if (playerSlots[i].highlightBorder != null)
            {
                playerSlots[i].highlightBorder.SetActive(i == displayIndex);
            }
        }
    }

    private int GetDisplayIndex(int playerIndex)
    {
        if (PlayerIndexMapper.Instance == null || PlayerIndexMapper.Instance.PlayerCount == 0)
            return playerIndex;

        int localIndex = PlayerIndexMapper.Instance.GetLocalPlayerIndex();
        if (localIndex < 0)
            return playerIndex;

        int count = PlayerIndexMapper.Instance.PlayerCount;
        return (playerIndex - localIndex + count) % count;
    }
}
