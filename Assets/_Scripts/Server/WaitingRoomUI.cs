using TMPro;
using UnityEngine;

public class WaitingoomUI : MonoBehaviour
{
    public static WaitingoomUI Instance;

    public TextMeshProUGUI countdownText;

    void Awake()
    {
        Instance = this;
    }

    public void UpdateCountdownText(int time)
    {
        countdownText.gameObject.SetActive(true);
        countdownText.text = time.ToString();
    }
    public void HideCountdown()
    {
        countdownText.gameObject.SetActive(false);
    }

}
