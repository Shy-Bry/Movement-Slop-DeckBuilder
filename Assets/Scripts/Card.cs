using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Card", menuName = "Card")]
public class Card : ScriptableObject
{
    public string cardName;

    public List<CardType> cardsType;

    public CardFlavor cardFlavor;

    public enum CardFlavor
    {
        Fire,
        Ice,
        Plant,
        Star,
        Earth,
        Pure,
        Void
    }

    public enum CardType
    {
        Movement,
        Attack,
        Effect,
        Curse,
        Utility
    }
}