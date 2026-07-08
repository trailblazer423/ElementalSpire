using System.Collections.Generic;
using ElementalSpire.Cards;
using UnityEngine;

/// <summary>
/// 手牌 - 挂载在 Player 对象上，管理玩家手牌实例。
/// </summary>
public class handCards : MonoBehaviour
{
    private List<CardInstance> _cards = new List<CardInstance>();

    public int Count => _cards.Count;
    public IReadOnlyList<CardInstance> Cards => _cards;

    public void AddCard(CardInstance card)
    {
        if (card != null)
            _cards.Add(card);
    }

    public bool RemoveCard(CardInstance card)
    {
        return _cards.Remove(card);
    }

    public bool Contains(CardInstance card)
    {
        return _cards.Contains(card);
    }

    public void Clear()
    {
        _cards.Clear();
    }

    public List<CardInstance> GetAll()
    {
        return new List<CardInstance>(_cards);
    }
}
