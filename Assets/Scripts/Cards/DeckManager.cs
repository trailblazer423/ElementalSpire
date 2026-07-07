using System.Collections.Generic;
using System.Linq;
using ElementalSpire.Cards;

/// <summary>
/// 牌组管理器 - 操作 Player 上的 drawPile / handCards / discardPile 组件。
/// </summary>
public class DeckManager
{
    private drawPile _drawPile;
    private handCards _handCards;
    private discardPile _discardPile;
    private List<CardData> _exhaustPile = new List<CardData>();
    private List<CardData> _powerPile = new List<CardData>();

    public drawPile DrawPileComponent => _drawPile;
    public handCards HandCardsComponent => _handCards;
    public discardPile DiscardPileComponent => _discardPile;

    public IReadOnlyList<CardData> handCards => _handCards.Cards;
    public IReadOnlyList<CardData> drawPile => _drawPile.Cards;
    public IReadOnlyList<CardData> discardPile => _discardPile.Cards;
    public IReadOnlyList<CardData> exhaustPile => _exhaustPile;
    public IReadOnlyList<CardData> powerPile => _powerPile;

    public int DrawPileCount => _drawPile.Count;
    public int HandCount => _handCards.Count;
    public int DiscardPileCount => _discardPile.Count;
    public int ExhaustPileCount => _exhaustPile.Count;
    public int PowerPileCount => _powerPile.Count;

    public DeckManager(drawPile drawPile, handCards handCards, discardPile discardPile)
    {
        _drawPile = drawPile;
        _handCards = handCards;
        _discardPile = discardPile;
    }

    public void Initialize(DeckPreset preset)
    {
        Initialize(CardDeckLibrary.GetCardsByDeckPreset(preset), true);
    }

    public void Initialize(IEnumerable<CardData> cards)
    {
        Initialize(cards, true);
    }

    public void Initialize(IEnumerable<CardData> cards, bool shuffle)
    {
        _drawPile.Initialize(cards);
        _handCards.Clear();
        _discardPile.Clear();
        _exhaustPile.Clear();
        _powerPile.Clear();
        if (shuffle)
            _drawPile.Shuffle();
    }

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

    public void AddCardToHand(CardData card)
    {
        _handCards.AddCard(card);
    }

    public bool RemoveFromHand(CardData card)
    {
        return _handCards.RemoveCard(card);
    }

    public void PlayCard(CardData card)
    {
        if (!_handCards.Contains(card)) return;
        _handCards.RemoveCard(card);
        MoveResolvedCardAfterPlay(card);
    }

    public void MoveResolvedCardAfterPlay(CardData card)
    {
        if (card == null) return;

        if (card.HasCardType(CardType.Power))
        {
            _powerPile.Add(card);
        }
        else if (card.exhaust)
        {
            _exhaustPile.Add(card);
        }
        else
        {
            _discardPile.AddCard(card);
        }
    }

    public void DiscardCard(CardData card)
    {
        if (!_handCards.Contains(card)) return;
        _handCards.RemoveCard(card);
        _discardPile.AddCard(card);
    }

    public void DiscardAllHand()
    {
        var all = _handCards.GetAll();
        _discardPile.AddRange(all);
        _handCards.Clear();
    }

    public void ReshuffleDiscardPile()
    {
        if (_discardPile.Count == 0) return;
        var cards = _discardPile.TakeAll();
        _drawPile.AddCards(cards);
        _drawPile.Shuffle();
    }

    public IEnumerable<CardData> GetAllCombatCards()
    {
        return _drawPile.Cards
            .Concat(_handCards.Cards)
            .Concat(_discardPile.Cards)
            .Concat(_exhaustPile)
            .Concat(_powerPile);
    }
}

