using Unity.Services.Core;
using Unity.Services.Authentication;
using UnityEngine;
#if UNITY_EDITOR
using Unity.Multiplayer.Playmode;
#endif

public class UnityServiceInit : MonoBehaviour
{
    async void Awake()
    {
        if (UnityServices.State == ServicesInitializationState.Uninitialized)
        {
            var options = new InitializationOptions();

#if UNITY_EDITOR
            // Mỗi virtual player trong MPPM cần profile riêng để auth không xung đột
            // Cần gán tag trong cửa sổ Multiplayer Play Mode (Window → Multiplayer → Multiplayer Play Mode)
            var tags = CurrentPlayer.ReadOnlyTags();
            options.SetProfile(tags.Length > 0 ? tags[0] : "DefaultPlayer");
#endif

            await UnityServices.InitializeAsync(options);
        }

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            try
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }
            catch (AuthenticationException ex) when (ex.ErrorCode == AuthenticationErrorCodes.ClientInvalidUserState)
            {
                // Already signing in from another Awake call — wait for it to finish
                while (!AuthenticationService.Instance.IsSignedIn)
                    await System.Threading.Tasks.Task.Delay(50);
            }
        }

        Debug.Log("Signed in: " + AuthenticationService.Instance.PlayerId);
    }
}
