using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UnoButtonView : MonoBehaviour
{
    [SerializeField] private int localPlayerIndex = 0;
    [SerializeField] private Button unoButton;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float inactiveAlpha = 0.4f;

    private int localHandCount;
    private bool hasCalledUno;
    private readonly Dictionary<int, int> opponentHandCounts = new Dictionary<int, int>();

    private void Awake()
    {
        if (unoButton != null)
            unoButton.onClick.AddListener(HandleButtonClicked);
    }

    private void OnEnable()
    {
        GameEvents.OnHandUpdated += HandleHandUpdated;
        GameEvents.OnOpponentHandCountChanged += HandleOpponentHandCountChanged;
        GameEvents.OnTurnChanged += HandleTurnChanged;
    }

    private void OnDisable()
    {
        GameEvents.OnHandUpdated -= HandleHandUpdated;
        GameEvents.OnOpponentHandCountChanged -= HandleOpponentHandCountChanged;
        GameEvents.OnTurnChanged -= HandleTurnChanged;
    }

    private void HandleHandUpdated(List<CardGameObject> cards)
    {
        localHandCount = cards != null ? cards.Count : 0;
        RefreshVisual();
    }

    private void HandleOpponentHandCountChanged(int playerIndex, int cardCount)
    {
        opponentHandCounts[playerIndex] = cardCount;
        RefreshVisual();
    }

    private void HandleTurnChanged(int playerIndex)
    {
        if (playerIndex == localPlayerIndex)
            hasCalledUno = false;
        RefreshVisual();
    }

    private void HandleButtonClicked()
    {
        if (CanCallUno())
        {
            hasCalledUno = true;
            GameEvents.RaiseUnoCalled();
            RefreshVisual();
            return;
        }

        int catchTarget = FindCatchTarget();
        if (catchTarget >= 0)
            GameEvents.RaiseCatchUno(catchTarget);
    }

    private bool CanCallUno() => localHandCount == 1 && !hasCalledUno;

    private int FindCatchTarget()
    {
        foreach (var entry in opponentHandCounts)
        {
            if (entry.Value == 1)
                return entry.Key;
        }
        return -1;
    }

    private void RefreshVisual()
    {
        if (canvasGroup == null) return;
        bool isActive = CanCallUno() || FindCatchTarget() >= 0;
        canvasGroup.alpha = isActive ? 1f : inactiveAlpha;
    }
}
