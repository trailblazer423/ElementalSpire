using System.Collections.Generic;
using ElementalSpire.Cards;
using UnityEngine;

/// <summary>
/// 弃牌堆 - 挂载在 Player 对象上，管理战斗中的弃牌堆实例。
/// </summary>
public class discardPile : MonoBehaviour
{
    private List<CardInstance> _cards = new List<CardInstance>();

    public int Count => _cards.Count;
    public IReadOnlyList<CardInstance> Cards => _cards;

    public void AddCard(CardInstance card)
    {
        if (card != null)
            _cards.Add(card);
    }

    public void AddRange(IEnumerable<CardInstance> cards)
    {
        _cards.AddRange(cards);
    }

    public List<CardInstance> TakeAll()
    {
        var all = new List<CardInstance>(_cards);
        _cards.Clear();
        return all;
    }

    public void Clear()
    {
        _cards.Clear();
    }
}
