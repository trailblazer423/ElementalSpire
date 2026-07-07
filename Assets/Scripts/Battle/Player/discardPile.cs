using System.Collections.Generic;
using ElementalSpire.Cards;
using UnityEngine;

/// <summary>
/// 弃牌堆 - 挂载在 Player 对象上，管理弃牌堆中的卡牌
/// </summary>
public class discardPile : MonoBehaviour
{
    private List<CardData> _cards = new List<CardData>();

    public int Count => _cards.Count;
    public IReadOnlyList<CardData> Cards => _cards;

    /// <summary>
    /// 将一张牌加入弃牌堆
    /// </summary>
    public void AddCard(CardData card)
    {
        if (card != null)
            _cards.Add(card);
    }

    /// <summary>
    /// 批量加入卡牌
    /// </summary>
    public void AddRange(IEnumerable<CardData> cards)
    {
        _cards.AddRange(cards);
    }

    /// <summary>
    /// 获取并清空弃牌堆（用于洗回抽牌堆）
    /// </summary>
    public List<CardData> TakeAll()
    {
        var all = new List<CardData>(_cards);
        _cards.Clear();
        return all;
    }

    /// <summary>
    /// 清空弃牌堆
    /// </summary>
    public void Clear()
    {
        _cards.Clear();
    }
}
