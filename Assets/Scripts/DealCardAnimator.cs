using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Plays the initial UNO deal on a presentation layer above the real hand UI.
/// The real hands stay hidden while temporary cards animate, so layout components
/// can calculate final positions without fighting DOTween.
/// </summary>
public class DealCardAnimator : MonoBehaviour
{
    [Header("Timing")]
    [SerializeField, Min(0.01f)] private float dealDuration = 0.35f;
    [SerializeField, Min(0f)] private float delayBetweenCards = 0.08f;

    [Header("Motion")]
    [SerializeField, Min(0.01f)] private float startScale = 0.75f;
    [SerializeField, Min(0f)] private float randomRotationRange = 12f;
    [SerializeField, Min(0f)] private float arcHeight = 45f;
    [SerializeField, Min(0f)] private float cardSpacing = 28f;

    [Header("Scene References")]
    [SerializeField] private Transform deckTransform;
    [Tooltip("Order: Bottom, Left, Top, Right.")]
    [SerializeField] private RectTransform[] playerHandParents = new RectTransform[4];
    [SerializeField] private RectTransform animationLayer;
    [SerializeField] private Image cardBackPrefab;

    private readonly List<RectTransform> temporaryCards = new List<RectTransform>();
    private readonly List<HandCanvasState> handCanvasStates = new List<HandCanvasState>();
    private Sequence dealSequence;

    private struct HandCanvasState
    {
        public CanvasGroup Group;
        public float Alpha;
        public bool Interactable;
        public bool BlocksRaycasts;
    }

    private void OnEnable()
    {
        GameEvents.OnInitialDealStarted += HandleInitialDealStarted;
    }

    private void OnDisable()
    {
        GameEvents.OnInitialDealStarted -= HandleInitialDealStarted;
        CancelCurrentDeal();
    }

    private void HandleInitialDealStarted(int playerCount, int cardsPerPlayer)
    {
        CancelCurrentDeal();

        int seatCount = Mathf.Min(playerCount, playerHandParents.Length);
        if (!CanAnimate(seatCount, cardsPerPlayer))
        {
            Debug.LogWarning("[DealCardAnimator] Missing scene references. Skipping deal animation.");
            GameEvents.RaiseInitialDealCompleted();
            return;
        }

        HideRealHands(seatCount);
        dealSequence = DOTween.Sequence().SetUpdate(true);

        int dealOrder = 0;
        for (int cardIndex = 0; cardIndex < cardsPerPlayer; cardIndex++)
        {
            for (int seatIndex = 0; seatIndex < seatCount; seatIndex++)
            {
                RectTransform targetHand = playerHandParents[seatIndex];
                if (targetHand == null)
                {
                    continue;
                }

                RectTransform visual = CreateTemporaryCard();
                float startTime = dealOrder * delayBetweenCards;
                Sequence cardTween = CreateCardTween(
                    visual,
                    targetHand,
                    seatIndex,
                    cardIndex,
                    cardsPerPlayer);

                dealSequence.InsertCallback(startTime, PlayDrawVfx);
                dealSequence.Insert(startTime, cardTween);
                dealOrder++;
            }
        }

        dealSequence.OnComplete(CompleteDeal);
    }

    private void PlayDrawVfx()
    {
        SoundManager.Instance.PlayDrawVfx();
    }

    private bool CanAnimate(int seatCount, int cardsPerPlayer)
    {
        return seatCount > 0
            && cardsPerPlayer > 0
            && deckTransform != null
            && animationLayer != null
            && cardBackPrefab != null;
    }

    private RectTransform CreateTemporaryCard()
    {
        Image image = Instantiate(cardBackPrefab, animationLayer);
        image.raycastTarget = false;

        RectTransform card = image.rectTransform;
        RectTransform deckRect = deckTransform as RectTransform;
        if (deckRect != null)
        {
            // Match the temporary deal card to the visible deck instead of
            // keeping the prefab's possibly smaller default dimensions.
            card.sizeDelta = deckRect.rect.size;
        }

        card.position = deckTransform.position;
        card.localScale = Vector3.one * startScale;
        card.localRotation = Quaternion.Euler(
            0f,
            0f,
            Random.Range(-randomRotationRange, randomRotationRange));
        card.SetAsLastSibling();

        temporaryCards.Add(card);
        return card;
    }

    private Sequence CreateCardTween(
        RectTransform card,
        RectTransform targetHand,
        int seatIndex,
        int cardIndex,
        int cardsPerPlayer)
    {
        Vector3 targetPosition = GetTargetWorldPosition(
            targetHand,
            seatIndex,
            cardIndex,
            cardsPerPlayer);

        Vector3 startPosition = card.position;
        Vector3 direction = targetPosition - startPosition;
        Vector3 perpendicular = new Vector3(-direction.y, direction.x, 0f).normalized;
        Vector3 midpoint = Vector3.Lerp(startPosition, targetPosition, 0.5f)
            + perpendicular * arcHeight;

        Vector3[] path = { startPosition, midpoint, targetPosition };
        Sequence tween = DOTween.Sequence();
        tween.Join(card.DOPath(path, dealDuration, PathType.CatmullRom)
            .SetEase(Ease.OutCubic));
        tween.Join(card.DOScale(Vector3.one, dealDuration)
            .SetEase(Ease.OutBack));
        tween.Join(card.DORotate(Vector3.zero, dealDuration, RotateMode.Fast)
            .SetEase(Ease.OutCubic));
        return tween;
    }

    private Vector3 GetTargetWorldPosition(
        RectTransform targetHand,
        int seatIndex,
        int cardIndex,
        int cardsPerPlayer)
    {
        float centeredIndex = cardIndex - (cardsPerPlayer - 1) * 0.5f;
        bool verticalSeat = seatIndex == 1 || seatIndex == 3;
        Vector3 localOffset = verticalSeat
            ? new Vector3(0f, -centeredIndex * cardSpacing, 0f)
            : new Vector3(centeredIndex * cardSpacing, 0f, 0f);

        return targetHand.TransformPoint(localOffset);
    }

    private void HideRealHands(int seatCount)
    {
        handCanvasStates.Clear();

        for (int i = 0; i < seatCount; i++)
        {
            RectTransform hand = playerHandParents[i];
            if (hand == null)
            {
                continue;
            }

            CanvasGroup group = hand.GetComponent<CanvasGroup>();
            if (group == null)
            {
                group = hand.gameObject.AddComponent<CanvasGroup>();
            }

            handCanvasStates.Add(new HandCanvasState
            {
                Group = group,
                Alpha = group.alpha,
                Interactable = group.interactable,
                BlocksRaycasts = group.blocksRaycasts
            });

            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
        }
    }

    private void CompleteDeal()
    {
        dealSequence = null;
        ClearTemporaryCards();
        RestoreRealHands();
        GameEvents.RaiseInitialDealCompleted();
    }

    private void CancelCurrentDeal()
    {
        if (dealSequence != null)
        {
            dealSequence.Kill();
            dealSequence = null;
        }

        ClearTemporaryCards();
        RestoreRealHands();
    }

    private void ClearTemporaryCards()
    {
        for (int i = 0; i < temporaryCards.Count; i++)
        {
            RectTransform card = temporaryCards[i];
            if (card != null)
            {
                card.DOKill();
                Destroy(card.gameObject);
            }
        }

        temporaryCards.Clear();
    }

    private void RestoreRealHands()
    {
        for (int i = 0; i < handCanvasStates.Count; i++)
        {
            HandCanvasState state = handCanvasStates[i];
            if (state.Group == null)
            {
                continue;
            }

            state.Group.alpha = state.Alpha;
            state.Group.interactable = state.Interactable;
            state.Group.blocksRaycasts = state.BlocksRaycasts;
        }

        handCanvasStates.Clear();
    }
}
