using System;
using System.Collections.Generic;
using System.Linq;
using ElementalSpire.Cards;
using UnityEngine;

/// <summary>
/// 卡牌效果解析器 - 根据 cardId 执行具体卡牌效果，元素反应交给 ElementReactionResolver。
/// </summary>
public class CardEffectResolver
{
    private readonly BattleManager battleManager;
    private readonly DeckManager deckManager;
    private readonly currentEnergy playerEnergy;
    private readonly playerBlock playerBlock;
    private readonly playerHP playerHp;
    private readonly PlayerState playerState;
    private readonly ElementReactionResolver elementReactionResolver;

    private bool corrosiveWaveActive = false;
    private bool toxicCloudActive = false;
    private bool hellionResolving = false;

    public CardEffectResolver(
        BattleManager battleManager,
        DeckManager deckManager,
        currentEnergy playerEnergy,
        playerBlock playerBlock,
        playerHP playerHp,
        PlayerState playerState)
    {
        this.battleManager = battleManager;
        this.deckManager = deckManager;
        this.playerEnergy = playerEnergy;
        this.playerBlock = playerBlock;
        this.playerHp = playerHp;
        this.playerState = playerState;
        elementReactionResolver = new ElementReactionResolver(battleManager, playerState);

        if (battleManager?.TurnManager != null)
        {
            battleManager.TurnManager.OnDiscardPhase += ClearCorrosiveWave;
            battleManager.TurnManager.OnEnemyActionStarted += OnToxicCloudTrigger;
        }
    }

    public void Dispose()
    {
        if (battleManager?.TurnManager == null) return;

        battleManager.TurnManager.OnDiscardPhase -= ClearCorrosiveWave;
        battleManager.TurnManager.OnEnemyActionStarted -= OnToxicCloudTrigger;
    }

    public void ClearElementAttachmentAtTurnStart()
    {
        elementReactionResolver.ClearAttachmentAtTurnStart();
    }

    public void ResolveEffect(CardData card, ElementType chosenElement = ElementType.None)
    {
        if (card == null) return;

        battleManager?.LogBattleEvent($"结算 {card.cardName}。");

        switch (card.cardId)
        {
            case "starter_strike":
                DealAttackDamage(6, 1, ElementType.None, card.cardName);
                break;
            case "starter_defend":
                GainBlock(5);
                break;

            case "fire_sacrifice":
                LoseHp(6);
                playerEnergy?.AddEnergy(2);
                DrawCards(3, card.cardName);
                break;
            case "fire_demon_form":
                playerState?.AddDemonFormPowerPerTurn(2);
                battleManager?.LogBattleEvent("获得能力：恶魔形态。");
                break;
            case "fire_hellion":
                playerState?.SetHellionActive();
                battleManager?.LogBattleEvent("获得能力：地狱狂徒。");
                break;
            case "fire_hilt_strike":
                DealAttackDamage(9, 1, ElementType.Fire, card.cardName);
                DrawCards(1, card.cardName);
                break;
            case "fire_not_yet":
                HealPlayer(10);
                break;
            case "fire_perfect_strike":
                PerfectStrike(card);
                break;
            case "fire_double_strike":
                DealAttackDamage(5, 2, ElementType.Fire, card.cardName);
                break;
            case "fire_bloodletting":
                LoseHp(3);
                AddPower(1);
                break;
            case "fire_barricade":
                playerState?.SetBarricadeActive();
                battleManager?.LogBattleEvent("获得能力：壁垒。");
                break;
            case "fire_blood_wall":
                LoseHp(4);
                GainBlock(12);
                break;

            case "poison_blade":
            {
                ElementReactionResult reaction = DealAttackDamage(5, 1, ElementType.Poison, card.cardName);
                ApplyPoisonStacks(4 * reaction.PoisonMultiplier, card.cardName, false);
                break;
            }
            case "poison_sneak_needle":
            {
                ElementReactionResult reaction = DealAttackDamage(6, 1, ElementType.Poison, card.cardName);
                ApplyPoisonStacks(3 * reaction.PoisonMultiplier, card.cardName, false);
                break;
            }
            case "poison_prepare":
                DrawCards(1, card.cardName);
                DiscardCards(1);
                break;
            case "poison_roll":
                GainBlock(7);
                DrawCards(1, card.cardName);
                DiscardCards(1);
                break;
            case "poison_fog_guard":
                GainBlock((battleManager?.EnemyState?.PoisonStacks ?? 0) > 0 ? 10 : 6);
                break;
            case "poison_acrobatics":
                DrawCards(3, card.cardName);
                DiscardCards(1);
                break;
            case "poison_smoke_bomb":
                ApplyPoisonStacks(3, card.cardName);
                GainBlock(6);
                break;
            case "poison_bouncing_flask":
                ApplyPoisonStacks(3, card.cardName + " 1/3");
                ApplyPoisonStacks(3, card.cardName + " 2/3");
                ApplyPoisonStacks(3, card.cardName + " 3/3");
                break;
            case "poison_corrosive_wave":
                ActivateCorrosiveWave();
                break;
            case "poison_toxic_cloud":
                ActivateToxicCloud();
                break;

            case "water_blade":
                DealAttackDamage(7, 1, ElementType.Water, card.cardName);
                break;
            case "water_surge":
                DealAttackDamage(9, 1, ElementType.Water, card.cardName);
                DrawCards(1, card.cardName);
                break;
            case "water_curtain":
                GainBlock(7);
                AddWater(1, card.cardName);
                break;
            case "water_ebb":
                GainBlock(8);
                break;
            case "water_gather":
                AddWater(2, card.cardName);
                DrawCards(1, card.cardName);
                break;
            case "water_wave_guard":
                GainBlock(playerState != null && playerState.ReactionThisTurn ? 16 : 12);
                break;
            case "water_spring":
                DrawCards(3, card.cardName);
                break;
            case "water_lake_echo":
                playerState?.AddLakeEchoBlockPerWater(2);
                battleManager?.LogBattleEvent("获得能力：星湖回响。");
                break;
            case "water_deep_burst":
                DealAttackDamage(24, 1, ElementType.Water, card.cardName);
                break;
            case "water_vein_resonance":
                playerState?.SetWaterResonanceActive();
                battleManager?.LogBattleEvent("获得能力：水脉共鸣。");
                break;

            case "color_prism":
                AddGeneratedCard(RandomCard(data => CardDeckLibrary.IsMainElement(data.elementType)), card.cardName);
                break;
            case "color_blank_strike":
                DealAttackDamage(8, 1, NormalizeChosenElement(chosenElement), card.cardName);
                break;
            case "color_emergency_shield":
                GainBlock(8);
                break;
            case "color_tactical_sort":
                DrawCards(1, card.cardName);
                DiscardCards(1);
                break;
            case "color_sample":
                AddGeneratedCard(RandomCard(data => CardDeckLibrary.IsMainElement(data.elementType) && data.rarity == CardDeckLibrary.Common), card.cardName);
                break;
            case "color_harmony":
                playerEnergy?.AddEnergy(1);
                AddGeneratedCard(RandomCard(data => CardDeckLibrary.IsMainElement(data.elementType)), card.cardName);
                break;
            case "color_neutral_arrow":
            {
                ElementReactionResult reaction = DealAttackDamage(6, 1, NormalizeChosenElement(chosenElement), card.cardName);
                if (reaction.Triggered)
                    DrawCards(1, card.cardName + "反应奖励");
                break;
            }
            case "color_panacea":
                GainBlock(6);
                AddGeneratedCard(RandomCard(data => CardDeckLibrary.IsMainElement(data.elementType) && data.HasCardType(CardType.Skill)), card.cardName);
                break;
            case "color_tri_core":
                playerState?.AddTriCoreLimit(1);
                battleManager?.LogBattleEvent("获得能力：三相核心。");
                break;
            case "color_rift":
                AddGeneratedCard(RandomCard(data => CardDeckLibrary.IsMainElement(data.elementType)), card.cardName);
                AddGeneratedCard(RandomCard(data => CardDeckLibrary.IsMainElement(data.elementType)), card.cardName);
                break;

            default:
                DefaultResolve(card);
                break;
        }
    }

    private ElementReactionResult DealAttackDamage(int baseDamage, int hitCount, ElementType element, string source)
    {
        ElementReactionResult reaction = ResolveElementAttachment(element, source);
        int powerBonus = playerState != null ? playerState.power : 0;
        int finalDamage = Mathf.Max(0, Mathf.FloorToInt((baseDamage + powerBonus) * reaction.DamageMultiplier));

        for (int i = 0; i < hitCount; i++)
        {
            battleManager?.DealDamageToEnemy(finalDamage);
        }

        return reaction;
    }

    private ElementReactionResult ResolveElementAttachment(ElementType elementType, string source)
    {
        ElementReactionResult reaction = elementReactionResolver.ResolveElementAttachment(elementType, source);
        if (reaction.Triggered)
            TriggerTriCore();
        return reaction;
    }

    private void TriggerTriCore()
    {
        if (playerState == null || !playerState.TryUseTriCore())
            return;

        playerEnergy?.AddEnergy(1);
        DrawCards(2, "三相核心");
        battleManager?.LogBattleEvent("三相核心触发：获得1能量并抽2张牌。");
    }

    private void GainBlock(int amount)
    {
        playerBlock?.AddBlock(amount);
    }

    private void DrawCards(int count, string source)
    {
        List<CardData> drawn = deckManager?.DrawCards(count);
        if (drawn == null || drawn.Count == 0)
            return;

        if (corrosiveWaveActive)
        {
            ApplyPoisonStacks(drawn.Count * 2, "腐蚀波", true);
        }

        ResolveHellionDraws(drawn);
        battleManager?.LogBattleEvent($"{source}：抽 {drawn.Count} 张牌。");
    }

    private void ResolveHellionDraws(List<CardData> drawn)
    {
        if (hellionResolving || playerState == null || !playerState.HellionActive)
            return;

        hellionResolving = true;
        try
        {
            foreach (CardData card in drawn.ToList())
            {
                if (card == null || !card.cardName.Contains("打击") || !deckManager.handCards.Contains(card))
                    continue;

                battleManager?.LogBattleEvent($"地狱狂徒：抽到 {card.cardName}，自动免费打出。");
                deckManager.RemoveFromHand(card);
                ResolveEffect(card);
                deckManager.MoveResolvedCardAfterPlay(card);
            }
        }
        finally
        {
            hellionResolving = false;
        }
    }

    private void DiscardCards(int count)
    {
        for (int i = 0; i < count && deckManager.handCards.Count > 0; i++)
        {
            CardData card = deckManager.handCards[deckManager.handCards.Count - 1];
            if (card.HasCardType(CardType.Trick))
            {
                battleManager?.LogBattleEvent($"奇巧触发：{card.cardName} 被主动丢弃，免费打出。");
                ResolveEffect(card);
                deckManager.PlayCard(card);
            }
            else
            {
                deckManager.DiscardCard(card);
            }
        }
    }

    private void ApplyPoisonStacks(int amount, string source, bool resolveElement = true)
    {
        EnemyState enemyState = battleManager?.EnemyState;
        if (enemyState == null) return;

        int finalAmount = amount;
        if (resolveElement)
        {
            ElementReactionResult reaction = ResolveElementAttachment(ElementType.Poison, source);
            finalAmount *= reaction.PoisonMultiplier;
        }

        if (finalAmount <= 0) return;
        enemyState.AddPoisonStacks(finalAmount);
        battleManager?.LogBattleEvent($"{source}：施加中毒 {finalAmount} 层，当前中毒 {enemyState.PoisonStacks}。");
    }

    private void AddPower(int amount)
    {
        playerState?.AddPower(amount);
        battleManager?.LogBattleEvent($"获得 {amount} 点力量，当前力量 {playerState?.power ?? 0}。");
    }

    private void AddWater(int amount, string source)
    {
        playerState?.AddWater(amount);
        battleManager?.LogBattleEvent($"{source}：获得 {amount} 点水源，当前水源 {playerState?.WaterSource ?? 0}。");
    }

    private void LoseHp(int amount)
    {
        if (playerHp == null || amount <= 0) return;
        playerHp.CurrentHP = playerHp.CurrentHP - amount;
    }

    private void HealPlayer(int amount)
    {
        if (playerHp == null || amount <= 0) return;
        playerHp.CurrentHP = playerHp.CurrentHP + amount;
    }

    private void ActivateCorrosiveWave()
    {
        corrosiveWaveActive = true;
        battleManager?.LogBattleEvent("腐蚀波激活：本回合内每抽一张牌给予敌人2层中毒。");
    }

    private void ClearCorrosiveWave()
    {
        if (!corrosiveWaveActive) return;
        corrosiveWaveActive = false;
        battleManager?.LogBattleEvent("腐蚀波效果已清除。");
    }

    private void ActivateToxicCloud()
    {
        toxicCloudActive = true;
        battleManager?.LogBattleEvent("毒云弥漫激活：敌人行动前给予3层中毒。");
    }

    private void OnToxicCloudTrigger()
    {
        if (!toxicCloudActive) return;
        ApplyPoisonStacks(3, "毒云弥漫", true);
    }

    private void PerfectStrike(CardData playedCard)
    {
        int powerBonus = playerState != null ? playerState.power : 0;
        int strikeCount = deckManager.GetAllCombatCards().Count(card => card.cardName.Contains("打击"));
        if (playedCard != null && !deckManager.GetAllCombatCards().Contains(playedCard) && playedCard.cardName.Contains("打击"))
            strikeCount++;

        int damage = 6 + powerBonus + strikeCount * 2;
        DealAttackDamage(damage - powerBonus, 1, ElementType.Fire, playedCard?.cardName ?? "完美打击");
    }

    private void AddGeneratedCard(CardData data, string source)
    {
        if (data == null)
        {
            battleManager?.LogBattleEvent($"{source}：没有可生成的卡牌。");
            return;
        }

        deckManager.AddCardToHand(data);
        battleManager?.LogBattleEvent($"{source}：{data.cardName} 加入手牌。");
    }

    private CardData RandomCard(Func<CardData, bool> predicate)
    {
        List<CardData> pool = CardDeckLibrary.GetAllCards().Where(predicate).ToList();
        if (pool.Count == 0)
            return null;

        return pool[UnityEngine.Random.Range(0, pool.Count)];
    }

    private ElementType NormalizeChosenElement(ElementType element)
    {
        return CardDeckLibrary.IsMainElement(element) ? element : ElementType.Fire;
    }

    private void DefaultResolve(CardData card)
    {
        if (card.HasCardType(CardType.Attack))
            DealAttackDamage(6, 1, CardDeckLibrary.IsMainElement(card.elementType) ? card.elementType : ElementType.None, card.cardName);
        if (card.HasCardType(CardType.Defense))
            GainBlock(6);
    }
}

