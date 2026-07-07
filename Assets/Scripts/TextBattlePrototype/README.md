# ElementalSpire Unity 纯文字战斗原型

目标 Unity 工程：`F:\unity\project\ElementalSpire`

## 使用方式

1. 将 `Assets/Scripts/TextBattlePrototype` 放入 Unity 项目的 `Assets/Scripts` 下。
2. 在任意场景中新建空物体，例如 `TextBattlePrototypeRunner`。
3. 挂载脚本 `TextBattlePrototypeRunner`。
4. 进入 Play Mode，即可看到纯文字战斗 UI。

## 当前实现

- 单敌人战斗。
- 敌人不会攻击。
- 抽牌堆、手牌、弃牌堆、消耗区、能力区。
- 能量、水源、生命、格挡、力量。
- 打牌、结束回合、调试抽牌、调试加牌。
- 火、毒、水元素附着和元素反应。
- 中毒层数与毒元素附着互相独立。
- 反应日志全部文字显示。

## 元素附着规则

- 使用元素攻击或施加中毒时，如果敌人当前没有元素附着，则附着本次元素。
- 如果敌人已经附着相同元素，本次效果后仍保持当前元素附着。
- 如果敌人已经附着其他元素，本次效果触发一次元素反应。
- 普通元素反应结束后，当前元素附着消失；本次元素不会继续留在敌人身上。
- 如果直到敌人回合开始时元素仍未反应，该元素附着消失。
- 中毒层数和毒元素附着互相独立：中毒不会持续提供毒元素附着。
- 毒攻击会先按毒元素尝试附着或反应，再施加中毒层数；单纯有中毒层数不会触发毒相关元素反应。

## 代码结构

- `BattleTypes.cs`：战斗枚举、状态类、运行时卡牌。
- `CardLibrary.cs`：40 张测试卡牌数据。
- `TextBattleEngine.cs`：纯 C# 战斗规则内核。
- `TextBattlePrototypeRunner.cs`：Unity `OnGUI` 文字界面入口。


