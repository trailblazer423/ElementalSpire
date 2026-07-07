using System.Collections.Generic;
using ElementalSpire.Cards;
using UnityEngine;

/// <summary>
/// 抽牌堆 - 挂载在 Player 对象上，管理抽牌堆中的卡牌
/// </summary>
public class drawPile : MonoBehaviour
{
    private List<CardData> _cards = new List<CardData>();

    public int Count => _cards.Count;
    public bool IsEmpty => _cards.Count == 0;
    public IReadOnlyList<CardData> Cards => _cards;

    /// <summary>
    /// 初始化抽牌堆（用预设牌组填充）
    /// </summary>
    public void Initialize(IEnumerable<CardData> cards)
    {
        _cards = new List<CardData>(cards);
    }

    /// <summary>
    /// 洗牌
    /// </summary>
    public void Shuffle()
    {
        System.Random rng = new System.Random();
        int n = _cards.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            var temp = _cards[k];
            _cards[k] = _cards[n];
            _cards[n] = temp;
        }
    }

    /// <summary>
    /// 从牌堆顶抽一张牌（不移出，只查看）
    /// </summary>
    public CardData PeekTop()
    {
        return _cards.Count > 0 ? _cards[0] : null;
    }

    /// <summary>
    /// 从牌堆顶抽一张牌并移除
    /// </summary>
    public CardData DrawTop()
    {
        if (_cards.Count == 0) return null;
        CardData card = _cards[0];
        _cards.RemoveAt(0);
        return card;
    }

    /// <summary>
    /// 将卡牌加入牌堆（用于从弃牌堆洗回）
    /// </summary>
    public void AddCards(IEnumerable<CardData> cards)
    {
        _cards.AddRange(cards);
    }

    /// <summary>
    /// 清空抽牌堆
    /// </summary>
    public void Clear()
    {
        _cards.Clear();
    }
}
