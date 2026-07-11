using System.Collections.Generic;
using System.Linq;

namespace ElementalSpire.Cards
{
    public static class CardDeckLibrary
    {
        public const string Common = "普通";
        public const string Rare = "稀有";
        public const string Precious = "珍贵";
        public const string Basic = "基础";

        /// <summary>
        /// 根据卡牌ID获取卡牌数据
        /// </summary>
        public static CardData GetCardDataById(string cardId)
        {
            // 合并所有牌池查找（如果有全局allCards列表直接查更高效）
            var allCards = GetInitialDraftPool(ElementType.Fire, ElementType.Water)
                .Concat(GetBattleRewardPool(ElementType.Fire, 1))
                .Concat(GetBattleRewardPool(ElementType.Poison, 1))
                .Concat(GetBattleRewardPool(ElementType.Water, 1))
                .GroupBy(c => c.cardId)
                .Select(g => g.First());

            return allCards.FirstOrDefault(c => c.cardId == cardId);
        }

        private static readonly List<CardData> cardDataList = new List<CardData>
        {
            CreateCard("starter_strike", 0, ElementType.Colorless, "无色打击", "基础", CardType.Attack, 1, 0, "造成6点伤害。", "造成9点伤害。", -1, -1, false, false, true, false, "", new[] { "打击" }),
            CreateCard("starter_defend", 0, ElementType.Colorless, "无色防御", "基础", CardType.Skill | CardType.Defense, 1, 0, "获得5点格挡。", "获得8点格挡。", -1, -1, false, false, true, false, "", new[] { "防御" }),
            CreateCard("fire_sacrifice", 1, ElementType.Fire, "祭品", "珍贵", CardType.Skill, 0, 0, "失去6点生命。获得2点能量。抽3张牌。", "失去6点生命。获得2点能量。抽5张牌。", -1, -1, false, false, false, false, "8-10关", new[] { "燃血" }),
            CreateCard("fire_demon_form", 2, ElementType.Fire, "恶魔形态", "珍贵", CardType.Power, 3, 0, "每回合开始时获得2点力量。", "回合开始时获得3点力量。", -1, -1, false, false, false, false, "8-10关", new[] { "力量成长" }),
            CreateCard("fire_hellion", 3, ElementType.Fire, "地狱狂徒", "珍贵", CardType.Power, 2, 0, "每当你抽到名字中含有“打击”的牌时，自动免费打出那张牌。", "费用变为1。", 1, -1, false, false, false, false, "8-10关", new[] { "打击体系" }),
            CreateCard("fire_hilt_strike", 4, ElementType.Fire, "剑柄打击", "普通", CardType.Attack, 1, 0, "造成9点伤害，抽1张牌。", "造成10点伤害，抽2张牌。", -1, -1, false, false, false, true, "1-10关", new[] { "火攻击", "打击" }),
            CreateCard("fire_not_yet", 5, ElementType.Fire, "时候未到", "珍贵", CardType.Skill, 2, 0, "回复10点生命。消耗。", "费用变为1。回复13点生命。消耗。", 1, -1, true, false, false, false, "8-10关", new[] { "消耗", "回复" }),
            CreateCard("fire_perfect_strike", 6, ElementType.Fire, "完美打击", "普通", CardType.Attack, 2, 0, "造成6点伤害。你的牌组中每有一张名字含有“打击”的牌，伤害+2。", "每张“打击”牌使伤害+3。", -1, -1, false, false, false, true, "1-10关", new[] { "火攻击", "打击体系" }),
            CreateCard("fire_double_strike", 7, ElementType.Fire, "双重打击", "普通", CardType.Attack, 1, 0, "造成5点伤害，攻击2次。", "造成7点伤害，攻击2次。", -1, -1, false, false, false, true, "1-10关", new[] { "火攻击", "打击", "多段" }),
            CreateCard("fire_bloodletting", 8, ElementType.Fire, "放血", "普通", CardType.Skill, 0, 0, "失去3点生命。获得1点力量。消耗。", "失去2点生命。获得1点力量。消耗。", -1, -1, true, false, false, true, "1-10关", new[] { "消耗", "燃血" }),
            CreateCard("fire_barricade", 9, ElementType.Fire, "壁垒", "珍贵", CardType.Power, 3, 0, "你的格挡在回合结束时不再清空。", "费用变为2。", 2, -1, false, false, false, false, "8-10关", new[] { "格挡保留" }),
            CreateCard("fire_blood_wall", 10, ElementType.Fire, "血墙", "稀有", CardType.Skill | CardType.Defense, 1, 0, "失去4点生命。获得12点格挡。", "失去4点生命。获得16点格挡。", -1, -1, false, false, false, true, "1-10关", new[] { "燃血", "防御" }),
            CreateCard("poison_blade", 1, ElementType.Poison, "毒刃", "普通", CardType.Attack, 1, 0, "造成5点伤害。给予敌人4层中毒。", "造成7点伤害。给予敌人5层中毒。", -1, -1, false, false, false, true, "1-10关", new[] { "毒攻击" }),
            CreateCard("poison_sneak_needle", 2, ElementType.Poison, "奇袭毒针", "普通", CardType.Attack | CardType.Trick, 1, 0, "造成6点伤害。给予敌人3层中毒。被主动丢弃时免费打出。", "造成8点伤害。给予敌人4层中毒。", -1, -1, false, false, false, true, "1-10关", new[] { "奇巧", "毒攻击" }),
            CreateCard("poison_prepare", 3, ElementType.Poison, "准备", "普通", CardType.Skill, 0, 0, "抽1张牌，丢弃1张牌。", "抽2张牌，丢弃1张牌。", -1, -1, false, false, false, true, "1-10关", new[] { "抽弃" }),
            CreateCard("poison_roll", 4, ElementType.Poison, "翻滚", "普通", CardType.Skill | CardType.Defense, 1, 0, "获得7点格挡。抽1张牌，然后丢弃1张牌。", "获得9点格挡。抽1张牌，然后丢弃1张牌。", -1, -1, false, false, false, true, "1-10关", new[] { "抽弃", "防御" }),
            CreateCard("poison_fog_guard", 5, ElementType.Poison, "毒雾护身", "普通", CardType.Skill | CardType.Defense, 1, 0, "获得6点格挡。如果敌人处于中毒状态，额外获得4点格挡。", "获得8点格挡。如果任意敌人处于中毒状态，额外获得5点格挡。", -1, -1, false, false, false, true, "1-10关", new[] { "防御" }),
            CreateCard("poison_acrobatics", 6, ElementType.Poison, "杂技", "稀有", CardType.Skill, 1, 0, "抽3张牌，丢弃1张牌。", "抽4张牌，丢弃1张牌。", -1, -1, false, false, false, true, "1-10关", new[] { "抽弃" }),
            CreateCard("poison_smoke_bomb", 7, ElementType.Poison, "毒烟弹", "稀有", CardType.Skill | CardType.Defense | CardType.Trick, 1, 0, "给予敌人3层中毒。获得6点格挡。被主动丢弃时免费打出。", "给予所有敌人4层中毒。获得8点格挡。", -1, -1, false, false, false, true, "1-10关", new[] { "奇巧", "防御" }),
            CreateCard("poison_bouncing_flask", 8, ElementType.Poison, "弹跳毒瓶", "稀有", CardType.Skill, 2, 0, "给予敌人3次中毒，每次3层。", "随机给予敌人4次，每次3层中毒。", -1, -1, false, false, false, false, "4-10关", new[] { "叠毒" }),
            CreateCard("poison_corrosive_wave", 9, ElementType.Poison, "腐蚀波", "珍贵", CardType.Skill, 2, 0, "本回合内，每当你抽到一张牌，给予敌人2层中毒。", "每次抽牌改为给予所有敌人3层中毒。", -1, -1, false, false, false, false, "8-10关", new[] { "抽牌触发" }),
            CreateCard("poison_toxic_cloud", 10, ElementType.Poison, "毒云弥漫", "珍贵", CardType.Power, 2, 0, "敌人行动前，给予敌人3层中毒。", "敌人行动前，给予所有敌人4层中毒。", -1, -1, false, false, false, false, "8-10关", new[] { "持续叠毒" }),
            CreateCard("water_blade", 1, ElementType.Water, "水刃", "普通", CardType.Attack, 1, 0, "造成7点伤害。附着水元素。", "造成10点伤害。附着水元素。", -1, -1, false, false, false, true, "1-10关", new[] { "水攻击" }),
            CreateCard("water_surge", 2, ElementType.Water, "潮涌", "普通", CardType.Attack, 1, 1, "造成9点伤害。抽1张牌。附着水元素。", "造成11点伤害。抽1张牌。附着水元素。", -1, -1, false, false, false, true, "1-10关", new[] { "水攻击", "消耗水源" }),
            CreateCard("water_curtain", 3, ElementType.Water, "水幕", "普通", CardType.Skill | CardType.Defense, 1, 0, "获得7点格挡。获得1点水源。", "获得9点格挡。获得1点水源。", -1, -1, false, false, false, true, "1-10关", new[] { "防御", "蓄水" }),
            CreateCard("water_ebb", 4, ElementType.Water, "退潮", "普通", CardType.Skill | CardType.Defense, 0, 1, "获得8点格挡。", "获得11点格挡。", -1, -1, false, false, false, true, "1-10关", new[] { "防御", "消耗水源" }),
            CreateCard("water_gather", 5, ElementType.Water, "聚流", "普通", CardType.Skill, 1, 0, "获得2点水源。抽1张牌。", "获得2点水源。抽2张牌。", -1, -1, false, false, false, true, "1-10关", new[] { "蓄水" }),
            CreateCard("water_wave_guard", 6, ElementType.Water, "浪涌护体", "稀有", CardType.Skill | CardType.Defense, 1, 1, "获得12点格挡。如果本回合触发过元素反应，额外获得4点格挡。", "获得15点格挡。如果本回合触发过元素反应，额外获得5点格挡。", -1, -1, false, false, false, true, "1-10关", new[] { "防御", "反应奖励" }),
            CreateCard("water_spring", 7, ElementType.Water, "清泉术", "稀有", CardType.Skill, 1, 2, "抽3张牌。", "抽4张牌。", -1, -1, false, false, false, false, "4-10关", new[] { "过牌", "消耗水源" }),
            CreateCard("water_lake_echo", 8, ElementType.Water, "星湖回响", "稀有", CardType.Power, 2, 0, "每当你消耗1点水源，获得2点格挡。", "每当你消耗水源时，获得3点格挡。", -1, -1, false, false, false, false, "4-10关", new[] { "水源防御" }),
            CreateCard("water_deep_burst", 9, ElementType.Water, "深海爆发", "珍贵", CardType.Attack, 2, 3, "造成24点伤害。附着水元素。", "造成32点伤害。附着水元素。", -1, -1, false, false, false, false, "8-10关", new[] { "水攻击", "爆发" }),
            CreateCard("water_vein_resonance", 10, ElementType.Water, "水脉共鸣", "珍贵", CardType.Power, 3, 0, "每回合开始时获得1点水源。你每回合第一次消耗水源时，抽1张牌。", "费用变为2。其余效果不变。", 2, -1, false, false, false, false, "8-10关", new[] { "水源成长" }),
            CreateCard("color_prism", 1, ElementType.Colorless, "元素棱镜", "普通", CardType.Skill, 1, 0, "从3张元素牌中选择1张加入手牌。该牌本场战斗后移除。消耗。", "费用变为0。", 0, -1, true, false, false, false, "", new[] { "发现", "消耗" }),
            CreateCard("color_blank_strike", 2, ElementType.Colorless, "空白打击", "普通", CardType.Attack, 1, 0, "造成8点伤害。选择火、毒、水之一，本次攻击视为该元素攻击。", "造成11点伤害。", -1, -1, false, true, false, false, "", new[] { "选择元素", "打击" }),
            CreateCard("color_emergency_shield", 3, ElementType.Colorless, "应急护盾", "普通", CardType.Skill | CardType.Defense, 1, 0, "获得8点格挡。", "获得11点格挡。", -1, -1, false, false, false, false, "", new[] { "防御" }),
            CreateCard("color_tactical_sort", 4, ElementType.Colorless, "战术整理", "普通", CardType.Skill, 0, 0, "抽1张牌，丢弃1张牌。", "抽2张牌，丢弃1张牌。", -1, -1, false, false, false, false, "", new[] { "抽弃" }),
            CreateCard("color_sample", 5, ElementType.Colorless, "元素样本", "稀有", CardType.Skill, 0, 0, "选择火、毒、水之一。随机将1张该元素普通牌加入手牌。消耗。", "改为从3张该元素普通牌中选择1张加入手牌。消耗。", -1, -1, true, false, false, false, "", new[] { "生成", "消耗" }),
            CreateCard("color_harmony", 6, ElementType.Colorless, "调和", "稀有", CardType.Skill, 1, 0, "获得1点能量。从3张元素牌中选择1张加入手牌，该牌本回合费用-1。消耗。", "生成的牌本回合费用减少2。", -1, -1, true, false, false, false, "", new[] { "发现", "消耗" }),
            CreateCard("color_neutral_arrow", 7, ElementType.Colorless, "中和箭", "稀有", CardType.Attack, 1, 0, "造成6点伤害。选择火、毒、水之一，本次攻击视为该元素攻击；若触发元素反应，抽1张牌。", "造成8点伤害；若触发元素反应，抽2张牌。", -1, -1, false, true, false, false, "", new[] { "选择元素", "反应奖励" }),
            CreateCard("color_panacea", 8, ElementType.Colorless, "万用药剂", "稀有", CardType.Skill | CardType.Defense, 1, 0, "获得6点格挡。从3张元素技能牌中选择1张加入手牌。消耗。", "获得9点格挡。其余效果不变。", -1, -1, true, false, false, false, "", new[] { "发现", "防御", "消耗" }),
            CreateCard("color_tri_core", 9, ElementType.Colorless, "三相核心", "珍贵", CardType.Power, 2, 0, "每场战斗第一次触发元素反应时，抽2张牌并获得1点能量。", "每场战斗前两次触发元素反应时，抽2张牌并获得1点能量。", -1, -1, false, false, false, false, "", new[] { "反应奖励" }),
            CreateCard("color_rift", 10, ElementType.Colorless, "元素裂隙", "珍贵", CardType.Skill, 2, 0, "从3张元素牌中选择2张加入手牌。它们本回合费用-1。消耗。", "费用变为1。", 1, -1, true, false, false, false, "", new[] { "发现", "消耗" }),
        };

        public static IReadOnlyList<CardData> GetAllCards()
        {
            return cardDataList;
        }

        public static CardData GetCardById(string cardId)
        {
            return cardDataList.FirstOrDefault(cardData => cardData.cardId == cardId);
        }

        public static IEnumerable<CardData> GetStarterDeck()
        {
            return Enumerable.Repeat(GetCardById("starter_strike"), 5)
                .Concat(Enumerable.Repeat(GetCardById("starter_defend"), 5));
        }

        public static IEnumerable<CardInstance> GetStarterCardInstances()
        {
            return Enumerable.Range(0, 5).Select(_ => new CardInstance("starter_strike"))
                .Concat(Enumerable.Range(0, 5).Select(_ => new CardInstance("starter_defend")));
        }

        public static IEnumerable<CardData> GetCardsByDeckPreset(DeckPreset deckPreset)
        {
            switch (deckPreset)
            {
                case DeckPreset.Fire:
                    return cardDataList.Where(cardData => cardData.elementType == ElementType.Fire);
                case DeckPreset.Poison:
                    return cardDataList.Where(cardData => cardData.elementType == ElementType.Poison);
                case DeckPreset.Water:
                    return cardDataList.Where(cardData => cardData.elementType == ElementType.Water);
                case DeckPreset.Colorless:
                    return cardDataList.Where(cardData => cardData.elementType == ElementType.Colorless);
                default:
                    return cardDataList;
            }
        }

        public static IEnumerable<CardData> GetInitialDraftPool(ElementType firstElement, ElementType secondElement)
        {
            return cardDataList.Where(cardData =>
                IsMainElement(cardData.elementType)
                && (cardData.elementType == firstElement || cardData.elementType == secondElement)
                && cardData.startEligible
                && cardData.rarity != Precious);
        }

        public static IEnumerable<CardData> GetBattleRewardPool(ElementType elementType, int floor)
        {
            return cardDataList.Where(cardData =>
                cardData.elementType == elementType
                && IsMainElement(cardData.elementType)
                && !cardData.starter
                && IsAllowedByFloor(cardData, floor));
        }

        public static IEnumerable<CardData> GetEventPool(int floor)
        {
            return cardDataList.Where(cardData =>
                !cardData.starter
                && cardData.rarity != Basic
                && IsAllowedByFloor(cardData, floor));
        }

        public static IEnumerable<CardData> GetPreciousCards()
        {
            return cardDataList.Where(cardData => cardData.rarity == Precious);
        }

        public static bool IsAllowedByFloor(CardData cardData, int floor)
        {
            if (cardData == null)
            {
                return false;
            }

            if (cardData.rewardStage == "8-10关")
            {
                return floor >= 8;
            }

            if (cardData.rewardStage == "4-10关")
            {
                return floor >= 4;
            }

            return true;
        }

        public static bool IsMainElement(ElementType elementType)
        {
            return elementType == ElementType.Fire
                || elementType == ElementType.Poison
                || elementType == ElementType.Water;
        }

        private static CardData CreateCard(
            string cardId,
            int cardNumber,
            ElementType elementType,
            string cardName,
            string rarity,
            CardType cardType,
            int cost,
            int waterCost,
            string description,
            string upgradeDescription,
            int upgradedCost,
            int upgradedWaterCost,
            bool exhaust = false,
            bool chooseElement = false,
            bool starter = false,
            bool startEligible = false,
            string rewardStage = "",
            string[] keywords = null)
        {
            return new CardData
            {
                cardId = cardId,
                cardNumber = cardNumber,
                elementType = elementType,
                cardName = cardName,
                rarity = rarity,
                cardType = cardType,
                cost = cost,
                waterCost = waterCost,
                description = description,
                upgradeDescription = upgradeDescription,
                upgradedCost = upgradedCost,
                upgradedWaterCost = upgradedWaterCost,
                hasUpgrade = !string.IsNullOrEmpty(upgradeDescription) || upgradedCost >= 0 || upgradedWaterCost >= 0,
                exhaust = exhaust,
                chooseElement = chooseElement,
                starter = starter,
                startEligible = startEligible,
                rewardStage = rewardStage,
                keywords = keywords ?? System.Array.Empty<string>()
            };
        }
    }
}
