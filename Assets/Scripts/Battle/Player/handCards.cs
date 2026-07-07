using System.Collections.Generic;
using ElementalSpire.Cards;
using UnityEngine;

/// <summary>
/// 手牌 - 挂载在 Player 对象上，管理玩家手牌
/// </summary>
public class handCards : MonoBehaviour
{
    private List<CardData> _cards = new List<CardData>();

    public int Count => _cards.Count;
    public IReadOnlyList<CardData> Cards => _cards;

    /// <summary>
    /// 将一张牌加入手牌（抽牌）
    /// </summary>
    public void AddCard(CardData card)
    {
        if (card != null)
            _cards.Add(card);
    }

    /// <summary>
    /// 从手牌移除一张牌（打出或弃牌）
    /// </summary>
    public bool RemoveCard(CardData card)
    {
        return _cards.Remove(card);
    }

    /// <summary>
    /// 检查手牌是否包含指定卡牌
    /// </summary>
    public bool Contains(CardData card)
    {
        return _cards.Contains(card);
    }

    /// <summary>
    /// 清空手牌
    /// </summary>
    public void Clear()
    {
        _cards.Clear();
    }

    /// <summary>
    /// 获取手牌中所有卡牌（用于遍历）
    /// </summary>
    public List<CardData> GetAll()
    {
        return new List<CardData>(_cards);
    }
}
