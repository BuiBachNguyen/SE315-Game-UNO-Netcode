using UnityEngine;

public class InitDebug : MonoBehaviour
{
    private void Start()
    {
        if (FindObjectsByType<GameManager>(FindObjectsSortMode.None).Length != 1)
        {
            Debug.LogError("There should be exactly one GameManager in the scene. Please check your setup. Current count: " + FindObjectsByType<GameManager>(FindObjectsSortMode.None).Length);
        }
    }
}
