using UnityEngine;

public class PlayerData : MonoBehaviour
{
    public static PlayerData Instance;
    public string PlayerName;

    void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
}
