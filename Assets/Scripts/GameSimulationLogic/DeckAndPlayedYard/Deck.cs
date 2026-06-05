using System.Collections.Generic;
using UnityEngine;

public enum ShuffleBackResult
{
    Success,
    PlayZoneNotEnough,
    Failed
}

public class Deck : MonoBehaviour
{
    [SerializeField] private List<CardQuantity> cardQuantities;
    private List<CardGameObject> cardGameObjects;

    private void Awake()
    {
        BuildDeck();
    }

    private void BuildDeck()
    {
        cardGameObjects = new List<CardGameObject>();

        if (cardQuantities != null && cardQuantities.Count > 0)
        {
            BuildDeckFromDefinitions();
        }
        else
        {
            BuildStandardDeck();
        }

        Shuffle();
    }

    /// <summary>
    /// Build deck từ ScriptableObject Card assets — designer có thể tùy chỉnh bộ bài trong Inspector.
    /// </summary>
    private void BuildDeckFromDefinitions()
    {
        foreach (var cardQuantity in cardQuantities)
        {
            if (cardQuantity.card == null) continue;
            for (int i = 0; i < cardQuantity.quantity; i++)
            {
                CardGameObject card = CardBuilder.GetCard(cardQuantity.card).GetComponent<CardGameObject>();
                cardGameObjects.Add(card);
                card.gameObject.transform.SetParent(GameManager.Instance.GetCardHolder().gameObject.transform);
            }
        }
    }

    /// <summary>
    /// Bộ bài UNO chuẩn 108 lá — dùng khi không có deckDefinition.
    /// 4 màu × (1 zero + 2×(1-9) + 2×Skip + 2×Reverse + 2×DrawTwo) + 4 Wild + 4 WildDrawFour.
    /// </summary>
    private void BuildStandardDeck()
    {
        CardColor[] colors = { CardColor.Red, CardColor.Green, CardColor.Blue, CardColor.Yellow };
        Transform holder = GameManager.Instance.GetCardHolder().gameObject.transform;

        foreach (CardColor color in colors)
        {
            // 1 lá số 0
            cardGameObjects.Add(CreateNumberCard(color, 0, holder));

            // 2 lá mỗi số 1-9
            for (int number = 1; number <= 9; number++)
            {
                cardGameObjects.Add(CreateNumberCard(color, number, holder));
                cardGameObjects.Add(CreateNumberCard(color, number, holder));
            }

            // 2 lá Skip
            cardGameObjects.Add(CreateActionCard(color, new List<ActionType> { ActionType.Skip }, 0, holder));
            cardGameObjects.Add(CreateActionCard(color, new List<ActionType> { ActionType.Skip }, 0, holder));

            // 2 lá Reverse
            cardGameObjects.Add(CreateActionCard(color, new List<ActionType> { ActionType.Reverse }, 0, holder));
            cardGameObjects.Add(CreateActionCard(color, new List<ActionType> { ActionType.Reverse }, 0, holder));

            // 2 lá Draw Two
            cardGameObjects.Add(CreateActionCard(color, new List<ActionType> { ActionType.Draw }, 2, holder));
            cardGameObjects.Add(CreateActionCard(color, new List<ActionType> { ActionType.Draw }, 2, holder));
        }

        // 4 lá Wild (ChangeColor)
        for (int i = 0; i < 4; i++)
        {
            cardGameObjects.Add(CreateActionCard(CardColor.Wild, new List<ActionType> { ActionType.ChangeColor }, 0, holder));
        }

        // 4 lá Wild Draw Four (Draw + ChangeColor)
        for (int i = 0; i < 4; i++)
        {
            cardGameObjects.Add(CreateActionCard(CardColor.Wild, new List<ActionType> { ActionType.Draw, ActionType.ChangeColor }, 4, holder));
        }
    }

    private CardGameObject CreateNumberCard(CardColor color, int number, Transform parent)
    {
        GameObject obj = new GameObject($"{color}_{number}");
        obj.transform.SetParent(parent);
        CardGameObject card = obj.AddComponent<CardGameObject>();
        card.SetColor(color);
        card.SetType(CardType.Number);
        card.SetNumber(number);
        card.SetActionTypes(null);
        card.SetDrawAmount(0);
        return card;
    }

    private CardGameObject CreateActionCard(CardColor color, List<ActionType> actions, int drawAmount, Transform parent)
    {
        string name = $"{color}_{string.Join("_", actions)}";
        if (drawAmount > 0) name += $"_+{drawAmount}";

        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent);
        CardGameObject card = obj.AddComponent<CardGameObject>();
        card.SetColor(color);
        card.SetType(CardType.Action);
        card.SetNumber(-1);
        card.SetActionTypes(actions);
        card.SetDrawAmount(drawAmount);
        return card;
    }

    /// <summary>
    /// Fisher-Yates shuffle — xáo trộn 1 lần, sau đó rút từ cuối.
    /// </summary>
    private void Shuffle()
    {
        for (int i = cardGameObjects.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            CardGameObject temp = cardGameObjects[i];
            cardGameObjects[i] = cardGameObjects[j];
            cardGameObjects[j] = temp;
        }
    }

    /// <summary>
    /// Rút bài từ cuối deck (đã shuffle). Nếu hết bài → ShuffleBack từ PlayedZone.
    /// </summary>
    public List<CardGameObject> DrawCard(int quantity)
    {
        List<CardGameObject> drawnCards = new List<CardGameObject>();

        for (int i = 0; i < quantity; i++)
        {
            if (cardGameObjects.Count == 0)
            {
                ShuffleBackResult result = ShuffleBack();
                if (result != ShuffleBackResult.Success)
                {
                    Debug.LogWarning($"Cannot draw more cards. ShuffleBack result: {result}");
                    break;
                }
            }

            if (cardGameObjects.Count == 0)
                break;

            int lastIndex = cardGameObjects.Count - 1;
            CardGameObject drawn = cardGameObjects[lastIndex];
            cardGameObjects.RemoveAt(lastIndex);
            drawnCards.Add(drawn);
        }

        return drawnCards;
    }

    /// <summary>
    /// Lấy bài từ PlayedZone (trừ lá trên cùng) → shuffle → đưa lại deck.
    /// </summary>
    public ShuffleBackResult ShuffleBack()
    {
        PlayedZone playedZone = GameManager.Instance.GetPlayedZone();
        if (playedZone == null)
        {
            Debug.LogError("Cannot shuffle back — PlayedZone is null!");
            return ShuffleBackResult.Failed;
        }

        List<CardGameObject> recycledCards = playedZone.GetCardsExceptTop();
        if (recycledCards.Count == 0)
        {
            return ShuffleBackResult.PlayZoneNotEnough;
        }

        playedZone.ClearExceptTop();
        cardGameObjects.AddRange(recycledCards);
        Shuffle();
        return ShuffleBackResult.Success;
    }

    public int GetRemainingCount()
    {
        return cardGameObjects.Count;
    }
}

[System.Serializable]
public class CardQuantity
{
    public Card card;
    public int quantity;
}
