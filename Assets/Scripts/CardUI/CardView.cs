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

    private Card cardData;
    private bool isPlayable;

    private void Awake()
    {
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
            canvasGroup.alpha = isPlayable ? 1f : 0.5f;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isPlayable || tooltipRoot == null || tooltipText == null)
        {
            return;
        }

        tooltipText.text = "Match color/number/action";
        tooltipRoot.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (tooltipRoot != null)
        {
            tooltipRoot.SetActive(false);
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
