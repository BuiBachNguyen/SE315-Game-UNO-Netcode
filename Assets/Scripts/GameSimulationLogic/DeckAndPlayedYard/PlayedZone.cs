using System.Collections.Generic;
using UnityEngine;

public class PlayedZone : MonoBehaviour
{
    private List<CardGameObject> playedCards = new List<CardGameObject>();

    public void AddCard(CardGameObject card)
    {
        playedCards.Add(card);
    }

    /// <summary>
    /// Lá bài trên đỉnh discard pile — dùng để validate bài đánh.
    /// </summary>
    public CardGameObject GetTopCard()
    {
        if (playedCards.Count == 0)
            return null;
        return playedCards[playedCards.Count - 1];
    }

    /// <summary>
    /// Lấy tất cả lá trừ lá trên cùng — dùng cho ShuffleBack (giữ lại top card).
    /// </summary>
    public List<CardGameObject> GetCardsExceptTop()
    {
        if (playedCards.Count <= 1)
            return new List<CardGameObject>();

        List<CardGameObject> cards = new List<CardGameObject>();
        for (int i = 0; i < playedCards.Count - 1; i++)
        {
            cards.Add(playedCards[i]);
        }
        return cards;
    }

    /// <summary>
    /// Xóa tất cả trừ lá trên cùng — dùng sau khi ShuffleBack lấy bài.
    /// </summary>
    public void ClearExceptTop()
    {
        if (playedCards.Count <= 1)
            return;

        CardGameObject top = playedCards[playedCards.Count - 1];
        playedCards.Clear();
        playedCards.Add(top);
    }

    public List<CardGameObject> GetPlayedCards()
    {
        return playedCards;
    }

    public void ClearPlayedCard()
    {
        playedCards.Clear();
    }

    public int GetCount()
    {
        return playedCards.Count;
    }
}
