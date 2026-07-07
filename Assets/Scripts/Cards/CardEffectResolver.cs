using ElementalSpire.Cards;
using UnityEngine;

/// <summary>
/// 卡牌效果解析器 - 根据 cardId 执行具体卡牌效果
/// </summary>
public class CardEffectResolver
{
    private BattleManager _battleManager;
    private DeckManager _deckManager;
    private currentEnergy _playerEnergy;
    private playerBlock _playerBlock;
    private playerHP _playerHP;

    public CardEffectResolver(
        BattleManager battleManager,
        DeckManager deckManager,
        currentEnergy playerEnergy,
        playerBlock playerBlock,
        playerHP playerHP)
    {
        _battleManager = battleManager;
        _deckManager = deckManager;
        _playerEnergy = playerEnergy;
        _playerBlock = playerBlock;
        _playerHP = playerHP;
    }

    /// <summary>
    /// 解析并执行卡牌效果
    /// </summary>
    public void ResolveEffect(CardData card)
    {
        Debug.Log($"[CardEffect] {card.cardName}");

        switch (card.cardId)
        {
            // ========== 火元素 ==========
            case "fire_sacrifice":
                _playerHP?.TakeDamage(6);
                _playerEnergy?.AddEnergy(2);
                DrawCards(3);
                break;

            case "fire_hilt_strike":
                DealAttackDamage(9, 1, ElementType.Fire);
                DrawCards(1);
                break;

            case "fire_perfect_strike":
                PerfectStrike();
                break;

            case "fire_double_strike":
                DealAttackDamage(5, 2, ElementType.Fire);
                break;

            case "fire_bloodletting":
                _playerHP?.TakeDamage(3);
                break;

            case "fire_blood_wall":
                _playerHP?.TakeDamage(4);
                GainBlock(12);
                break;

            // ========== 毒元素 ==========
            case "poison_blade":
                DealAttackDamage(5, 1, ElementType.Poison);
                break;

            case "poison_sneak_needle":
                DealAttackDamage(6, 1, ElementType.Poison);
                break;

            case "poison_prepare":
                DrawCards(1);
                DiscardCards(1);
                break;

            case "poison_roll":
                GainBlock(7);
                DrawCards(1);
                DiscardCards(1);
                break;

            case "poison_fog_guard":
                GainBlock(6);
                break;

            case "poison_acrobatics":
                DrawCards(3);
                DiscardCards(1);
                break;

            case "poison_smoke_bomb":
                GainBlock(6);
                break;

            // ========== 水元素 ==========
            case "water_blade":
                DealAttackDamage(7, 1, ElementType.Water);
                break;

            case "water_surge":
                DealAttackDamage(9, 1, ElementType.Water);
                DrawCards(1);
                break;

            case "water_curtain":
                GainBlock(7);
                break;

            case "water_ebb":
                GainBlock(8);
                break;

            case "water_gather":
                DrawCards(1);
                break;

            case "water_wave_guard":
                GainBlock(12);
                break;

            case "water_spring":
                DrawCards(3);
                break;

            // ========== 无色 ==========
            case "color_blank_strike":
                DealAttackDamage(8, 1, ElementType.Colorless);
                break;

            case "color_emergency_shield":
                GainBlock(8);
                break;

            case "color_tactical_sort":
                DrawCards(1);
                DiscardCards(1);
                break;

            case "color_neutral_arrow":
                DealAttackDamage(6, 1, ElementType.Colorless);
                break;

            default:
                DefaultResolve(card);
                break;
        }
    }

    // ==========================================
    //  基础效果方法
    // ==========================================

    /// <summary>
    /// 造成攻击伤害（支持多段攻击）
    /// </summary>
    private void DealAttackDamage(int damage, int hitCount, ElementType elementType)
    {
        for (int i = 0; i < hitCount; i++)
        {
            _battleManager?.DealDamageToEnemy(damage);
        }
    }

    /// <summary>
    /// 获得格挡
    /// </summary>
    private void GainBlock(int amount)
    {
        if (_playerBlock != null)
            _playerBlock.AddBlock(amount);
    }

    /// <summary>
    /// 抽牌
    /// </summary>
    private void DrawCards(int count)
    {
        _deckManager?.DrawCards(count);
    }

    /// <summary>
    /// 从手牌末尾弃牌
    /// </summary>
    private void DiscardCards(int count)
    {
        for (int i = 0; i < count && _deckManager.handCards.Count > 0; i++)
        {
            var card = _deckManager.handCards[_deckManager.handCards.Count - 1];
            _deckManager.DiscardCard(card);
        }
    }

    // ==========================================
    //  特殊卡牌效果
    // ==========================================

    private void PerfectStrike()
    {
        int damage = 6;
        int strikeCount = 0;

        foreach (var c in _deckManager.drawPile)
            if (c.cardName.Contains("打击")) strikeCount++;
        foreach (var c in _deckManager.handCards)
            if (c.cardName.Contains("打击")) strikeCount++;
        foreach (var c in _deckManager.discardPile)
            if (c.cardName.Contains("打击")) strikeCount++;

        damage += strikeCount * 2;
        _battleManager?.DealDamageToEnemy(damage);
    }

    private void DefaultResolve(CardData card)
    {
        if (card.HasCardType(CardType.Attack))
            DealAttackDamage(6, 1, card.elementType);
        if (card.HasCardType(CardType.Defense))
            GainBlock(6);
    }
}
