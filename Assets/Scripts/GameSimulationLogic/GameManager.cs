using UnityEngine;

public class GameManager : MonoBehaviour
{
    private static GameManager __instance;
    [SerializeField] private TurnManager turnManager;
    [SerializeField] private Deck deck;
    [SerializeField] private PlayedZone playedZone;
    public static GameManager Instance
    {
        get
        {
            if (__instance == null)
            {
                __instance = FindAnyObjectByType<GameManager>();
                if (__instance == null)
                {
                    GameObject obj = new GameObject("GameManager");
                    __instance = obj.AddComponent<GameManager>();
                }
                DontDestroyOnLoad(__instance.gameObject);
            }
            return __instance;
        }
    }

    public void Awake()
    {
        if (playedZone == null)
        {
            Debug.LogError("PlayedZone reference is not set in GameManager!");
        }
        if (deck == null)
        {
            Debug.LogError("Deck reference is not set in GameManager!");
        }
        if (turnManager == null)
        {
            Debug.LogError("TurnManager reference is not set in GameManager!");
        }
    }

    public TurnManager GetTurnManager()
    {
        return turnManager;
    }

    public Deck GetDeck()
    {
        return deck;
    }

    public PlayedZone GetPlayedZone()
    {
        return playedZone;
    }
}
