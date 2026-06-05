using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField] private List<CardGameObject> handCards = new List<CardGameObject>();
    [SerializeField] private int playerId;

    /// <summary>
    /// Player đã gọi UNO trong turn hiện tại chưa.
    /// </summary>
    public bool HasCalledUno { get; set; }

    public int GetPlayerId() => playerId;
    public int GetHandCount() => handCards.Count;
    public List<CardGameObject> GetHandCards() => handCards;

    public void SetPlayerId(int id)
    {
        playerId = id;
    }

    /// <summary>
    /// Rút bài từ Deck và thêm vào tay.
    /// </summary>
    public List<CardGameObject> DrawCard(int quantity)
    {
        List<CardGameObject> drawnCards = GameManager.Instance.GetDeck().DrawCard(quantity);
        handCards.AddRange(drawnCards);
        return drawnCards;
    }

    /// <summary>
    /// Đánh 1 lá bài — xóa khỏi tay và thêm vào PlayedZone.
    /// </summary>
    public bool PlayCard(CardGameObject card)
    {
        if (card == null || !handCards.Contains(card))
        {
            Debug.LogError("Card not found in hand!");
            return false;
        }

        handCards.Remove(card);
        GameManager.Instance.GetPlayedZone().AddCard(card);
        return true;
    }

    /// <summary>
    /// Tìm và xóa 1 lá bài khỏi tay (dùng Guid Id matching).
    /// Trả về lá bài đã xóa, hoặc null nếu không tìm thấy.
    /// </summary>
    public CardGameObject FindAndRemoveCard(CardGameObject target)
    {
        for (int i = 0; i < handCards.Count; i++)
        {
            if (handCards[i].Id == target.Id)
            {
                CardGameObject card = handCards[i];
                handCards.RemoveAt(i);
                return card;
            }
        }
        return null;
    }

    /// <summary>
    /// Tính tổng điểm tất cả lá trên tay — dùng cho scoring cuối round.
    /// </summary>
    public int GetHandScore()
    {
        int score = 0;
        foreach (var card in handCards)
        {
            score += card.GetScoreValue();
        }
        return score;
    }

    /// <summary>
    /// Reset trạng thái cho round mới.
    /// </summary>
    public void ResetForNewRound()
    {
        handCards.Clear();
        HasCalledUno = false;
    }
}
