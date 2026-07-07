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
    private PlayerState _playerState;
    private EnemyState _enemyState;
    private bool _corrosiveWaveActive = false;
    private bool _toxicCloudActive = false;

    public CardEffectResolver(
        BattleManager battleManager,
        DeckManager deckManager,
        currentEnergy playerEnergy,
        playerBlock playerBlock,
        playerHP playerHP,
        PlayerState playerState)
    {
        _battleManager = battleManager;
        _deckManager = deckManager;
        _playerEnergy = playerEnergy;
        _playerBlock = playerBlock;
        _playerHP = playerHP;
        _playerState = playerState;

        // 腐蚀波：回合结束时清除效果
        if (_battleManager?.TurnManager != null)
        {
            _battleManager.TurnManager.OnDiscardPhase += ClearCorrosiveWave;
            _battleManager.TurnManager.OnEnemyActionStarted += OnToxicCloudTrigger;
        }
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
                DealAttackDamage(9, 1);
                DrawCards(1);
                break;

            case "fire_demon_form":
                // 回合开始时获得2点力量（简化：立即获得）
                AddPower(2);
                break;

            case "fire_perfect_strike":
                PerfectStrike();
                break;

            case "fire_double_strike":
                DealAttackDamage(5, 2);
                break;

            case "fire_bloodletting":
                _playerHP?.TakeDamage(3);
                AddPower(1);
                break;

            case "fire_blood_wall":
                _playerHP?.TakeDamage(4);
                GainBlock(12);
                break;

            // ========== 毒元素 ==========
            case "poison_blade":
                DealAttackDamage(5, 1);
                ApplyPoison(4);
                break;

            case "poison_sneak_needle":
                DealAttackDamage(6, 1);
                ApplyPoison(3);
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
                {
                    var es = _battleManager?.EnemyState;
                    if (es != null && es.poison > 0)
                        GainBlock(4);
                }
                break;

            case "poison_acrobatics":
                DrawCards(3);
                DiscardCards(1);
                break;

            case "poison_smoke_bomb":
                ApplyPoison(3);
                GainBlock(6);
                break;

            case "poison_corrosive_wave":
                ActivateCorrosiveWave();
                break;

            case "poison_toxic_cloud":
                ActivateToxicCloud();
                break;

            case "poison_bouncing_flask":
                ApplyPoison(3);
                ApplyPoison(3);
                ApplyPoison(3);
                break;

            // ========== 水元素 ==========
            case "water_blade":
                DealAttackDamage(7, 1);
                break;

            case "water_surge":
                DealAttackDamage(9, 1);
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
                DealAttackDamage(8, 1);
                break;

            case "color_emergency_shield":
                GainBlock(8);
                break;

            case "color_tactical_sort":
                DrawCards(1);
                DiscardCards(1);
                break;

            case "color_neutral_arrow":
                DealAttackDamage(6, 1);
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
    /// 造成攻击伤害（每段伤害 = 基础伤害 + 力量，支持多段攻击）
    /// </summary>
    private void DealAttackDamage(int baseDamage, int hitCount)
    {
        int powerBonus = _playerState != null ? _playerState.power : 0;
        int finalDamage = baseDamage + powerBonus;

        for (int i = 0; i < hitCount; i++)
        {
            _battleManager?.DealDamageToEnemy(finalDamage);
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
        var drawn = _deckManager?.DrawCards(count);

        // 腐蚀波：本回合内每抽一张牌，给予敌人2层中毒
        if (_corrosiveWaveActive && drawn != null && drawn.Count > 0)
        {
            var enemyState = _battleManager?.EnemyState;
            if (enemyState != null)
            {
                enemyState.AddPoison(drawn.Count * 2);
                Debug.Log($"[CardEffect] 腐蚀波：抽{drawn.Count}张牌，给予{drawn.Count * 2}层中毒，当前中毒: {enemyState.poison}");
            }
        }
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

    /// <summary>
    /// 施加中毒
    /// </summary>
    private void ApplyPoison(int amount)
    {
        var enemyState = _battleManager?.EnemyState;
        if (enemyState == null) return;

        enemyState.AddPoison(amount);
        Debug.Log($"[CardEffect] 施加中毒 {amount} 层，当前中毒: {enemyState.poison}");
    }

    /// <summary>
    /// 增加力量
    /// </summary>
    private void AddPower(int amount)
    {
        if (_playerState != null)
        {
            _playerState.AddPower(amount);
            Debug.Log($"[CardEffect] 获得 {amount} 点力量，当前力量: {_playerState.power}");
        }
    }

    // ==========================================
    //  特殊卡牌效果
    // ==========================================

    // ==========================================
    //  腐蚀波
    // ==========================================

    private void ActivateCorrosiveWave()
    {
        _corrosiveWaveActive = true;
        Debug.Log("[CardEffect] 腐蚀波激活：本回合内每抽一张牌给予敌人2层中毒");
    }

    private void ClearCorrosiveWave()
    {
        if (!_corrosiveWaveActive) return;
        _corrosiveWaveActive = false;
        Debug.Log("[CardEffect] 腐蚀波效果已清除（回合结束）");
    }

    // ==========================================
    //  毒云弥漫
    // ==========================================

    private void ActivateToxicCloud()
    {
        _toxicCloudActive = true;
        Debug.Log("[CardEffect] 毒云弥漫激活：每次敌人行动前给予3层中毒");
    }

    private void OnToxicCloudTrigger()
    {
        if (!_toxicCloudActive) return;

        var enemyState = _battleManager?.EnemyState;
        if (enemyState != null)
        {
            enemyState.AddPoison(3);
            Debug.Log($"[CardEffect] 毒云弥漫：敌人行动前给予3层中毒，当前中毒: {enemyState.poison}");
        }
    }

    private void PerfectStrike()
    {
        int powerBonus = _playerState != null ? _playerState.power : 0;
        int damage = 6 + powerBonus;
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
            DealAttackDamage(6, 1);
        if (card.HasCardType(CardType.Defense))
            GainBlock(6);
    }
}
