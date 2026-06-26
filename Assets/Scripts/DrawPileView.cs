using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DrawPileView : MonoBehaviour
{
    [SerializeField] private Button drawButton;
    [SerializeField] private MonoBehaviour gameLogicBehaviour;

    [Header("Popup")]
    [SerializeField] private GameObject popupRoot;
    [SerializeField] private TMP_Text popupText;
    [SerializeField] private Button yesButton;
    [SerializeField] private Button noButton;
    [SerializeField] private float popupDuration = 7f;
    [SerializeField] private int popupSortingOrder = 32000;

    private IGameLogic gameLogic;
    private Coroutine popupRoutine;
    private Card pendingCard;
    private bool isAwaitingDrawDecision;
    private Canvas popupCanvas;

    private void Awake()
    {
        gameLogic = gameLogicBehaviour as IGameLogic;
        if (gameLogic == null)
        {
            Debug.LogError("DrawPileView requires a component that implements IGameLogic.");
        }

        if (drawButton != null)
        {
            drawButton.onClick.AddListener(HandleDrawClicked);
        }

        if (yesButton != null)
        {
            yesButton.onClick.AddListener(HandlePlayYes);
        }

        if (noButton != null)
        {
            noButton.onClick.AddListener(HandlePlayNo);
        }

        if (popupRoot != null)
        {
            popupRoot.SetActive(false);
        }
    }

    private void OnEnable()
    {
        GameEvents.OnTurnChanged += HandleTurnChanged;
        GameEvents.OnCardDrawn += HandleCardDrawn;
        UpdateInteractable();
    }

    private void OnDisable()
    {
        GameEvents.OnTurnChanged -= HandleTurnChanged;
        GameEvents.OnCardDrawn -= HandleCardDrawn;
    }

    private void HandleTurnChanged(int playerIndex)
    {
        UpdateInteractable();
    }

    private void UpdateInteractable()
    {
        if (drawButton == null)
        {
            return;
        }

        drawButton.interactable = gameLogic != null
            && gameLogic.IsLocalPlayersTurn()
            && !isAwaitingDrawDecision;
    }

    private void HandleDrawClicked()
    {
        GameEvents.RaiseDrawCardRequested();
    }

    private void HandleCardDrawn(Card card, bool isPlayable)
    {
        if (!isPlayable || popupRoot == null)
        {
            return;
        }

        pendingCard = card;
        isAwaitingDrawDecision = true;
        UpdateInteractable();

        if (popupText != null)
        {
            popupText.text = "Play this card?";
        }

        if (popupRoutine != null)
        {
            StopCoroutine(popupRoutine);
        }

        popupRoutine = StartCoroutine(PopupCountdown());
    }

    private IEnumerator PopupCountdown()
    {
        ConfigurePopupSorting();
        popupRoot.SetActive(true);
        float timer = popupDuration;

        while (timer > 0f)
        {
            timer -= Time.deltaTime;
            yield return null;
        }

        ClosePopup(true);
    }

    private void HandlePlayYes()
    {
        if (pendingCard != null)
        {
            GameEvents.RaiseCardPlayed(pendingCard);
        }

        ClosePopup(false);
    }

    private void HandlePlayNo()
    {
        ClosePopup(true);
    }

    private void ClosePopup(bool declineDrawnCard)
    {
        bool shouldDecline = declineDrawnCard && pendingCard != null;

        if (popupRoutine != null)
        {
            StopCoroutine(popupRoutine);
            popupRoutine = null;
        }

        pendingCard = null;
        isAwaitingDrawDecision = false;
        if (popupRoot != null)
        {
            ConfigurePopupSorting();
            popupRoot.SetActive(false);
        }

        UpdateInteractable();

        if (shouldDecline)
        {
            GameEvents.RaiseDrawnCardDeclined();
        }
    }

    private void ConfigurePopupSorting()
    {
        if (popupRoot == null)
        {
            return;
        }

        Canvas rootCanvas = popupRoot.GetComponentInParent<Canvas>()?.rootCanvas;
        if (rootCanvas != null && popupRoot.transform.parent != rootCanvas.transform)
        {
            popupRoot.transform.SetParent(rootCanvas.transform, true);
        }

        popupRoot.transform.SetAsLastSibling();

        if (popupCanvas == null)
        {
            popupCanvas = popupRoot.GetComponent<Canvas>();
            if (popupCanvas == null)
            {
                popupCanvas = popupRoot.AddComponent<Canvas>();
            }
        }

        popupCanvas.overrideSorting = true;
        popupCanvas.sortingOrder = popupSortingOrder;

        if (popupRoot.GetComponent<GraphicRaycaster>() == null)
        {
            popupRoot.AddComponent<GraphicRaycaster>();
        }
    }
}
