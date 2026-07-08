using System.Collections.Generic;
using System.Linq;

namespace ElementalSpire.TextBattlePrototype
{
    public static class CardLibrary
    {
        private static readonly List<CardData> Cards = new List<CardData>
        {
            Card("fire_sacrifice", 1, ElementType.Fire, "祭品", "珍贵", CardType.Skill, 0, 0, "失去6点生命。获得2点能量。抽3张牌。"),
            Card("fire_demon_form", 2, ElementType.Fire, "恶魔形态", "珍贵", CardType.Power, 3, 0, "回合开始时获得2点力量。"),
            Card("fire_hellion", 3, ElementType.Fire, "地狱狂徒", "珍贵", CardType.Power, 2, 0, "每当你抽到名字中有“打击”的牌时，对敌人免费打出这张牌。"),
            Card("fire_hilt_strike", 4, ElementType.Fire, "剑柄打击", "普通", CardType.Attack, 1, 0, "造成9点伤害，抽1张牌。"),
            Card("fire_not_yet", 5, ElementType.Fire, "时候未到", "珍贵", CardType.Skill, 2, 0, "回复10点生命。消耗。", exhaust: true),
            Card("fire_perfect_strike", 6, ElementType.Fire, "完美打击", "普通", CardType.Attack, 2, 0, "造成6点伤害。你每拥有一张名字中含有“打击”的牌，伤害+2。"),
            Card("fire_double_strike", 7, ElementType.Fire, "双重打击", "普通", CardType.Attack, 1, 0, "造成5点伤害，攻击2次。"),
            Card("fire_bloodletting", 8, ElementType.Fire, "放血", "普通", CardType.Skill, 0, 0, "失去3点生命。获得1点力量。消耗。", exhaust: true),
            Card("fire_barricade", 9, ElementType.Fire, "壁垒", "珍贵", CardType.Power, 3, 0, "你的格挡在回合结束时不再清空。"),
            Card("fire_blood_wall", 10, ElementType.Fire, "血墙", "稀有", CardType.Skill | CardType.Defense, 1, 0, "失去4点生命。获得12点格挡。"),

            Card("poison_blade", 1, ElementType.Poison, "毒刃", "普通", CardType.Attack, 1, 0, "造成5点伤害。给予敌人4层中毒。"),
            Card("poison_sneak_needle", 2, ElementType.Poison, "奇袭毒针", "普通", CardType.Attack | CardType.Trick, 1, 0, "造成6点伤害。给予敌人3层中毒。"),
            Card("poison_prepare", 3, ElementType.Poison, "准备", "普通", CardType.Skill, 0, 0, "抽1张牌，丢弃1张牌。"),
            Card("poison_roll", 4, ElementType.Poison, "翻滚", "普通", CardType.Skill | CardType.Defense, 1, 0, "获得7点格挡。抽1张牌，然后丢弃1张牌。"),
            Card("poison_fog_guard", 5, ElementType.Poison, "毒雾护身", "普通", CardType.Skill | CardType.Defense, 1, 0, "获得6点格挡。如果任意敌人处于中毒状态，额外获得4点格挡。"),
            Card("poison_acrobatics", 6, ElementType.Poison, "杂技", "稀有", CardType.Skill, 1, 0, "抽3张牌，丢弃1张牌。"),
            Card("poison_smoke_bomb", 7, ElementType.Poison, "毒烟弹", "稀有", CardType.Skill | CardType.Defense | CardType.Trick, 1, 0, "给予所有敌人3层中毒。获得6点格挡。"),
            Card("poison_bouncing_flask", 8, ElementType.Poison, "弹跳毒瓶", "稀有", CardType.Skill, 2, 0, "随机给予敌人3次，每次3层中毒。"),
            Card("poison_corrosive_wave", 9, ElementType.Poison, "腐蚀波", "珍贵", CardType.Skill, 2, 0, "本回合内，每当你抽到一张牌，给予所有敌人2层中毒。"),
            Card("poison_toxic_cloud", 10, ElementType.Poison, "毒云弥漫", "珍贵", CardType.Power, 2, 0, "敌人行动前，给予所有敌人3层中毒。"),

            Card("water_blade", 1, ElementType.Water, "水刃", "普通", CardType.Attack, 1, 0, "造成7点伤害。附着水元素。"),
            Card("water_surge", 2, ElementType.Water, "潮涌", "普通", CardType.Attack, 1, 1, "造成9点伤害。抽1张牌。附着水元素。"),
            Card("water_curtain", 3, ElementType.Water, "水幕", "普通", CardType.Skill | CardType.Defense, 1, 0, "获得7点格挡。获得1点水源。"),
            Card("water_ebb", 4, ElementType.Water, "退潮", "普通", CardType.Skill | CardType.Defense, 0, 1, "获得8点格挡。"),
            Card("water_gather", 5, ElementType.Water, "聚流", "普通", CardType.Skill, 1, 0, "获得2点水源。抽1张牌。"),
            Card("water_wave_guard", 6, ElementType.Water, "浪涌护体", "稀有", CardType.Skill | CardType.Defense, 1, 1, "获得12点格挡。如果本回合触发过元素反应，额外获得4点格挡。"),
            Card("water_spring", 7, ElementType.Water, "清泉术", "稀有", CardType.Skill, 1, 2, "抽3张牌。"),
            Card("water_lake_echo", 8, ElementType.Water, "星湖回响", "稀有", CardType.Power, 2, 0, "每当你消耗水源时，获得2点格挡。"),
            Card("water_deep_burst", 9, ElementType.Water, "深海爆发", "珍贵", CardType.Attack, 2, 3, "造成24点伤害。附着水元素。"),
            Card("water_vein_resonance", 10, ElementType.Water, "水脉共鸣", "珍贵", CardType.Power, 3, 0, "每回合开始时额外获得1点水源。你每回合第一次消耗水源时，抽1张牌。"),

            Card("color_prism", 1, ElementType.Colorless, "元素棱镜", "普通", CardType.Skill, 1, 0, "随机将1张火/毒/水元素牌加入手牌。消耗。", exhaust: true),
            Card("color_blank_strike", 2, ElementType.Colorless, "空白打击", "普通", CardType.Attack, 1, 0, "造成8点伤害。选择火、毒、水之一，本次攻击视为该元素攻击。", chooseElement: true),
            Card("color_emergency_shield", 3, ElementType.Colorless, "应急护盾", "普通", CardType.Skill | CardType.Defense, 1, 0, "获得8点格挡。"),
            Card("color_tactical_sort", 4, ElementType.Colorless, "战术整理", "普通", CardType.Skill, 0, 0, "抽1张牌，丢弃1张牌。"),
            Card("color_sample", 5, ElementType.Colorless, "元素样本", "稀有", CardType.Skill, 0, 0, "随机将1张火/毒/水普通牌加入手牌。消耗。", exhaust: true),
            Card("color_harmony", 6, ElementType.Colorless, "调和", "稀有", CardType.Skill, 1, 0, "获得1点能量。随机将1张火/毒/水元素牌加入手牌，该牌本回合费用-1。消耗。", exhaust: true),
            Card("color_neutral_arrow", 7, ElementType.Colorless, "中和箭", "稀有", CardType.Attack, 1, 0, "造成6点伤害。选择火、毒、水之一；若触发元素反应，抽1张牌。", chooseElement: true),
            Card("color_panacea", 8, ElementType.Colorless, "万用药剂", "稀有", CardType.Skill | CardType.Defense, 1, 0, "获得6点格挡。随机将1张火/毒/水技能牌加入手牌。消耗。", exhaust: true),
            Card("color_tri_core", 9, ElementType.Colorless, "三相核心", "珍贵", CardType.Power, 2, 0, "每场战斗第一次触发元素反应时，抽2张牌并获得1点能量。"),
            Card("color_rift", 10, ElementType.Colorless, "元素裂隙", "珍贵", CardType.Skill, 2, 0, "随机将2张火/毒/水元素牌加入手牌，它们本回合费用-1。消耗。", exhaust: true),
        };

        public static IReadOnlyList<CardData> All
        {
            get { return Cards; }
        }

        public static CardData Get(string id)
        {
            return Cards.First(card => card.Id == id);
        }

        public static IEnumerable<CardData> ForDeck(DeckPreset preset)
        {
            switch (preset)
            {
                case DeckPreset.Fire:
                    return Cards.Where(card => card.Element == ElementType.Fire);
                case DeckPreset.Poison:
                    return Cards.Where(card => card.Element == ElementType.Poison);
                case DeckPreset.Water:
                    return Cards.Where(card => card.Element == ElementType.Water);
                case DeckPreset.Colorless:
                    return Cards.Where(card => card.Element == ElementType.Colorless);
                default:
                    return Cards;
            }
        }

        private static CardData Card(
            string id,
            int number,
            ElementType element,
            string name,
            string rarity,
            CardType types,
            int energyCost,
            int waterCost,
            string text,
            bool exhaust = false,
            bool chooseElement = false)
        {
            return new CardData
            {
                Id = id,
                Number = number,
                Element = element,
                Name = name,
                Rarity = rarity,
                Types = types,
                EnergyCost = energyCost,
                WaterCost = waterCost,
                Text = text,
                Exhaust = exhaust,
                ChooseElement = chooseElement
            };
        }
    }
}
