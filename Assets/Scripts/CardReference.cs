using System.Collections.Generic;
using UnityEngine;

public class CardReference : MonoBehaviour
{
    [SerializeField] private List<Card> cards;
    [SerializeField] private List<ReferencePair> cardDictionary;

    private static CardReference __instance;
    public static CardReference Instance
    {
        get
        {
            if (__instance == null)
            {
                __instance = FindAnyObjectByType<CardReference>();
                if (__instance == null)
                {
                    Debug.LogError("No CardReference instance found in the scene.");
                }
                DontDestroyOnLoad(__instance.gameObject);
            }
            return __instance;
        }
    }
    private void OnValidate()
    {
        cardDictionary = new List<ReferencePair>();
        foreach (var card in cards)
        {
            cardDictionary.Add(new ReferencePair(card));
        }
    }

    public Card GetCardByName(string name)
    {
        name=name.Normalize();
        foreach (var pair in cardDictionary)
        {
            if (pair.name == name)
            {
                return pair.card;
            }
        }
        Debug.LogWarning($"Card with name {name} not found.");
        return null;
    }
}

[System.Serializable]
public struct ReferencePair
{
    public string name;
    public Card card;

    public ReferencePair(Card card)
    {
        this.name = card.name.Normalize();
        this.card = card;
    }
}