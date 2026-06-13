using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using UnityEngine;
#if UNITY_EDITOR
using Unity.Multiplayer.Playmode;
#endif

public class UnityServiceInit : MonoBehaviour
{
    static Task initializationTask;
    static string hostedLobbyId;
    static bool heartbeatRunning;

    async void Awake()
    {
        DontDestroyOnLoad(gameObject);

        try
        {
            await EnsureInitializedAsync();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
    }

    public static Task EnsureInitializedAsync()
    {
        if (initializationTask == null)
            initializationTask = InitializeAsync();

        return initializationTask;
    }

    static async Task InitializeAsync()
    {
        if (UnityServices.State == ServicesInitializationState.Uninitialized)
        {
            var options = new InitializationOptions();

#if UNITY_EDITOR
            var tags = CurrentPlayer.ReadOnlyTags();
            options.SetProfile(tags.Length > 0 ? tags[0] : CreateEditorProfile());
#endif

            await UnityServices.InitializeAsync(options);
        }

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            try
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }
            catch (AuthenticationException exception)
                when (exception.ErrorCode == AuthenticationErrorCodes.ClientInvalidUserState)
            {
                while (!AuthenticationService.Instance.IsSignedIn)
                    await Task.Delay(50);
            }
        }

        Debug.Log("Signed in: " + AuthenticationService.Instance.PlayerId);
    }

#if UNITY_EDITOR
    static string CreateEditorProfile()
    {
        // Every MPPM clone has a distinct data path, even without configured player tags.
        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(Application.dataPath));
            return "mppm-" + BitConverter.ToString(hash, 0, 8).Replace("-", "").ToLowerInvariant();
        }
    }
#endif

    public static void StartLobbyHeartbeat(string lobbyId)
    {
        hostedLobbyId = lobbyId;

        if (!heartbeatRunning)
            _ = RunHeartbeatAsync();
    }

    static async Task RunHeartbeatAsync()
    {
        heartbeatRunning = true;

        try
        {
            while (!string.IsNullOrEmpty(hostedLobbyId))
            {
                await Task.Delay(15000);

                if (!string.IsNullOrEmpty(hostedLobbyId))
                    await LobbyService.Instance.SendHeartbeatPingAsync(hostedLobbyId);
            }
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
        finally
        {
            heartbeatRunning = false;
        }
    }
}
