using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OpponentHandView : MonoBehaviour
{
    [SerializeField] private CardSpriteMap cardSpriteMap;
    [SerializeField] private Transform cardContainer;
    [SerializeField] private Image cardBackPrefab;
    [SerializeField] private GameObject highlightBorder;
    [SerializeField] private int opponentPlayerIndex;

    private readonly List<Image> activeCards = new List<Image>();
    private readonly Stack<Image> pooledCards = new Stack<Image>();

    private void OnEnable()
    {
        GameEvents.OnOpponentHandCountChanged += HandleOpponentHandCountChanged;
        GameEvents.OnTurnChanged += HandleTurnChanged;
    }

    private void OnDisable()
    {
        GameEvents.OnOpponentHandCountChanged -= HandleOpponentHandCountChanged;
        GameEvents.OnTurnChanged -= HandleTurnChanged;
    }

    private void HandleOpponentHandCountChanged(int playerIndex, int cardCount)
    {
        // Each instance tracks a specific opponent to avoid mixing UI across players.
        if (playerIndex != opponentPlayerIndex)
        {
            return;
        }

        UpdateCardBacks(cardCount);
    }

    private void HandleTurnChanged(int playerIndex)
    {
        if (highlightBorder != null)
        {
            highlightBorder.SetActive(playerIndex == opponentPlayerIndex);
        }
    }

    private void UpdateCardBacks(int cardCount)
    {
        for (int i = 0; i < activeCards.Count; i++)
        {
            ReturnToPool(activeCards[i]);
        }
        activeCards.Clear();

        if (cardContainer == null || cardBackPrefab == null || cardSpriteMap == null)
        {
            return;
        }

        Sprite backSprite = cardSpriteMap.GetCardBack();
        for (int i = 0; i < cardCount; i++)
        {
            Image view = GetFromPool();
            view.transform.SetParent(cardContainer, false);
            view.sprite = backSprite;
            activeCards.Add(view);
        }
    }

    private Image GetFromPool()
    {
        if (pooledCards.Count > 0)
        {
            Image view = pooledCards.Pop();
            view.gameObject.SetActive(true);
            return view;
        }

        return Instantiate(cardBackPrefab);
    }

    private void ReturnToPool(Image view)
    {
        if (view == null)
        {
            return;
        }

        view.gameObject.SetActive(false);
        view.transform.SetParent(cardContainer, false);
        pooledCards.Push(view);
    }
}
