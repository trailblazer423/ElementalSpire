using System.Collections.Generic;
using System.Linq;
using ElementalSpire.Cards;

/// <summary>
/// 牌组管理器 - 操作 Player 上的 drawPile / handCards / discardPile 组件。
/// CardData 是静态卡表，CardInstance 才是战斗中的具体牌。
/// </summary>
public class DeckManager
{
    private readonly drawPile _drawPile;
    private readonly handCards _handCards;
    private readonly discardPile _discardPile;
    private readonly List<CardInstance> _exhaustPile = new List<CardInstance>();
    private readonly List<CardInstance> _powerPile = new List<CardInstance>();

    public drawPile DrawPileComponent => _drawPile;
    public handCards HandCardsComponent => _handCards;
    public discardPile DiscardPileComponent => _discardPile;

    public IReadOnlyList<CardInstance> handCards => _handCards.Cards;
    public IReadOnlyList<CardInstance> drawPile => _drawPile.Cards;
    public IReadOnlyList<CardInstance> discardPile => _discardPile.Cards;
    public IReadOnlyList<CardInstance> exhaustPile => _exhaustPile;
    public IReadOnlyList<CardInstance> powerPile => _powerPile;

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
        IEnumerable<CardInstance> instances = cards
            .Where(card => card != null)
            .Select(card => new CardInstance(card.cardId));
        Initialize(instances, shuffle);
    }

    public void Initialize(IEnumerable<CardInstance> cards, bool shuffle)
    {
        _drawPile.Initialize(cards.Where(card => card != null));
        _handCards.Clear();
        _discardPile.Clear();
        _exhaustPile.Clear();
        _powerPile.Clear();
        if (shuffle)
            _drawPile.Shuffle();
    }

    public List<CardInstance> DrawCards(int count)
    {
        var drawn = new List<CardInstance>();
        for (int i = 0; i < count; i++)
        {
            if (_drawPile.IsEmpty)
                ReshuffleDiscardPile();

            if (_drawPile.IsEmpty)
                break;

            CardInstance card = _drawPile.DrawTop();
            _handCards.AddCard(card);
            drawn.Add(card);
        }
        return drawn;
    }

    public void AddCardToHand(CardData card)
    {
        if (card != null)
            AddCardToHand(new CardInstance(card.cardId));
    }

    public void AddCardToHand(CardInstance card)
    {
        _handCards.AddCard(card);
    }

    public bool RemoveFromHand(CardInstance card)
    {
        return _handCards.RemoveCard(card);
    }

    public void PlayCard(CardInstance card)
    {
        if (!_handCards.Contains(card)) return;
        _handCards.RemoveCard(card);
        MoveResolvedCardAfterPlay(card);
    }

    public void MoveResolvedCardAfterPlay(CardInstance card)
    {
        if (card == null) return;

        CardData data = card.GetCardData();
        if (data == null) return;

        card.ClearTemporaryCostModifiers();

        if (data.HasCardType(CardType.Power))
        {
            _powerPile.Add(card);
        }
        else if (data.exhaust)
        {
            _exhaustPile.Add(card);
        }
        else
        {
            _discardPile.AddCard(card);
        }
    }

    public void DiscardCard(CardInstance card)
    {
        if (!_handCards.Contains(card)) return;
        _handCards.RemoveCard(card);
        card.ClearTemporaryCostModifiers();
        _discardPile.AddCard(card);
    }

    public void DiscardAllHand()
    {
        var all = _handCards.GetAll();
        foreach (CardInstance card in all)
            card.ClearTemporaryCostModifiers();
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

    public IEnumerable<CardInstance> GetAllCombatCards()
    {
        return _drawPile.Cards
            .Concat(_handCards.Cards)
            .Concat(_discardPile.Cards)
            .Concat(_exhaustPile)
            .Concat(_powerPile);
    }
}

