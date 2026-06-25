using System.Collections.Generic;
using UnityEngine;

public class HandView : MonoBehaviour
{
    [SerializeField] private Transform handContainer;
    [SerializeField] private Transform poolContainer;
    [SerializeField] private CardView cardViewPrefab;
    [SerializeField] private MonoBehaviour gameLogicBehaviour;

    private IGameLogic gameLogic;
    private AdaptiveCardHandLayout handLayout;
    private CanvasGroup handCanvasGroup;
    private readonly List<CardView> activeViews = new List<CardView>();
    private readonly Stack<CardView> pooledViews = new Stack<CardView>();
    private readonly List<Card> currentCards = new List<Card>();

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

        if (handContainer != null)
        {
            handLayout = handContainer.GetComponent<AdaptiveCardHandLayout>();
            handCanvasGroup = handContainer.GetComponent<CanvasGroup>();
            if (handCanvasGroup == null)
            {
                handCanvasGroup = handContainer.gameObject.AddComponent<CanvasGroup>();
            }
        }
    }

    private void OnEnable()
    {
        GameEvents.OnHandUpdated += HandleHandUpdated;
        GameEvents.OnTurnChanged += HandlePlayStateChanged;
        GameEvents.OnColorChanged += HandlePlayStateChanged;

        if (gameLogicBehaviour is NetworkGameManager networkGameManager)
            networkGameManager.RefreshLocalHand();
    }

    private void OnDisable()
    {
        GameEvents.OnHandUpdated -= HandleHandUpdated;
        GameEvents.OnTurnChanged -= HandlePlayStateChanged;
        GameEvents.OnColorChanged -= HandlePlayStateChanged;
    }

    private void HandleHandUpdated(List<Card> cards)
    {
        currentCards.Clear();
        if (cards != null)
        {
            currentCards.AddRange(cards);
        }

        RebuildHand();
    }

    private void HandlePlayStateChanged(int playerIndex)
    {
        RebuildHand();
    }

    private void HandlePlayStateChanged(CardColor color)
    {
        RebuildHand();
    }

    private void RebuildHand()
    {
        // Recycle existing views before creating or reusing new ones.
        for (int i = 0; i < activeViews.Count; i++)
        {
            ReturnToPool(activeViews[i]);
        }
        activeViews.Clear();

        if (handContainer == null || cardViewPrefab == null)
        {
            return;
        }

        bool canInteract = gameLogic != null && gameLogic.IsLocalPlayersTurn();
        UpdateHandInteraction(canInteract);

        for (int i = 0; i < currentCards.Count; i++)
        {
            Card card = currentCards[i];
            CardView view = GetFromPool();

            view.transform.SetParent(handContainer, false);
            view.transform.SetSiblingIndex(i);
            view.Setup(card, canInteract);
            activeViews.Add(view);
        }

        handLayout?.RefreshLayout();
    }

    private void UpdateHandInteraction(bool canInteract)
    {
        if (handCanvasGroup == null)
        {
            return;
        }

        // DealCardAnimator owns visibility during the initial deal.
        // This view only controls whether the hand receives input.
        handCanvasGroup.interactable = canInteract;
        handCanvasGroup.blocksRaycasts = canInteract;
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
