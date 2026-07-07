using System.Collections.Generic;
using System.Linq;
using ElementalSpire.Cards;

/// <summary>
/// 牌组管理器 - 操作 Player 上的 drawPile / handCards / discardPile 组件
/// </summary>
public class DeckManager
{
    private drawPile _drawPile;
    private handCards _handCards;
    private discardPile _discardPile;
    private List<CardData> _exhaustPile = new List<CardData>();

    public drawPile DrawPileComponent => _drawPile;
    public handCards HandCardsComponent => _handCards;
    public discardPile DiscardPileComponent => _discardPile;

    public IReadOnlyList<CardData> handCards => _handCards.Cards;
    public IReadOnlyList<CardData> drawPile => _drawPile.Cards;
    public IReadOnlyList<CardData> discardPile => _discardPile.Cards;
    public IReadOnlyList<CardData> exhaustPile => _exhaustPile;

    public int DrawPileCount => _drawPile.Count;
    public int HandCount => _handCards.Count;
    public int DiscardPileCount => _discardPile.Count;
    public int ExhaustPileCount => _exhaustPile.Count;

    public DeckManager(drawPile drawPile, handCards handCards, discardPile discardPile)
    {
        _drawPile = drawPile;
        _handCards = handCards;
        _discardPile = discardPile;
    }

    /// <summary>
    /// 根据预设初始化牌组
    /// </summary>
    public void Initialize(DeckPreset preset)
    {
        var cards = CardDeckLibrary.GetCardsByDeckPreset(preset).ToList();
        _drawPile.Initialize(cards);
        _handCards.Clear();
        _discardPile.Clear();
        _exhaustPile.Clear();
        _drawPile.Shuffle();
    }

    /// <summary>
    /// 抽指定数量的牌
    /// </summary>
    public List<CardData> DrawCards(int count)
    {
        var drawn = new List<CardData>();
        for (int i = 0; i < count; i++)
        {
            if (_drawPile.IsEmpty)
                ReshuffleDiscardPile();

            if (_drawPile.IsEmpty)
                break;

            CardData card = _drawPile.DrawTop();
            _handCards.AddCard(card);
            drawn.Add(card);
        }
        return drawn;
    }

    /// <summary>
    /// 打出一张牌（从手牌移至弃牌堆或消耗区）
    /// </summary>
    public void PlayCard(CardData card)
    {
        if (!_handCards.Contains(card)) return;
        _handCards.RemoveCard(card);

        if (card.exhaust)
        {
            _exhaustPile.Add(card);
        }
        else
        {
            _discardPile.AddCard(card);
        }
    }

    /// <summary>
    /// 弃掉手牌中的指定牌
    /// </summary>
    public void DiscardCard(CardData card)
    {
        if (!_handCards.Contains(card)) return;
        _handCards.RemoveCard(card);
        _discardPile.AddCard(card);
    }

    /// <summary>
    /// 弃掉全部手牌
    /// </summary>
    public void DiscardAllHand()
    {
        var all = _handCards.GetAll();
        _discardPile.AddRange(all);
        _handCards.Clear();
    }

    /// <summary>
    /// 将弃牌堆洗回抽牌堆
    /// </summary>
    public void ReshuffleDiscardPile()
    {
        if (_discardPile.Count == 0) return;
        var cards = _discardPile.TakeAll();
        _drawPile.AddCards(cards);
        _drawPile.Shuffle();
    }
}
