using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CardView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image cardImage;
    [SerializeField] private Button button;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private CardSpriteMap cardSpriteMap;
    [SerializeField] private float hoverOffset = 25f;
    [SerializeField] private float hoverScale = 1.05f;

    private Card cardData;
    private bool canInteract;
    private bool isHovered;
    private Vector2 layoutPosition;
    private RectTransform rectTransform;
    private Canvas hoverCanvas;
    private GraphicRaycaster graphicRaycaster;

    private void Awake()
    {
        rectTransform = transform as RectTransform;
        hoverCanvas = GetComponent<Canvas>();
        if (hoverCanvas == null)
        {
            hoverCanvas = gameObject.AddComponent<Canvas>();
        }

        graphicRaycaster = GetComponent<GraphicRaycaster>();
        if (graphicRaycaster == null)
        {
            graphicRaycaster = gameObject.AddComponent<GraphicRaycaster>();
        }

        if (button != null)
        {
            button.onClick.AddListener(HandleClick);

            ColorBlock colors = button.colors;
            colors.disabledColor = Color.white;
            button.colors = colors;
        }
    }

    public void Setup(Card card, bool allowInteraction)
    {
        cardData = card;
        canInteract = allowInteraction;
        isHovered = false;

        if (cardSpriteMap != null && cardImage != null)
        {
            cardImage.sprite = cardSpriteMap.GetSprite(card.Color, card.Type, card.Number);
        }

        if (button != null)
        {
            button.interactable = canInteract;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = canInteract;
            canvasGroup.blocksRaycasts = canInteract;
        }

        if (graphicRaycaster != null)
        {
            graphicRaycaster.enabled = canInteract;
        }

        ApplyHoverState();
    }

    public void SetLayoutPosition(Vector2 position)
    {
        layoutPosition = position;
        ApplyHoverState();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!canInteract)
        {
            return;
        }

        isHovered = true;
        ApplyHoverState();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        ApplyHoverState();
    }

    private void OnDisable()
    {
        isHovered = false;

        if (hoverCanvas != null)
        {
            hoverCanvas.overrideSorting = false;
        }
    }

    private void ApplyHoverState()
    {
        if (rectTransform == null)
            rectTransform = transform as RectTransform;

        if (rectTransform != null)
        {
            bool shouldHover = canInteract && isHovered;
            rectTransform.anchoredPosition = layoutPosition
                + (shouldHover ? Vector2.up * hoverOffset : Vector2.zero);
            rectTransform.localScale = shouldHover
                ? Vector3.one * hoverScale
                : Vector3.one;
        }

        if (hoverCanvas != null)
        {
            bool shouldHover = canInteract && isHovered;
            hoverCanvas.overrideSorting = shouldHover;
            hoverCanvas.sortingOrder = shouldHover ? 100 : 0;
        }
    }

    private void HandleClick()
    {
        if (!canInteract || cardData == null)
        {
            return;
        }

        GameEvents.RaiseCardPlayed(cardData);
    }
}
