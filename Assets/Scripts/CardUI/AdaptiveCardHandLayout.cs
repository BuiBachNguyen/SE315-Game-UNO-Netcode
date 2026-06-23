using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class AdaptiveCardHandLayout : MonoBehaviour
{
    public enum HandDirection
    {
        Horizontal,
        Vertical
    }

    [Header("Layout")]
    [SerializeField] private HandDirection direction = HandDirection.Horizontal;
    [SerializeField] private Vector2 cardSize = new Vector2(160f, 240f);
    [SerializeField, Min(0f)] private float maxStep = 110f;
    [SerializeField, Min(0f)] private float minStep = 35f;
    [SerializeField] private bool reverseOrder;

    [Header("Optional")]
    [SerializeField] private bool resizeCards = true;

    private RectTransform container;

    private void Awake()
    {
        CacheContainer();
    }

    private void OnEnable()
    {
        CacheContainer();
        RefreshLayout();
    }

    private void OnValidate()
    {
        cardSize.x = Mathf.Max(1f, cardSize.x);
        cardSize.y = Mathf.Max(1f, cardSize.y);
        maxStep = Mathf.Max(0f, maxStep);
        minStep = Mathf.Clamp(minStep, 0f, maxStep);

        RefreshLayout();
    }

    private void OnTransformChildrenChanged()
    {
        RefreshLayout();
    }

    private void OnRectTransformDimensionsChange()
    {
        RefreshLayout();
    }

    public void RefreshLayout()
    {
        CacheContainer();
        if (container == null)
            return;

        int cardCount = CountActiveCards();
        if (cardCount == 0)
            return;

        float availableSize = direction == HandDirection.Horizontal
            ? container.rect.width
            : container.rect.height;

        float cardLength = direction == HandDirection.Horizontal
            ? cardSize.x
            : cardSize.y;

        float step = CalculateStep(availableSize, cardLength, cardCount);
        float totalLength = cardLength + step * (cardCount - 1);
        float startPosition = -totalLength * 0.5f + cardLength * 0.5f;
        int activeIndex = 0;

        for (int i = 0; i < container.childCount; i++)
        {
            RectTransform card = container.GetChild(i) as RectTransform;
            if (!IsLayoutItem(card))
            {
                continue;
            }

            int layoutIndex = reverseOrder
                ? cardCount - 1 - activeIndex
                : activeIndex;

            if (resizeCards)
                card.sizeDelta = cardSize;

            card.anchorMin = new Vector2(0.5f, 0.5f);
            card.anchorMax = new Vector2(0.5f, 0.5f);
            card.pivot = new Vector2(0.5f, 0.5f);
            card.localRotation = Quaternion.identity;

            float position = startPosition + layoutIndex * step;
            Vector2 layoutPosition = direction == HandDirection.Horizontal
                ? new Vector2(position, 0f)
                : new Vector2(0f, -position);

            CardView cardView = card.GetComponent<CardView>();
            if (cardView != null)
                cardView.SetLayoutPosition(layoutPosition);
            else
            {
                card.localScale = Vector3.one;
                card.anchoredPosition = layoutPosition;
            }

            activeIndex++;
        }
    }

    private float CalculateStep(float availableSize, float cardLength, int cardCount)
    {
        if (cardCount <= 1)
            return 0f;

        float fittedStep = (availableSize - cardLength) / (cardCount - 1);
        return Mathf.Clamp(fittedStep, minStep, maxStep);
    }

    private int CountActiveCards()
    {
        int count = 0;

        for (int i = 0; i < container.childCount; i++)
        {
            if (IsLayoutItem(container.GetChild(i) as RectTransform))
                count++;
        }

        return count;
    }

    private static bool IsLayoutItem(RectTransform child)
    {
        if (child == null || !child.gameObject.activeSelf)
            return false;

        LayoutElement layoutElement = child.GetComponent<LayoutElement>();
        if (layoutElement != null && layoutElement.ignoreLayout)
            return false;

        // Ignore helper objects such as PoolContainer. Both local cards and
        // opponent card backs have a UI Graphic on their root object.
        return child.GetComponent<Graphic>() != null;
    }

    private void CacheContainer()
    {
        if (container == null)
            container = transform as RectTransform;
    }
}
