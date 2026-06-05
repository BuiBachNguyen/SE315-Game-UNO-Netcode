using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Hệ thống tính điểm UNO.
/// Number cards = face value (0-9), Action cards = 20, Wild cards = 50.
/// Người thắng round nhận tổng điểm bài trên tay đối thủ.
/// Match kết thúc khi ai đạt >= matchPointTarget.
/// </summary>
public class ScoringSystem : MonoBehaviour
{
    [SerializeField] private int matchPointTarget = 500;

    private readonly Dictionary<int, int> matchScores = new Dictionary<int, int>();

    public int GetMatchPointTarget() => matchPointTarget;

    public void InitializeScores(int playerCount)
    {
        matchScores.Clear();
        for (int i = 0; i < playerCount; i++)
        {
            matchScores[i] = 0;
        }
    }

    /// <summary>
    /// Tính điểm round: người thắng nhận tổng điểm bài trên tay tất cả đối thủ.
    /// Trả về breakdown điểm từng player (điểm bài trên tay họ).
    /// </summary>
    public Dictionary<int, int> CalculateRoundScore(int winnerIndex, List<Player> players)
    {
        int roundScore = 0;
        Dictionary<int, int> breakdown = new Dictionary<int, int>();

        for (int i = 0; i < players.Count; i++)
        {
            int playerHandScore = players[i].GetHandScore();
            breakdown[i] = playerHandScore;

            if (i != winnerIndex)
            {
                roundScore += playerHandScore;
            }
        }

        matchScores[winnerIndex] += roundScore;
        return breakdown;
    }

    public int GetMatchScore(int playerIndex)
    {
        return matchScores.TryGetValue(playerIndex, out int score) ? score : 0;
    }

    public bool IsMatchOver(int playerIndex)
    {
        return GetMatchScore(playerIndex) >= matchPointTarget;
    }

    public void ResetMatchScores()
    {
        List<int> keys = new List<int>(matchScores.Keys);
        foreach (int key in keys)
        {
            matchScores[key] = 0;
        }
    }

    public Dictionary<int, int> GetAllMatchScores()
    {
        return new Dictionary<int, int>(matchScores);
    }
}
