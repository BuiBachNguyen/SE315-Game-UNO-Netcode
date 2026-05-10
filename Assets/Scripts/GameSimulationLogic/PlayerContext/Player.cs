using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField] private List<CardGameObject> handCards = new List<CardGameObject>();
    [SerializeField] private int playerId;

    private void Start()
    {
        // Initialize player state if needed
        DrawCard(5); // Example: Draw 5 cards at the start of the game
    }

    public void DrawCard(int quantity)
    {
        List<CardGameObject> drawnCards = GameManager.Instance.GetDeck().DrawCard(quantity);
        handCards.AddRange(drawnCards);
    }

    public void PlayCard(CardGameObject card)
    {
        if (handCards.Contains(card))
        {
            if (card)
            handCards.Remove(card);
            GameManager.Instance.GetPlayedZone().AddCard(card);
        }
        else
        {
            Debug.LogError("Card not found in hand!");
        }
    }
}
