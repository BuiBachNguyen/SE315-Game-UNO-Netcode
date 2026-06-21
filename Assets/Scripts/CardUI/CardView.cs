using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.UI;

public class CardView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image cardImage;
    [SerializeField] private Image highlight;
    [SerializeField] private Button button;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private CardSpriteMap cardSpriteMap;
    [SerializeField] private GameObject tooltipRoot;
    [SerializeField] private TMP_Text tooltipText;
    [SerializeField] private float hoverOffset = 25f;
    [SerializeField] private float hoverScale = 1.05f;

    private Card cardData;
    private bool isPlayable;
    private bool isHovered;
    private Vector2 layoutPosition;
    private RectTransform rectTransform;
    private Canvas hoverCanvas;

    private void Awake()
    {
        rectTransform = transform as RectTransform;
        hoverCanvas = GetComponent<Canvas>();
        if (hoverCanvas == null)
            hoverCanvas = gameObject.AddComponent<Canvas>();

        if (button != null)
        {
            button.onClick.AddListener(HandleClick);
        }

        if (tooltipRoot != null)
        {
            tooltipRoot.SetActive(false);
        }
    }

    public void Setup(Card card, bool playable)
    {
        cardData = card;
        isPlayable = playable;

        if (cardSpriteMap != null && cardImage != null)
        {
            cardImage.sprite = cardSpriteMap.GetSprite(card.Color, card.Type, card.Number);
        }

        if (highlight != null)
        {
            highlight.gameObject.SetActive(isPlayable);
        }

        if (button != null)
        {
            button.interactable = isPlayable;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = isPlayable ? 1f : 0.75f;
        }
    }

    public void SetLayoutPosition(Vector2 position)
    {
        layoutPosition = position;
        ApplyHoverState();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        ApplyHoverState();

        if (!isPlayable && tooltipRoot != null && tooltipText != null)
        {
            tooltipText.text = "Match color/number/action";
            tooltipRoot.SetActive(true);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        ApplyHoverState();

        if (tooltipRoot != null)
        {
            tooltipRoot.SetActive(false);
        }
    }

    private void OnDisable()
    {
        isHovered = false;

        if (hoverCanvas != null)
            hoverCanvas.overrideSorting = false;

        if (tooltipRoot != null)
            tooltipRoot.SetActive(false);
    }

    private void ApplyHoverState()
    {
        if (rectTransform == null)
            rectTransform = transform as RectTransform;

        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = layoutPosition +
                (isHovered ? Vector2.up * hoverOffset : Vector2.zero);
            rectTransform.localScale = isHovered
                ? Vector3.one * hoverScale
                : Vector3.one;
        }

        if (hoverCanvas != null)
        {
            hoverCanvas.overrideSorting = isHovered;
            hoverCanvas.sortingOrder = isHovered ? 100 : 0;
        }
    }

    private void HandleClick()
    {
        if (cardData == null)
        {
            return;
        }

        GameEvents.RaiseCardPlayed(cardData);
    }
}
