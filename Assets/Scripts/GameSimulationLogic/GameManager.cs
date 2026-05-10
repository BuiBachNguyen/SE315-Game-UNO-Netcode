using UnityEngine;

public class CardSystemManager : MonoBehaviour
{
    private static CardSystemManager __instance;
    [SerializeField] private TurnManager turnManager;
    [SerializeField] private Deck deck;
    [SerializeField] private PlayedZone playedZone;
    [SerializeField] private CardHolder cardHolder;

    public static CardSystemManager Instance
    {
        get
        {
            if (__instance == null)
            {
                __instance = FindAnyObjectByType<CardSystemManager>();
                if (__instance == null)
                {
                    GameObject obj = new GameObject("CardSystemManager");
                    __instance = obj.AddComponent<CardSystemManager>();
                }
                DontDestroyOnLoad(__instance.gameObject);
            }
            return __instance;
        }
    }

    public void Awake()
    {
        if (playedZone == null)
            Debug.LogError("PlayedZone reference is not set in CardSystemManager!");
        if (deck == null)
            Debug.LogError("Deck reference is not set in CardSystemManager!");
        if (turnManager == null)
            Debug.LogError("TurnManager reference is not set in CardSystemManager!");
        if (cardHolder == null)
            Debug.LogError("CardHolder reference is not set in CardSystemManager!");
    }

    public TurnManager GetTurnManager() => turnManager;
    public Deck GetDeck() => deck;
    public PlayedZone GetPlayedZone() => playedZone;
    public CardHolder GetCardHolder() => cardHolder;
}
