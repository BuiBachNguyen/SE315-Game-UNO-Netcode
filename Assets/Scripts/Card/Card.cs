using UnityEngine;
using System.Collections.Generic;

namespace CardSystem
{
    [CreateAssetMenu(fileName = "New Card", menuName = "CardSystem/Card")]
    public class Card : ScriptableObject
    {
        [SerializeField] private CardColor color;
        [SerializeField] private CardType type;
        [SerializeField] private int number;
        [SerializeField] private List<ActionType> actionTypes;
        [SerializeField] private int drawAmount;
        [SerializeField] private Sprite cardSprite;

        public Sprite GetCardSprite() => cardSprite;
        public CardColor GetColor() => color;
        public CardType GetCardType() => type;
        public int GetNumber() => number;
        public List<ActionType> GetActionTypes() => actionTypes;
        public int GetDrawAmount() => drawAmount;
    }
}
