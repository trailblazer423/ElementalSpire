using System;
using System.Collections.Generic;
using System.Linq;

namespace ElementalSpire.TextBattlePrototype
{
    public enum ElementType
    {
        None,
        Fire,
        Poison,
        Water,
        Colorless
    }

    [Flags]
    public enum CardType
    {
        None = 0,
        Attack = 1,
        Skill = 2,
        Defense = 4,
        Power = 8,
        Trick = 16
    }

    public enum DeckPreset
    {
        Fire,
        Poison,
        Water,
        Colorless,
        All
    }

    public sealed class CardData
    {
        public string Id;
        public int Number;
        public ElementType Element;
        public string Name;
        public string Rarity;
        public CardType Types;
        public int EnergyCost;
        public int WaterCost;
        public string Text;
        public bool Exhaust;
        public bool ChooseElement;

        public bool HasType(CardType type)
        {
            return (Types & type) == type;
        }
    }

    public sealed class RuntimeCard
    {
        public RuntimeCard(int instanceId, CardData data)
        {
            InstanceId = instanceId;
            Data = data;
        }

        public int InstanceId { get; }
        public CardData Data { get; }
        public bool Temporary { get; set; }
        public int CostModifier { get; set; }
        public bool TemporaryCost { get; set; }

        public int CurrentEnergyCost
        {
            get { return Math.Max(0, Data.EnergyCost + CostModifier); }
        }
    }

    public sealed class PlayerState
    {
        public int Hp = 80;
        public int MaxHp = 80;
        public int Block;
        public int Energy;
        public int MaxEnergy = 3;
        public int Water;
        public int Strength;
    }

    public sealed class EnemyState
    {
        public int Hp = 220;
        public int MaxHp = 220;
        public int Poison;
        public ElementType TemporaryAttachment = ElementType.None;
        public bool DeepPoison;
    }

    public sealed class PowerState
    {
        public int DemonFormStrengthPerTurn;
        public bool Hellion;
        public bool Barricade;
        public int ToxicCloudPoison;
        public int LakeEchoBlockPerWater;
        public bool WaterResonance;
        public int TriCoreLimit;
        public int TriCoreUsed;
    }

    public sealed class BattleState
    {
        public readonly PlayerState Player = new PlayerState();
        public readonly EnemyState Enemy = new EnemyState();
        public readonly PowerState PowersState = new PowerState();
        public readonly List<RuntimeCard> DrawPile = new List<RuntimeCard>();
        public readonly List<RuntimeCard> Hand = new List<RuntimeCard>();
        public readonly List<RuntimeCard> DiscardPile = new List<RuntimeCard>();
        public readonly List<RuntimeCard> ExhaustPile = new List<RuntimeCard>();
        public readonly List<RuntimeCard> Powers = new List<RuntimeCard>();
        public readonly List<string> Log = new List<string>();

        public int Turn;
        public bool ReactionThisTurn;
        public int CorrosiveWavePoisonOnDraw;
        public bool WaterResonanceUsedThisTurn;
        public int PendingDiscardCount;
        public string PendingDiscardSource;

        public bool IsWaitingForDiscard
        {
            get { return PendingDiscardCount > 0; }
        }
    }

    public sealed class ElementReactionResult
    {
        public bool Triggered;
        public string Name = string.Empty;
        public float DamageMultiplier = 1f;
        public int PoisonMultiplier = 1;
    }

    public static class BattleText
    {
        public static string ElementName(ElementType element)
        {
            switch (element)
            {
                case ElementType.Fire:
                    return "火";
                case ElementType.Poison:
                    return "毒";
                case ElementType.Water:
                    return "水";
                case ElementType.Colorless:
                    return "无色";
                default:
                    return "无";
            }
        }

        public static string TypesName(CardType types)
        {
            var names = new List<string>();
            if ((types & CardType.Attack) != 0) names.Add("攻击");
            if ((types & CardType.Skill) != 0) names.Add("技能");
            if ((types & CardType.Defense) != 0) names.Add("防御");
            if ((types & CardType.Power) != 0) names.Add("能力");
            if ((types & CardType.Trick) != 0) names.Add("奇巧");
            return names.Count == 0 ? "无" : string.Join("/", names);
        }

        public static string CardList(IEnumerable<RuntimeCard> cards)
        {
            return string.Join("、", cards.Select(card => card.Data.Name));
        }
    }
}
