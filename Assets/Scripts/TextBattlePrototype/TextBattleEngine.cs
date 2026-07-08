using System;
using System.Collections.Generic;
using System.Linq;

namespace ElementalSpire.TextBattlePrototype
{
    public sealed class TextBattleEngine
    {
        private readonly Random random = new Random();
        private int nextInstanceId = 1;

        public BattleState State { get; private set; } = new BattleState();

        public void StartNewBattle(DeckPreset preset)
        {
            nextInstanceId = 1;
            State = new BattleState();

            foreach (var card in CardLibrary.ForDeck(preset))
            {
                State.DrawPile.Add(CreateRuntimeCard(card));
            }

            Shuffle(State.DrawPile);
            Log("开始战斗：" + preset + " 牌组，共 " + State.DrawPile.Count + " 张。");
            StartTurn();
        }

        public void StartTurn()
        {
            State.Turn++;
            State.PendingDiscardCount = 0;
            State.PendingDiscardSource = null;
            State.ReactionThisTurn = false;
            State.CorrosiveWavePoisonOnDraw = 0;
            State.WaterResonanceUsedThisTurn = false;
            State.Player.Energy = State.Player.MaxEnergy;

            int waterGain = 1 + (State.PowersState.WaterResonance ? 1 : 0);
            State.Player.Water += waterGain;

            if (State.PowersState.DemonFormStrengthPerTurn > 0)
            {
                GainStrength(State.PowersState.DemonFormStrengthPerTurn, "恶魔形态");
            }

            Log("第 " + State.Turn + " 回合开始：获得 " + State.Player.Energy + " 点能量，获得 " + waterGain + " 点水源。");
            DrawCards(5, "回合开始");
        }

        public bool CanPlay(RuntimeCard card)
        {
            return card != null
                && !State.IsWaitingForDiscard
                && State.Player.Energy >= card.CurrentEnergyCost
                && State.Player.Water >= card.Data.WaterCost;
        }

        public void PlayCard(RuntimeCard card, ElementType chosenElement = ElementType.None, bool free = false, string source = null)
        {
            if (card == null)
            {
                return;
            }

            if (State.IsWaitingForDiscard)
            {
                Log("需要先完成丢弃选择。");
                return;
            }

            if (!State.Hand.Remove(card))
            {
                return;
            }

            if (!free)
            {
                if (State.Player.Energy < card.CurrentEnergyCost || State.Player.Water < card.Data.WaterCost)
                {
                    State.Hand.Add(card);
                    Log("资源不足，无法打出 " + card.Data.Name + "。");
                    return;
                }

                State.Player.Energy -= card.CurrentEnergyCost;
                if (card.Data.WaterCost > 0)
                {
                    SpendWater(card.Data.WaterCost, card.Data.Name);
                }

                Log("打出 " + card.Data.Name + "：消耗 " + card.CurrentEnergyCost + " 能量"
                    + (card.Data.WaterCost > 0 ? "，" + card.Data.WaterCost + " 水源" : "") + "。");
            }
            else
            {
                Log((source ?? "免费") + "打出 " + card.Data.Name + "。");
            }

            ExecuteCard(card, chosenElement);
            MoveAfterPlay(card);
        }

        public void DiscardCard(RuntimeCard card)
        {
            if (card == null || !State.IsWaitingForDiscard)
            {
                return;
            }

            if (!State.Hand.Remove(card))
            {
                return;
            }

            Log("丢弃 " + card.Data.Name + "。");
            if (card.Data.HasType(CardType.Trick))
            {
                Log("奇巧触发：" + card.Data.Name + " 被主动丢弃，免费打出。");
                ExecuteCard(card, ElementType.None);
                MoveAfterPlay(card);
            }
            else
            {
                State.DiscardPile.Add(card);
            }

            State.PendingDiscardCount--;
            if (State.PendingDiscardCount <= 0)
            {
                State.PendingDiscardCount = 0;
                State.PendingDiscardSource = null;
            }
        }

        public void EndTurn()
        {
            if (State.IsWaitingForDiscard)
            {
                Log("需要先完成丢弃选择。");
                return;
            }

            ResetTemporaryCosts();

            if (State.Hand.Count > 0)
            {
                Log("结束回合：未打出的手牌进入弃牌堆：" + BattleText.CardList(State.Hand) + "。");
                State.DiscardPile.AddRange(State.Hand);
                State.Hand.Clear();
            }
            else
            {
                Log("结束回合。");
            }

            ResolvePoison();

            if (State.PowersState.ToxicCloudPoison > 0 && State.Enemy.Hp > 0)
            {
                ApplyPoison(State.PowersState.ToxicCloudPoison, "毒云弥漫");
            }

            ClearElementAttachment("敌人回合开始");

            if (!State.PowersState.Barricade)
            {
                if (State.Player.Block > 0)
                {
                    Log("格挡清空：" + State.Player.Block + " -> 0。");
                }
                State.Player.Block = 0;
            }
            else
            {
                Log("壁垒生效：保留 " + State.Player.Block + " 点格挡。");
            }

            Log("敌人行动：跳过。");
            StartTurn();
        }

        public void DrawCards(int count, string source)
        {
            int drawn = 0;
            for (int i = 0; i < count; i++)
            {
                if (State.DrawPile.Count == 0)
                {
                    if (State.DiscardPile.Count == 0)
                    {
                        Log("没有牌可抽。");
                        break;
                    }

                    State.DrawPile.AddRange(State.DiscardPile);
                    State.DiscardPile.Clear();
                    Shuffle(State.DrawPile);
                    Log("抽牌堆为空：弃牌堆洗回抽牌堆。");
                }

                var card = State.DrawPile[State.DrawPile.Count - 1];
                State.DrawPile.RemoveAt(State.DrawPile.Count - 1);
                State.Hand.Add(card);
                drawn++;
                OnDraw(card, source);
            }

            if (drawn > 0)
            {
                Log(source + "：抽 " + drawn + " 张牌。");
            }
        }

        public void AddDebugCard(CardData data)
        {
            var card = CreateRuntimeCard(data);
            card.Temporary = true;
            State.Hand.Add(card);
            Log("调试加入手牌：" + data.Name + "。");
        }

        public ElementType GetElementAttachment()
        {
            return State.Enemy.TemporaryAttachment;
        }

        private RuntimeCard CreateRuntimeCard(CardData data)
        {
            return new RuntimeCard(nextInstanceId++, data);
        }

        private void ExecuteCard(RuntimeCard card, ElementType chosenElement)
        {
            string id = card.Data.Id;
            switch (id)
            {
                case "fire_sacrifice":
                    LoseHp(6, "祭品");
                    GainEnergy(2, "祭品");
                    DrawCards(3, "祭品");
                    break;
                case "fire_demon_form":
                    State.PowersState.DemonFormStrengthPerTurn += 2;
                    Log("获得能力：恶魔形态。");
                    break;
                case "fire_hellion":
                    State.PowersState.Hellion = true;
                    Log("获得能力：地狱狂徒。");
                    break;
                case "fire_hilt_strike":
                    Attack(9, 1, ElementType.Fire, "剑柄打击");
                    DrawCards(1, "剑柄打击");
                    break;
                case "fire_not_yet":
                    HealPlayer(10);
                    break;
                case "fire_perfect_strike":
                    int strikeCount = CountCards(data => data.Name.Contains("打击"), card);
                    Attack(6 + strikeCount * 2, 1, ElementType.Fire, "完美打击（" + strikeCount + " 张打击）");
                    break;
                case "fire_double_strike":
                    Attack(5, 2, ElementType.Fire, "双重打击");
                    break;
                case "fire_bloodletting":
                    LoseHp(3, "放血");
                    GainStrength(1, "放血");
                    break;
                case "fire_barricade":
                    State.PowersState.Barricade = true;
                    Log("获得能力：壁垒。");
                    break;
                case "fire_blood_wall":
                    LoseHp(4, "血墙");
                    GainBlock(12, "血墙");
                    break;

                case "poison_blade":
                    ApplyPoison(4 * Attack(5, 1, ElementType.Poison, "毒刃").PoisonMultiplier, "毒刃", false);
                    break;
                case "poison_sneak_needle":
                    ApplyPoison(3 * Attack(6, 1, ElementType.Poison, "奇袭毒针").PoisonMultiplier, "奇袭毒针", false);
                    break;
                case "poison_prepare":
                    DrawCards(1, "准备");
                    RequestDiscard(1, "准备");
                    break;
                case "poison_roll":
                    GainBlock(7, "翻滚");
                    DrawCards(1, "翻滚");
                    RequestDiscard(1, "翻滚");
                    break;
                case "poison_fog_guard":
                    GainBlock(State.Enemy.Poison > 0 ? 10 : 6, "毒雾护身");
                    break;
                case "poison_acrobatics":
                    DrawCards(3, "杂技");
                    RequestDiscard(1, "杂技");
                    break;
                case "poison_smoke_bomb":
                    ApplyPoison(3, "毒烟弹");
                    GainBlock(6, "毒烟弹");
                    break;
                case "poison_bouncing_flask":
                    ApplyPoison(3, "弹跳毒瓶 1/3");
                    ApplyPoison(3, "弹跳毒瓶 2/3");
                    ApplyPoison(3, "弹跳毒瓶 3/3");
                    break;
                case "poison_corrosive_wave":
                    State.CorrosiveWavePoisonOnDraw += 2;
                    Log("腐蚀波：本回合每抽到一张牌，给予敌人2层中毒。");
                    break;
                case "poison_toxic_cloud":
                    State.PowersState.ToxicCloudPoison += 3;
                    Log("获得能力：毒云弥漫。");
                    break;

                case "water_blade":
                    Attack(7, 1, ElementType.Water, "水刃");
                    break;
                case "water_surge":
                    Attack(9, 1, ElementType.Water, "潮涌");
                    DrawCards(1, "潮涌");
                    break;
                case "water_curtain":
                    GainBlock(7, "水幕");
                    GainWater(1, "水幕");
                    break;
                case "water_ebb":
                    GainBlock(8, "退潮");
                    break;
                case "water_gather":
                    GainWater(2, "聚流");
                    DrawCards(1, "聚流");
                    break;
                case "water_wave_guard":
                    GainBlock(State.ReactionThisTurn ? 16 : 12, "浪涌护体");
                    break;
                case "water_spring":
                    DrawCards(3, "清泉术");
                    break;
                case "water_lake_echo":
                    State.PowersState.LakeEchoBlockPerWater += 2;
                    Log("获得能力：星湖回响。");
                    break;
                case "water_deep_burst":
                    Attack(24, 1, ElementType.Water, "深海爆发");
                    break;
                case "water_vein_resonance":
                    State.PowersState.WaterResonance = true;
                    Log("获得能力：水脉共鸣。");
                    break;

                case "color_prism":
                    AddGeneratedCard(RandomCard(data => data.Element != ElementType.Colorless), "元素棱镜");
                    break;
                case "color_blank_strike":
                    Attack(8, 1, NormalizeChosenElement(chosenElement), "空白打击(" + BattleText.ElementName(NormalizeChosenElement(chosenElement)) + ")");
                    break;
                case "color_emergency_shield":
                    GainBlock(8, "应急护盾");
                    break;
                case "color_tactical_sort":
                    DrawCards(1, "战术整理");
                    RequestDiscard(1, "战术整理");
                    break;
                case "color_sample":
                    AddGeneratedCard(RandomCard(data => data.Element != ElementType.Colorless && data.Rarity == "普通"), "元素样本");
                    break;
                case "color_harmony":
                    GainEnergy(1, "调和");
                    AddGeneratedCard(RandomCard(data => data.Element != ElementType.Colorless), "调和", -1, true);
                    break;
                case "color_neutral_arrow":
                    var reaction = Attack(6, 1, NormalizeChosenElement(chosenElement), "中和箭(" + BattleText.ElementName(NormalizeChosenElement(chosenElement)) + ")");
                    if (reaction.Triggered)
                    {
                        DrawCards(1, "中和箭反应奖励");
                    }
                    break;
                case "color_panacea":
                    GainBlock(6, "万用药剂");
                    AddGeneratedCard(RandomCard(data => data.Element != ElementType.Colorless && data.HasType(CardType.Skill)), "万用药剂");
                    break;
                case "color_tri_core":
                    State.PowersState.TriCoreLimit += 1;
                    Log("获得能力：三相核心。");
                    break;
                case "color_rift":
                    AddGeneratedCard(RandomCard(data => data.Element != ElementType.Colorless), "元素裂隙", -1, true);
                    AddGeneratedCard(RandomCard(data => data.Element != ElementType.Colorless), "元素裂隙", -1, true);
                    break;
                default:
                    Log(card.Data.Name + " 暂未实现。");
                    break;
            }
        }

        private ElementReactionResult Attack(int baseDamage, int hits, ElementType element, string source)
        {
            var reaction = ResolveElementAttack(element, source);
            int perHit = Math.Max(0, (int)Math.Floor((baseDamage + State.Player.Strength) * reaction.DamageMultiplier));
            int total = perHit * hits;
            DamageEnemy(total, source + (hits > 1 ? " " + perHit + "x" + hits : ""));
            return reaction;
        }

        private ElementReactionResult ResolveElementAttack(ElementType nextElement, string source)
        {
            var current = GetElementAttachment();
            var reaction = new ElementReactionResult();

            if (current == ElementType.None || current == nextElement)
            {
                State.Enemy.TemporaryAttachment = nextElement;

                if (current == ElementType.None)
                {
                    Log(source + "：敌人被附着" + BattleText.ElementName(nextElement) + "元素。");
                }
                else
                {
                    Log(source + "：敌人已附着" + BattleText.ElementName(nextElement) + "元素，同元素攻击后仍保持该附着。");
                }

                return reaction;
            }

            if ((current == ElementType.Fire && nextElement == ElementType.Water)
                || (current == ElementType.Water && nextElement == ElementType.Fire))
            {
                reaction.Triggered = true;
                reaction.Name = "蒸发";
                reaction.DamageMultiplier = 1.5f;
            }
            else if (current == ElementType.Fire && nextElement == ElementType.Poison)
            {
                reaction.Triggered = true;
                reaction.Name = "毒性加深";
                reaction.PoisonMultiplier = 2;
            }
            else if (current == ElementType.Poison && nextElement == ElementType.Fire)
            {
                reaction.Triggered = true;
                reaction.Name = "毒性爆发";
                TriggerToxicBurst();
            }
            else if ((current == ElementType.Poison && nextElement == ElementType.Water)
                || (current == ElementType.Water && nextElement == ElementType.Poison))
            {
                reaction.Triggered = true;
                reaction.Name = "深度中毒";
                State.Enemy.DeepPoison = true;
            }

            if (reaction.Triggered)
            {
                State.ReactionThisTurn = true;
                Log("元素反应：" + BattleText.ElementName(current) + " + " + BattleText.ElementName(nextElement) + " 触发 " + reaction.Name + "。");
                TriggerTriCore();
                ClearElementAttachment("元素反应结束");
            }

            return reaction;
        }

        private void ClearElementAttachment(string source)
        {

            if (State.Enemy.TemporaryAttachment == ElementType.None)
            {
                return;
            }

            Log(source + "：" + BattleText.ElementName(State.Enemy.TemporaryAttachment) + "元素附着消失。");
            State.Enemy.TemporaryAttachment = ElementType.None;
        }

        private void MoveAfterPlay(RuntimeCard card)
        {
            if (card.Data.HasType(CardType.Power))
            {
                State.Powers.Add(card);
                return;
            }

            if (card.Data.Exhaust)
            {
                State.ExhaustPile.Add(card);
                return;
            }

            State.DiscardPile.Add(card);
        }

        private void OnDraw(RuntimeCard card, string source)
        {
            if (State.CorrosiveWavePoisonOnDraw > 0 && source != "腐蚀波")
            {
                ApplyPoison(State.CorrosiveWavePoisonOnDraw, "腐蚀波");
            }

            if (State.PowersState.Hellion && card.Data.Name.Contains("打击"))
            {
                if (State.Hand.Remove(card))
                {
                    Log("地狱狂徒：抽到 " + card.Data.Name + "，自动免费打出。");
                    ExecuteCard(card, ElementType.None);
                    MoveAfterPlay(card);
                }
            }
        }

        private void RequestDiscard(int count, string source)
        {
            if (State.Hand.Count == 0)
            {
                Log(source + "：没有手牌可丢弃。");
                return;
            }

            State.PendingDiscardCount = Math.Min(count, State.Hand.Count);
            State.PendingDiscardSource = source;
            Log(source + "：请选择 " + State.PendingDiscardCount + " 张牌丢弃。");
        }

        private void ResolvePoison()
        {
            if (State.Enemy.Poison <= 0 || State.Enemy.Hp <= 0)
            {
                return;
            }

            int poisonDamage = State.Enemy.Poison;
            DamageEnemy(poisonDamage, "中毒结算");
            State.Enemy.Poison = Math.Max(0, State.Enemy.Poison - 1);
            Log("中毒结算：层数降低到 " + State.Enemy.Poison + "。");
        }

        private void ApplyPoison(int amount, string source, bool resolveElement = true)
        {
            int value = Math.Max(0, amount);
            if (resolveElement)
            {
                value *= ResolveElementAttack(ElementType.Poison, source).PoisonMultiplier;
            }

            if (value <= 0)
            {
                return;
            }

            State.Enemy.Poison += value;
            Log(source + "：给予 " + value + " 层中毒，当前 " + State.Enemy.Poison + " 层；中毒层数不会被当作毒元素附着。");
        }

        private void TriggerToxicBurst()
        {
            if (State.Enemy.Poison <= 0)
            {
                Log("毒性爆发：敌人没有中毒层数。");
                return;
            }

            int poisonDamage = State.Enemy.Poison;
            DamageEnemy(poisonDamage, "毒性爆发");
            State.Enemy.Poison = Math.Max(0, (int)Math.Floor(State.Enemy.Poison * 0.8f));
            Log("毒性爆发：中毒层数降低到 " + State.Enemy.Poison + "。");
        }

        private void TriggerTriCore()
        {
            if (State.PowersState.TriCoreUsed >= State.PowersState.TriCoreLimit)
            {
                return;
            }

            State.PowersState.TriCoreUsed++;
            GainEnergy(1, "三相核心");
            DrawCards(2, "三相核心");
        }

        private void SpendWater(int amount, string source)
        {
            State.Player.Water -= amount;
            if (State.PowersState.LakeEchoBlockPerWater > 0)
            {
                GainBlock(State.PowersState.LakeEchoBlockPerWater * amount, "星湖回响");
            }

            if (State.PowersState.WaterResonance && !State.WaterResonanceUsedThisTurn)
            {
                State.WaterResonanceUsedThisTurn = true;
                DrawCards(1, "水脉共鸣");
            }

            Log(source + "：消耗 " + amount + " 点水源。");
        }

        private void AddGeneratedCard(CardData data, string source, int costModifier = 0, bool temporaryCost = false)
        {
            if (data == null)
            {
                Log(source + "：没有可生成的卡。");
                return;
            }

            var card = CreateRuntimeCard(data);
            card.Temporary = true;
            card.CostModifier = costModifier;
            card.TemporaryCost = temporaryCost;
            State.Hand.Add(card);
            Log(source + "：" + data.Name + " 加入手牌。");
        }

        private CardData RandomCard(Func<CardData, bool> predicate)
        {
            var pool = CardLibrary.All.Where(predicate).ToList();
            if (pool.Count == 0)
            {
                return null;
            }

            return pool[random.Next(pool.Count)];
        }

        private void ResetTemporaryCosts()
        {
            foreach (var card in State.DrawPile.Concat(State.Hand).Concat(State.DiscardPile).Concat(State.ExhaustPile))
            {
                if (card.TemporaryCost)
                {
                    card.CostModifier = 0;
                    card.TemporaryCost = false;
                }
            }
        }

        private int CountCards(Func<CardData, bool> predicate, RuntimeCard extraCard)
        {
            int count = extraCard != null && predicate(extraCard.Data) ? 1 : 0;
            foreach (var card in State.DrawPile.Concat(State.Hand).Concat(State.DiscardPile).Concat(State.ExhaustPile).Concat(State.Powers))
            {
                if (predicate(card.Data))
                {
                    count++;
                }
            }

            return count;
        }

        private ElementType NormalizeChosenElement(ElementType element)
        {
            return element == ElementType.Fire || element == ElementType.Poison || element == ElementType.Water
                ? element
                : ElementType.Fire;
        }

        private void DamageEnemy(int amount, string source)
        {
            int value = Math.Max(0, amount);
            State.Enemy.Hp = Math.Max(0, State.Enemy.Hp - value);
            Log(source + "：敌人受到 " + value + " 点伤害。");
            if (State.Enemy.Hp == 0)
            {
                Log("敌人生命归零。可以继续测试或重开战斗。");
            }
        }

        private void LoseHp(int amount, string source)
        {
            int value = Math.Max(0, amount);
            State.Player.Hp = Math.Max(0, State.Player.Hp - value);
            Log(source + "：玩家失去 " + value + " 点生命。");
        }

        private void HealPlayer(int amount)
        {
            int before = State.Player.Hp;
            State.Player.Hp = Math.Min(State.Player.MaxHp, State.Player.Hp + amount);
            Log("回复生命：" + before + " -> " + State.Player.Hp + "。");
        }

        private void GainEnergy(int amount, string source)
        {
            State.Player.Energy += amount;
            Log(source + "：获得 " + amount + " 点能量。");
        }

        private void GainWater(int amount, string source)
        {
            State.Player.Water += amount;
            Log(source + "：获得 " + amount + " 点水源。");
        }

        private void GainStrength(int amount, string source)
        {
            State.Player.Strength += amount;
            Log(source + "：获得 " + amount + " 点力量，当前 " + State.Player.Strength + "。");
        }

        private void GainBlock(int amount, string source)
        {
            State.Player.Block += amount;
            Log(source + "：获得 " + amount + " 点格挡，当前 " + State.Player.Block + "。");
        }

        private void Shuffle<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                T temp = list[i];
                list[i] = list[j];
                list[j] = temp;
            }
        }

        private void Log(string message)
        {
            State.Log.Insert(0, message);
            if (State.Log.Count > 120)
            {
                State.Log.RemoveAt(State.Log.Count - 1);
            }
        }
    }
}



