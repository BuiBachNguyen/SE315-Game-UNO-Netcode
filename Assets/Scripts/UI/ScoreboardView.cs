using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ScoreboardView : MonoBehaviour
{
    [Header("Round End")]
    [SerializeField] private GameObject roundEndPanel;
    [SerializeField] private TMP_Text roundWinnerText;
    [SerializeField] private TMP_Text[] roundScoreLines;
    [SerializeField] private Button nextRoundButton;

    [Header("Match End")]
    [SerializeField] private GameObject matchEndPanel;
    [SerializeField] private TMP_Text matchWinnerText;
    [SerializeField] private TMP_Text matchTotalScoreText;
    [SerializeField] private Button rematchButton;
    [SerializeField] private Button mainMenuButton;

    private void Awake()
    {
        if (nextRoundButton != null) nextRoundButton.onClick.AddListener(HandleNextRound);
        if (rematchButton != null) rematchButton.onClick.AddListener(HandleRematch);
        if (mainMenuButton != null) mainMenuButton.onClick.AddListener(HandleMainMenu);
    }

    private void Start()
    {
        if (roundEndPanel != null) roundEndPanel.SetActive(false);
        if (matchEndPanel != null) matchEndPanel.SetActive(false);
    }

    private void OnEnable()
    {
        GameEvents.OnRoundEnd += HandleRoundEnd;
        GameEvents.OnMatchEnd += HandleMatchEnd;
    }

    private void OnDisable()
    {
        GameEvents.OnRoundEnd -= HandleRoundEnd;
        GameEvents.OnMatchEnd -= HandleMatchEnd;
    }

    private void HandleRoundEnd(int winnerIndex, Dictionary<int, int> scoreBreakdown)
    {
        if (roundEndPanel != null) roundEndPanel.SetActive(true);
        if (roundWinnerText != null) roundWinnerText.text = "Winner: Player " + winnerIndex;

        if (roundScoreLines != null && scoreBreakdown != null)
        {
            for (int i = 0; i < roundScoreLines.Length; i++)
            {
                if (roundScoreLines[i] == null) continue;
                scoreBreakdown.TryGetValue(i, out int score);
                roundScoreLines[i].text = "Player " + i + ": " + score;
            }
        }
    }

    private void HandleMatchEnd(int winnerIndex, int totalScore)
    {
        if (roundEndPanel != null) roundEndPanel.SetActive(false);
        if (matchEndPanel != null) matchEndPanel.SetActive(true);
        if (matchWinnerText != null) matchWinnerText.text = "Winner: Player " + winnerIndex;
        if (matchTotalScoreText != null) matchTotalScoreText.text = "Total: " + totalScore;
    }

    private void HandleNextRound()
    {
        if (roundEndPanel != null) roundEndPanel.SetActive(false);
        GameEvents.RaiseNextRoundRequested();
    }

    private void HandleRematch()
    {
        if (matchEndPanel != null) matchEndPanel.SetActive(false);
        if (roundEndPanel != null) roundEndPanel.SetActive(false);
        GameEvents.RaiseRematchRequested();
    }

    private void HandleMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
}
