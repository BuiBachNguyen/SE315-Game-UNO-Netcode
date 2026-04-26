using NUnit.Framework;
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
    private void BuildDeck()
    {
        cardGameObjects = new List<CardGameObject>();
        foreach (var cardQuantity in cardQuantities)
        {
            for (int i = 0; i < cardQuantity.quantity; i++)
            {
                CardGameObject card = CardBuilder.GetCard(cardQuantity.card).GetComponent<CardGameObject>();
                cardGameObjects.Add(card);
                card.gameObject.transform.SetParent(GameManager.Instance.GetCardHolder().gameObject.transform);
            }
        }
    }

    private void Awake()
    {
        BuildDeck();
    }

    public List<CardGameObject> DrawCard(int quantity)
    {
        if (quantity > cardGameObjects.Count)
        {
            ShuffleBackResult result = ShuffleBack(quantity);
            if (result == ShuffleBackResult.PlayZoneNotEnough)
            {
                Debug.LogWarning("Not enough cards in the played zone to shuffle back!Checking Winning");
                return new List<CardGameObject>();
            }
            else if (result == ShuffleBackResult.Failed)
            {
                Debug.LogError("Failed to shuffle back cards!");
                return new List<CardGameObject>();
            }
        }

        List<CardGameObject> drawnCards = new List<CardGameObject>();
        for (int i = 0; i < quantity; i++)
        {
            int randomIndex = Random.Range(0, cardGameObjects.Count);
            CardGameObject drawnCard = cardGameObjects[randomIndex];
            cardGameObjects.RemoveAt(randomIndex);
            drawnCards.Add(drawnCard);
        }

        return drawnCards;
    }

    public ShuffleBackResult ShuffleBack(int drawquantity)
    {
        List<CardGameObject> cards= GameManager.Instance.GetPlayedZone().GetPlayedCards();
        if (cards==null)
        {
            Debug.LogError("Cannot shuffle back null cards!");
            return ShuffleBackResult.Failed;
        }

        if (cards.Count + cardGameObjects.Count < drawquantity)
        {
            // No cards to shuffle back, so we can call end match and count player card to determine the winner
            return ShuffleBackResult.PlayZoneNotEnough;
        }

        GameManager.Instance.GetPlayedZone().ClearPlayedCard();
        cardGameObjects.AddRange(cards);
        return ShuffleBackResult.Success;
    }
}

[System.Serializable]
public class CardQuantity
{
    public Card card;
    public int quantity;
}

