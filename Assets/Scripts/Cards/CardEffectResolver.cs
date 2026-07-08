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

    private int corrosiveWavePoisonPerDraw = 0;
    private int toxicCloudPoisonAmount = 0;
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
        ResolveEffect(new CardInstance(card.cardId), chosenElement);
    }

    public void ResolveEffect(CardInstance cardInstance, ElementType chosenElement = ElementType.None)
    {
        if (cardInstance == null) return;

        CardData card = cardInstance.GetCardData();
        if (card == null) return;

        bool upgraded = cardInstance.isUpgraded;
        battleManager?.LogBattleEvent($"结算 {card.cardName}{(upgraded ? "+" : string.Empty)}。");

        switch (card.cardId)
        {
            case "starter_strike":
                DealAttackDamage(upgraded ? 9 : 6, 1, ElementType.None, card.cardName);
                break;
            case "starter_defend":
                GainBlock(upgraded ? 8 : 5);
                break;

            case "fire_sacrifice":
                LoseHp(6);
                playerEnergy?.AddEnergy(2);
                DrawCards(upgraded ? 5 : 3, card.cardName);
                break;
            case "fire_demon_form":
                playerState?.AddDemonFormPowerPerTurn(upgraded ? 3 : 2);
                battleManager?.LogBattleEvent("获得能力：恶魔形态。");
                break;
            case "fire_hellion":
                playerState?.SetHellionActive();
                battleManager?.LogBattleEvent("获得能力：地狱狂徒。");
                break;
            case "fire_hilt_strike":
                DealAttackDamage(upgraded ? 10 : 9, 1, ElementType.Fire, card.cardName);
                DrawCards(upgraded ? 2 : 1, card.cardName);
                break;
            case "fire_not_yet":
                HealPlayer(upgraded ? 13 : 10);
                break;
            case "fire_perfect_strike":
                PerfectStrike(cardInstance);
                break;
            case "fire_double_strike":
                DealAttackDamage(upgraded ? 7 : 5, 2, ElementType.Fire, card.cardName);
                break;
            case "fire_bloodletting":
                LoseHp(upgraded ? 2 : 3);
                AddPower(1);
                break;
            case "fire_barricade":
                playerState?.SetBarricadeActive();
                battleManager?.LogBattleEvent("获得能力：壁垒。");
                break;
            case "fire_blood_wall":
                LoseHp(4);
                GainBlock(upgraded ? 16 : 12);
                break;

            case "poison_blade":
            {
                ElementReactionResult reaction = DealAttackDamage(upgraded ? 7 : 5, 1, ElementType.Poison, card.cardName);
                ApplyPoisonStacks((upgraded ? 5 : 4) * reaction.PoisonMultiplier, card.cardName, false);
                break;
            }
            case "poison_sneak_needle":
            {
                ElementReactionResult reaction = DealAttackDamage(upgraded ? 8 : 6, 1, ElementType.Poison, card.cardName);
                ApplyPoisonStacks((upgraded ? 4 : 3) * reaction.PoisonMultiplier, card.cardName, false);
                break;
            }
            case "poison_prepare":
                DrawCards(upgraded ? 2 : 1, card.cardName);
                DiscardCards(1);
                break;
            case "poison_roll":
                GainBlock(upgraded ? 9 : 7);
                DrawCards(1, card.cardName);
                DiscardCards(1);
                break;
            case "poison_fog_guard":
                GainBlock((battleManager?.EnemyState?.PoisonStacks ?? 0) > 0 ? (upgraded ? 13 : 10) : (upgraded ? 8 : 6));
                break;
            case "poison_acrobatics":
                DrawCards(upgraded ? 4 : 3, card.cardName);
                DiscardCards(1);
                break;
            case "poison_smoke_bomb":
                ApplyPoisonStacks(upgraded ? 4 : 3, card.cardName);
                GainBlock(upgraded ? 8 : 6);
                break;
            case "poison_bouncing_flask":
            {
                int repeatCount = upgraded ? 4 : 3;
                for (int i = 1; i <= repeatCount; i++)
                    ApplyPoisonStacks(3, $"{card.cardName} {i}/{repeatCount}");
                break;
            }
            case "poison_corrosive_wave":
                ActivateCorrosiveWave(upgraded ? 3 : 2);
                break;
            case "poison_toxic_cloud":
                ActivateToxicCloud(upgraded ? 4 : 3);
                break;

            case "water_blade":
                DealAttackDamage(upgraded ? 10 : 7, 1, ElementType.Water, card.cardName);
                break;
            case "water_surge":
                DealAttackDamage(upgraded ? 11 : 9, 1, ElementType.Water, card.cardName);
                DrawCards(1, card.cardName);
                break;
            case "water_curtain":
                GainBlock(upgraded ? 9 : 7);
                AddWater(1, card.cardName);
                break;
            case "water_ebb":
                GainBlock(upgraded ? 11 : 8);
                break;
            case "water_gather":
                AddWater(2, card.cardName);
                DrawCards(upgraded ? 2 : 1, card.cardName);
                break;
            case "water_wave_guard":
                GainBlock(playerState != null && playerState.ReactionThisTurn ? (upgraded ? 20 : 16) : (upgraded ? 15 : 12));
                break;
            case "water_spring":
                DrawCards(upgraded ? 4 : 3, card.cardName);
                break;
            case "water_lake_echo":
                playerState?.AddLakeEchoBlockPerWater(upgraded ? 3 : 2);
                battleManager?.LogBattleEvent("获得能力：星湖回响。");
                break;
            case "water_deep_burst":
                DealAttackDamage(upgraded ? 32 : 24, 1, ElementType.Water, card.cardName);
                break;
            case "water_vein_resonance":
                playerState?.SetWaterResonanceActive();
                battleManager?.LogBattleEvent("获得能力：水脉共鸣。");
                break;

            case "color_prism":
                AddGeneratedCard(RandomCard(data => CardDeckLibrary.IsMainElement(data.elementType)), card.cardName);
                break;
            case "color_blank_strike":
                DealAttackDamage(upgraded ? 11 : 8, 1, NormalizeChosenElement(chosenElement), card.cardName);
                break;
            case "color_emergency_shield":
                GainBlock(upgraded ? 11 : 8);
                break;
            case "color_tactical_sort":
                DrawCards(upgraded ? 2 : 1, card.cardName);
                DiscardCards(1);
                break;
            case "color_sample":
                AddGeneratedCard(RandomCard(data => CardDeckLibrary.IsMainElement(data.elementType) && data.rarity == CardDeckLibrary.Common), card.cardName);
                break;
            case "color_harmony":
                playerEnergy?.AddEnergy(1);
                AddGeneratedCard(RandomCard(data => CardDeckLibrary.IsMainElement(data.elementType)), card.cardName, upgraded ? -2 : -1);
                break;
            case "color_neutral_arrow":
            {
                ElementReactionResult reaction = DealAttackDamage(upgraded ? 8 : 6, 1, NormalizeChosenElement(chosenElement), card.cardName);
                if (reaction.Triggered)
                    DrawCards(upgraded ? 2 : 1, card.cardName + "反应奖励");
                break;
            }
            case "color_panacea":
                GainBlock(upgraded ? 9 : 6);
                AddGeneratedCard(RandomCard(data => CardDeckLibrary.IsMainElement(data.elementType) && data.HasCardType(CardType.Skill)), card.cardName);
                break;
            case "color_tri_core":
                playerState?.AddTriCoreLimit(upgraded ? 2 : 1);
                battleManager?.LogBattleEvent("获得能力：三相核心。");
                break;
            case "color_rift":
                AddGeneratedCard(RandomCard(data => CardDeckLibrary.IsMainElement(data.elementType)), card.cardName, -1);
                AddGeneratedCard(RandomCard(data => CardDeckLibrary.IsMainElement(data.elementType)), card.cardName, -1);
                break;

            default:
                DefaultResolve(card);
                break;
        }
    }

    private ElementReactionResult DealAttackDamage(int baseDamage, int hitCount, ElementType element, string source)
    {
        int powerBonus = playerState != null ? playerState.power : 0;
        ElementReactionResult lastReaction = new ElementReactionResult();

        for (int i = 0; i < hitCount; i++)
        {
            // 每段攻击独立结算元素附着/反应
            lastReaction = ResolveElementAttachment(element, source);

            int finalDamage = Mathf.Max(0, Mathf.FloorToInt((baseDamage + powerBonus) * lastReaction.DamageMultiplier));
            battleManager?.DealDamageToEnemy(finalDamage);
            battleManager?.LogBattleEvent($"{source} 第{i + 1}段：造成 {finalDamage} 点伤害。");
        }

        return lastReaction;
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
        List<CardInstance> drawn = deckManager?.DrawCards(count);
        if (drawn == null || drawn.Count == 0)
            return;

        if (corrosiveWavePoisonPerDraw > 0)
        {
            ApplyPoisonStacks(drawn.Count * corrosiveWavePoisonPerDraw, "腐蚀波", true);
        }

        ResolveHellionDraws(drawn);
        battleManager?.LogBattleEvent($"{source}：抽 {drawn.Count} 张牌。");
    }

    private void ResolveHellionDraws(List<CardInstance> drawn)
    {
        if (hellionResolving || playerState == null || !playerState.HellionActive)
            return;

        hellionResolving = true;
        try
        {
            foreach (CardInstance cardInstance in drawn.ToList())
            {
                CardData card = cardInstance?.GetCardData();
                if (card == null || !card.cardName.Contains("打击") || !deckManager.handCards.Contains(cardInstance))
                    continue;

                battleManager?.LogBattleEvent($"地狱狂徒：抽到 {card.cardName}{(cardInstance.isUpgraded ? "+" : string.Empty)}，自动免费打出。");
                deckManager.RemoveFromHand(cardInstance);
                ResolveEffect(cardInstance);
                deckManager.MoveResolvedCardAfterPlay(cardInstance);
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
            CardInstance cardInstance = deckManager.handCards[deckManager.handCards.Count - 1];
            CardData card = cardInstance.GetCardData();
            if (card == null)
                continue;

            if (card.HasCardType(CardType.Trick))
            {
                battleManager?.LogBattleEvent($"奇巧触发：{card.cardName}{(cardInstance.isUpgraded ? "+" : string.Empty)} 被主动丢弃，免费打出。");
                ResolveEffect(cardInstance);
                deckManager.PlayCard(cardInstance);
            }
            else
            {
                deckManager.DiscardCard(cardInstance);
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

    private void ActivateCorrosiveWave(int poisonPerDraw)
    {
        corrosiveWavePoisonPerDraw = poisonPerDraw;
        battleManager?.LogBattleEvent($"腐蚀波激活：本回合内每抽一张牌给予敌人{poisonPerDraw}层中毒。");
    }

    private void ClearCorrosiveWave()
    {
        if (corrosiveWavePoisonPerDraw <= 0) return;
        corrosiveWavePoisonPerDraw = 0;
        battleManager?.LogBattleEvent("腐蚀波效果已清除。");
    }

    private void ActivateToxicCloud(int poisonAmount)
    {
        toxicCloudPoisonAmount = poisonAmount;
        battleManager?.LogBattleEvent($"毒云弥漫激活：敌人行动前给予{poisonAmount}层中毒。");
    }

    private void OnToxicCloudTrigger()
    {
        if (toxicCloudPoisonAmount <= 0) return;
        ApplyPoisonStacks(toxicCloudPoisonAmount, "毒云弥漫", true);
    }

    private void PerfectStrike(CardInstance playedCard)
    {
        CardData playedData = playedCard?.GetCardData();
        bool upgraded = playedCard != null && playedCard.isUpgraded;
        int powerBonus = playerState != null ? playerState.power : 0;
        int strikeCount = deckManager.GetAllCombatCards().Count(cardInstance =>
        {
            CardData data = cardInstance?.GetCardData();
            return data != null && data.cardName.Contains("打击");
        });

        if (playedData != null && !deckManager.GetAllCombatCards().Contains(playedCard) && playedData.cardName.Contains("打击"))
            strikeCount++;

        int damage = 6 + powerBonus + strikeCount * (upgraded ? 3 : 2);
        DealAttackDamage(damage - powerBonus, 1, ElementType.Fire, playedData?.cardName ?? "完美打击");
    }

    private void AddGeneratedCard(CardData data, string source, int energyCostModifier = 0)
    {
        if (data == null)
        {
            battleManager?.LogBattleEvent($"{source}：没有可生成的卡牌。");
            return;
        }

        CardInstance generatedCard = new CardInstance(data.cardId)
        {
            energyCostModifier = energyCostModifier
        };
        deckManager.AddCardToHand(generatedCard);

        string costText = energyCostModifier != 0 ? $"，本回合费用{energyCostModifier}" : string.Empty;
        battleManager?.LogBattleEvent($"{source}：{data.cardName} 加入手牌{costText}。");
        battleManager?.NotifyHandChanged();
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
