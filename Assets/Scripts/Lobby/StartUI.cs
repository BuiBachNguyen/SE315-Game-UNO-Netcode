using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Services.Core;
using Unity.Services.Authentication;

public class StartUI : MonoBehaviour
{
    [SerializeField] TMP_InputField nameInput;

    async void Awake()
    {
        await UnityServices.InitializeAsync();

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance
                .SignInAnonymouslyAsync();
        }

        Debug.Log("Signed in: " +
            AuthenticationService.Instance.PlayerId);
    }

    public void OnStartClick()
    {
        PlayerData.Instance.PlayerName = nameInput.text;
        SceneManager.LoadScene("LobbyScene");
    }
}
