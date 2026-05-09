using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UnoButtonView : MonoBehaviour
{
    [SerializeField] private int localPlayerIndex = 0;

    [SerializeField] private GameObject unoButtonRoot;
    [SerializeField] private Button unoButton;
    [SerializeField] private GameObject unoPulseHighlight;

    [SerializeField] private GameObject catchButtonRoot;
    [SerializeField] private Button catchButton;
    [SerializeField] private float catchAutoHideSeconds = 2f;

    private int localHandCount;
    private bool hasCalledUno;
    private int currentTurnIndex;

    private readonly Dictionary<int, int> opponentHandCounts = new Dictionary<int, int>();
    private Coroutine pulseRoutine;
    private Coroutine catchHideRoutine;
    private int catchTargetIndex = -1;

    private void Awake()
    {
        if (unoButton != null)
        {
            unoButton.onClick.AddListener(HandleUnoClicked);
        }

        if (catchButton != null)
        {
            catchButton.onClick.AddListener(HandleCatchClicked);
        }
    }

    private void Start()
    {
        SetUnoVisible(false);
        SetCatchVisible(false);
    }

    private void OnEnable()
    {
        GameEvents.OnHandUpdated += HandleHandUpdated;
        GameEvents.OnOpponentHandCountChanged += HandleOpponentHandCountChanged;
        GameEvents.OnTurnChanged += HandleTurnChanged;
    }

    private void OnDisable()
    {
        GameEvents.OnHandUpdated -= HandleHandUpdated;
        GameEvents.OnOpponentHandCountChanged -= HandleOpponentHandCountChanged;
        GameEvents.OnTurnChanged -= HandleTurnChanged;
    }

    private void HandleHandUpdated(List<Card> cards)
    {
        localHandCount = cards != null ? cards.Count : 0;
        UpdateUnoButtonState();
    }

    private void HandleOpponentHandCountChanged(int playerIndex, int cardCount)
    {
        opponentHandCounts[playerIndex] = cardCount;
        UpdateCatchButtonState();
    }

    private void HandleTurnChanged(int playerIndex)
    {
        currentTurnIndex = playerIndex;
        // Chỉ reset khi bắt đầu lượt MỚI của local player
        if (playerIndex == localPlayerIndex)
        {
            hasCalledUno = false;
        }
        UpdateUnoButtonState();
        UpdateCatchButtonState();
    }

    private void UpdateUnoButtonState()
    {
        bool shouldShow = localHandCount == 1 && !hasCalledUno;
        SetUnoVisible(shouldShow);

        if (localHandCount == 1 && shouldShow)
        {
            StartPulse();
        }
        else
        {
            StopPulse();
        }
    }

    private void UpdateCatchButtonState()
    {
        int target = FindOpponentWithOneCard();
        if (target == -1)
        {
            SetCatchVisible(false);
            return;
        }

        catchTargetIndex = target;
        SetCatchVisible(true);

        if (catchHideRoutine != null)
        {
            StopCoroutine(catchHideRoutine);
        }

        // Auto-hide to avoid lingering catch prompts.
        catchHideRoutine = StartCoroutine(AutoHideCatch());
    }

    private int FindOpponentWithOneCard()
    {
        foreach (KeyValuePair<int, int> entry in opponentHandCounts)
        {
            if (entry.Value == 1)
            {
                return entry.Key;
            }
        }

        return -1;
    }

    private void HandleUnoClicked()
    {
        hasCalledUno = true;
        GameEvents.RaiseUnoCalled();
        UpdateUnoButtonState();
    }

    private void HandleCatchClicked()
    {
        if (catchTargetIndex < 0)
        {
            return;
        }

        GameEvents.RaiseCatchUno(catchTargetIndex);
        SetCatchVisible(false);
    }

    private IEnumerator AutoHideCatch()
    {
        yield return new WaitForSeconds(catchAutoHideSeconds);
        SetCatchVisible(false);
    }

    private void SetUnoVisible(bool visible)
    {
        if (unoButtonRoot != null)
        {
            unoButtonRoot.SetActive(visible);
        }
    }

    private void SetCatchVisible(bool visible)
    {
        if (catchButtonRoot != null)
        {
            catchButtonRoot.SetActive(visible);
        }
    }

    private void StartPulse()
    {
        if (unoPulseHighlight == null)
        {
            return;
        }

        if (pulseRoutine != null)
        {
            return;
        }

        // Pulse to emphasize the required UNO call when only one card remains.
        pulseRoutine = StartCoroutine(PulseHighlight());
    }

    private void StopPulse()
    {
        if (pulseRoutine != null)
        {
            StopCoroutine(pulseRoutine);
            pulseRoutine = null;
        }

        if (unoPulseHighlight != null)
        {
            unoPulseHighlight.transform.localScale = Vector3.one;
        }
    }

    private IEnumerator PulseHighlight()
    {
        float t = 0f;
        while (true)
        {
            t += Time.deltaTime * 3f;
            float scale = 1f + Mathf.Sin(t) * 0.08f;
            unoPulseHighlight.transform.localScale = new Vector3(scale, scale, 1f);
            yield return null;
        }
    }
}
