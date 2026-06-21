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
    [SerializeField] private float popupDuration = 3f;

    private IGameLogic gameLogic;
    private Coroutine popupRoutine;
    private Card pendingCard;

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

        drawButton.interactable = gameLogic != null && gameLogic.IsLocalPlayersTurn();
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
        popupRoot.SetActive(true);
        float timer = popupDuration;

        while (timer > 0f)
        {
            timer -= Time.deltaTime;
            yield return null;
        }

        ClosePopup();
    }

    private void HandlePlayYes()
    {
        if (pendingCard != null)
        {
            GameEvents.RaiseCardPlayed(pendingCard);
        }

        ClosePopup();
    }

    private void HandlePlayNo()
    {
        ClosePopup();
    }

    private void ClosePopup()
    {
        if (popupRoutine != null)
        {
            StopCoroutine(popupRoutine);
            popupRoutine = null;
        }

        pendingCard = null;
        if (popupRoot != null)
        {
            popupRoot.SetActive(false);
        }
    }
}
