using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Quản lý UI hiển thị bài trên tay local player.
/// Sử dụng object pooling để tái sử dụng CardView thay vì Instantiate/Destroy liên tục.
/// </summary>
public class HandView : MonoBehaviour
{
    [SerializeField] private Transform handContainer;
    [SerializeField] private Transform poolContainer;
    [SerializeField] private CardView cardViewPrefab;
    [SerializeField] private MonoBehaviour gameLogicBehaviour;

    private IGameLogic gameLogic;
    private readonly List<CardView> activeViews = new List<CardView>();
    private readonly Stack<CardView> pooledViews = new Stack<CardView>();

    private void Awake()
    {
        gameLogic = gameLogicBehaviour as IGameLogic;
        if (gameLogic == null)
        {
            Debug.LogError("HandView requires a component that implements IGameLogic.");
        }

        if (poolContainer == null)
        {
            poolContainer = handContainer;
        }
    }

    private void OnEnable()
    {
        GameEvents.OnHandUpdated += HandleHandUpdated;
    }

    private void OnDisable()
    {
        GameEvents.OnHandUpdated -= HandleHandUpdated;
    }

    private void HandleHandUpdated(List<CardGameObject> cards)
    {
        // Recycle existing views
        for (int i = 0; i < activeViews.Count; i++)
        {
            ReturnToPool(activeViews[i]);
        }
        activeViews.Clear();

        if (cards == null || handContainer == null || cardViewPrefab == null)
        {
            return;
        }

        for (int i = 0; i < cards.Count; i++)
        {
            CardGameObject card = cards[i];
            CardView view = GetFromPool();
            bool playable = gameLogic != null && gameLogic.IsValidPlay(card);

            view.transform.SetParent(handContainer, false);
            view.Setup(card, playable);
            activeViews.Add(view);
        }
    }

    private CardView GetFromPool()
    {
        if (pooledViews.Count > 0)
        {
            CardView view = pooledViews.Pop();
            view.gameObject.SetActive(true);
            return view;
        }

        return Instantiate(cardViewPrefab);
    }

    private void ReturnToPool(CardView view)
    {
        if (view == null)
        {
            return;
        }

        view.gameObject.SetActive(false);
        view.transform.SetParent(poolContainer, false);
        pooledViews.Push(view);
    }
}
