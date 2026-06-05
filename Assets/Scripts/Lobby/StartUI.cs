using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StartUI : MonoBehaviour
{
    [SerializeField] TMP_InputField nameInput;

    public void OnStartClick()
    {
        PlayerData.Instance.PlayerName = nameInput.text;
        SceneManager.LoadScene("LobbyScene");
    }
}
