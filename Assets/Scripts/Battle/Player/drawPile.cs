using System.Collections.Generic;
using ElementalSpire.Cards;
using UnityEngine;

/// <summary>
/// 抽牌堆 - 挂载在 Player 对象上，管理战斗中的抽牌堆实例。
/// </summary>
public class drawPile : MonoBehaviour
{
    private List<CardInstance> _cards = new List<CardInstance>();
    private static System.Random _rng = new System.Random();

    public int Count => _cards.Count;
    public bool IsEmpty => _cards.Count == 0;
    public IReadOnlyList<CardInstance> Cards => _cards;

    public void Initialize(IEnumerable<CardInstance> cards)
    {
        _cards = new List<CardInstance>(cards);
    }

    public void Shuffle()
    {
        var rng = _rng;
        int n = _cards.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            CardInstance temp = _cards[k];
            _cards[k] = _cards[n];
            _cards[n] = temp;
        }
    }

    public CardInstance PeekTop()
    {
        return _cards.Count > 0 ? _cards[0] : null;
    }

    public CardInstance DrawTop()
    {
        if (_cards.Count == 0) return null;
        CardInstance card = _cards[0];
        _cards.RemoveAt(0);
        return card;
    }

    public void AddCards(IEnumerable<CardInstance> cards)
    {
        _cards.AddRange(cards);
    }

    public void Clear()
    {
        _cards.Clear();
    }
}
