using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ElementalSpire.TextBattlePrototype
{
    public sealed class TextBattlePrototypeRunner : MonoBehaviour
    {
        private readonly TextBattleEngine engine = new TextBattleEngine();
        private readonly List<string> deckLabels = new List<string> { "火", "毒", "水", "无色", "全部" };

        private Vector2 handScroll;
        private Vector2 poolScroll;
        private Vector2 logScroll;
        private Vector2 pileScroll;
        private int selectedDeckIndex;

        private void Start()
        {
            engine.StartNewBattle(DeckPreset.Fire);
        }

        private void OnGUI()
        {
            GUI.skin.button.wordWrap = true;
            GUI.skin.label.wordWrap = true;

            float margin = 12f;
            float topHeight = 78f;
            DrawTopBar(new Rect(margin, margin, Screen.width - margin * 2f, topHeight));

            float y = margin + topHeight + 8f;
            float leftWidth = Mathf.Max(420f, Screen.width * 0.56f);
            float rightWidth = Screen.width - leftWidth - margin * 3f;
            float height = Screen.height - y - margin;

            DrawLeftPanel(new Rect(margin, y, leftWidth, height));
            DrawRightPanel(new Rect(margin * 2f + leftWidth, y, rightWidth, height));
        }

        private void DrawTopBar(Rect rect)
        {
            GUI.Box(rect, "Elemental Spire 纯文字战斗原型");
            GUILayout.BeginArea(new Rect(rect.x + 12f, rect.y + 22f, rect.width - 24f, rect.height - 28f));
            GUILayout.BeginHorizontal();

            GUILayout.Label("起始牌组", GUILayout.Width(70f));
            selectedDeckIndex = GUILayout.SelectionGrid(selectedDeckIndex, deckLabels.ToArray(), deckLabels.Count, GUILayout.Height(28f));

            if (GUILayout.Button("新战斗", GUILayout.Width(90f), GUILayout.Height(28f)))
            {
                engine.StartNewBattle((DeckPreset)selectedDeckIndex);
            }

            if (GUILayout.Button("抽1张", GUILayout.Width(80f), GUILayout.Height(28f)))
            {
                engine.DrawCards(1, "调试抽牌");
            }

            if (GUILayout.Button("抽5张", GUILayout.Width(80f), GUILayout.Height(28f)))
            {
                engine.DrawCards(5, "调试抽牌");
            }

            if (GUILayout.Button("结束回合", GUILayout.Width(90f), GUILayout.Height(28f)))
            {
                engine.EndTurn();
            }

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void DrawLeftPanel(Rect rect)
        {
            GUI.Box(rect, string.Empty);
            GUILayout.BeginArea(new Rect(rect.x + 10f, rect.y + 10f, rect.width - 20f, rect.height - 20f));

            DrawStateSummary();
            GUILayout.Space(8f);
            DrawHand();

            GUILayout.EndArea();
        }

        private void DrawRightPanel(Rect rect)
        {
            GUI.Box(rect, string.Empty);
            GUILayout.BeginArea(new Rect(rect.x + 10f, rect.y + 10f, rect.width - 20f, rect.height - 20f));

            GUILayout.Label("调试卡牌池：点击加入手牌");
            poolScroll = GUILayout.BeginScrollView(poolScroll, GUILayout.Height(160f));
            foreach (var card in CardLibrary.All)
            {
                if (GUILayout.Button(CardButtonText(card), GUILayout.Height(42f)))
                {
                    engine.AddDebugCard(card);
                }
            }
            GUILayout.EndScrollView();

            GUILayout.Space(8f);
            DrawPiles();

            GUILayout.Space(8f);
            GUILayout.Label("战斗日志（最新在上）");
            logScroll = GUILayout.BeginScrollView(logScroll);
            foreach (string entry in engine.State.Log)
            {
                GUILayout.Label("• " + entry);
            }
            GUILayout.EndScrollView();

            GUILayout.EndArea();
        }

        private void DrawStateSummary()
        {
            var state = engine.State;
            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("玩家");
            GUILayout.Label("生命：" + state.Player.Hp + "/" + state.Player.MaxHp);
            GUILayout.Label("能量：" + state.Player.Energy + "    水源：" + state.Player.Water);
            GUILayout.Label("格挡：" + state.Player.Block + "    力量：" + state.Player.Strength);
            GUILayout.Label("能力：" + PowerText());
            GUILayout.EndVertical();

            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("敌人");
            GUILayout.Label("生命：" + state.Enemy.Hp + "/" + state.Enemy.MaxHp);
            GUILayout.Label("中毒：" + state.Enemy.Poison);
            GUILayout.Label("元素附着：" + BattleText.ElementName(engine.GetElementAttachment()));
            GUILayout.Label("深度中毒：" + (state.Enemy.DeepPoison ? "有" : "无"));
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
        }

        private void DrawHand()
        {
            var state = engine.State;
            GUILayout.BeginHorizontal();
            GUILayout.Label("手牌：" + state.Hand.Count + " 张");
            if (state.IsWaitingForDiscard)
            {
                GUILayout.Label("请选择 " + state.PendingDiscardCount + " 张牌丢弃");
            }
            GUILayout.EndHorizontal();

            handScroll = GUILayout.BeginScrollView(handScroll);
            foreach (var card in state.Hand.ToList())
            {
                GUILayout.BeginVertical(GUI.skin.box);
                GUILayout.Label(CardFullText(card));

                if (state.IsWaitingForDiscard)
                {
                    if (GUILayout.Button("丢弃", GUILayout.Height(30f)))
                    {
                        engine.DiscardCard(card);
                    }
                }
                else if (card.Data.ChooseElement)
                {
                    GUILayout.BeginHorizontal();
                    DrawPlayButton(card, ElementType.Fire, "以火打出");
                    DrawPlayButton(card, ElementType.Poison, "以毒打出");
                    DrawPlayButton(card, ElementType.Water, "以水打出");
                    GUILayout.EndHorizontal();
                }
                else
                {
                    bool canPlay = engine.CanPlay(card);
                    GUI.enabled = canPlay;
                    if (GUILayout.Button(canPlay ? "打出" : "资源不足", GUILayout.Height(30f)))
                    {
                        engine.PlayCard(card);
                    }
                    GUI.enabled = true;
                }

                GUILayout.EndVertical();
            }
            GUILayout.EndScrollView();
        }

        private void DrawPlayButton(RuntimeCard card, ElementType element, string label)
        {
            bool canPlay = engine.CanPlay(card);
            GUI.enabled = canPlay;
            if (GUILayout.Button(canPlay ? label : "资源不足", GUILayout.Height(30f)))
            {
                engine.PlayCard(card, element);
            }
            GUI.enabled = true;
        }

        private void DrawPiles()
        {
            var state = engine.State;
            GUILayout.Label("牌堆：抽牌 " + state.DrawPile.Count
                + " / 弃牌 " + state.DiscardPile.Count
                + " / 消耗 " + state.ExhaustPile.Count
                + " / 能力 " + state.Powers.Count);

            pileScroll = GUILayout.BeginScrollView(pileScroll, GUILayout.Height(120f));
            GUILayout.Label("抽牌堆：" + ShortCardList(state.DrawPile));
            GUILayout.Label("弃牌堆：" + ShortCardList(state.DiscardPile));
            GUILayout.Label("消耗区：" + ShortCardList(state.ExhaustPile));
            GUILayout.Label("能力区：" + ShortCardList(state.Powers));
            GUILayout.EndScrollView();
        }

        private string CardButtonText(CardData card)
        {
            return card.Name + "    " + BattleText.ElementName(card.Element) + " / "
                + BattleText.TypesName(card.Types) + " / " + card.EnergyCost + "费"
                + (card.WaterCost > 0 ? " / " + card.WaterCost + "水" : "");
        }

        private string CardFullText(RuntimeCard card)
        {
            return card.Data.Name + (card.Temporary ? " *" : "") + "\n"
                + BattleText.ElementName(card.Data.Element) + " / " + BattleText.TypesName(card.Data.Types)
                + " / " + card.CurrentEnergyCost + "费"
                + (card.Data.WaterCost > 0 ? " / " + card.Data.WaterCost + "水" : "")
                + "\n" + card.Data.Text;
        }

        private string ShortCardList(IEnumerable<RuntimeCard> cards)
        {
            var list = cards.Select(card => card.Data.Name).ToList();
            return list.Count == 0 ? "无" : string.Join("、", list);
        }

        private string PowerText()
        {
            var powers = engine.State.PowersState;
            var items = new List<string>();
            if (powers.DemonFormStrengthPerTurn > 0) items.Add("恶魔+" + powers.DemonFormStrengthPerTurn);
            if (powers.Hellion) items.Add("地狱狂徒");
            if (powers.Barricade) items.Add("壁垒");
            if (powers.ToxicCloudPoison > 0) items.Add("毒云" + powers.ToxicCloudPoison);
            if (powers.LakeEchoBlockPerWater > 0) items.Add("星湖" + powers.LakeEchoBlockPerWater);
            if (powers.WaterResonance) items.Add("水脉");
            if (powers.TriCoreLimit > 0) items.Add("三相" + powers.TriCoreUsed + "/" + powers.TriCoreLimit);
            return items.Count == 0 ? "无" : string.Join("，", items);
        }
    }
}

