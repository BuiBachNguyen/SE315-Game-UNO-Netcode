using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class PlayedZone : MonoBehaviour
{
    private List<CardGameObject> playedCards = new List<CardGameObject>();

    public void AddCard(CardGameObject card)
    {
        playedCards.Add(card);
    }

    public List<CardGameObject> GetPlayedCards()
    {
        return playedCards;
    }

    public void ShuffleBack()
    {
        GameManager.Instance.GetDeck().ShuffleBack(playedCards);
        playedCards.Clear();
    }

}
