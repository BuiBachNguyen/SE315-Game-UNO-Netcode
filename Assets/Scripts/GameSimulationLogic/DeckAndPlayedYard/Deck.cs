using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

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
                cardGameObjects.Add(CardBuilder.GetCard(cardQuantity.card).GetComponent<CardGameObject>());
            }
        }
    }

    public List<CardGameObject> DrawCard(int quantity)
    {
        if (quantity > cardGameObjects.Count)
        {
            Debug.LogWarning("Not enough cards in the deck!");
            return null;
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

    public void ShuffleBack(List<CardGameObject> cards)
    {
        if (cards==null)
        {
            Debug.LogError("Cannot shuffle back null cards!");
            return;
        }

        if (cards.Count == 0)
        {
            // No cards to shuffle back, so we can call end match and count player card to determine the winner
            return;
        }

        cardGameObjects.AddRange(cards);
    }
}

[System.Serializable]
public class CardQuantity
{
    public Card card;
    public int quantity;
}

