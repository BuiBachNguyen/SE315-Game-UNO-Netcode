using Unity.Services.Authentication;
using UnityEngine;

public class GameBootstrap : MonoBehaviour
{
    private async void Start()
    {
        await UnityServiceInit.EnsureInitializedAsync();

        Debug.Log(
            AuthenticationService.Instance.PlayerId);
    }
}
