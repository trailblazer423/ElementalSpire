using UnityEngine;
using UnityEngine.UI;

/// <summary>战斗内敌人机制说明面板。</summary>
public sealed class EnemyIntroductionPanel : MonoBehaviour
{
    private Font _font;

    public void Initialize(Font font)
    {
        _font = font != null ? font : CardView.GetCompatibleFont();
        BuildUi();
        gameObject.SetActive(false);
    }

    public void Show()
    {
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
    }

    private void BuildUi()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = 32000;
        gameObject.AddComponent<GraphicRaycaster>();

        Image backdrop = GetComponent<Image>();
        backdrop.color = new Color(0f, 0f, 0f, 0.74f);
        RectTransform root = GetComponent<RectTransform>();
        Stretch(root, Vector2.zero, Vector2.zero);

        GameObject window = CreateImage("Window", transform, new Color(0.075f, 0.08f, 0.11f, 0.99f));
        RectTransform windowRect = window.GetComponent<RectTransform>();
        windowRect.anchorMin = new Vector2(0.5f, 0.5f);
        windowRect.anchorMax = new Vector2(0.5f, 0.5f);
        windowRect.pivot = new Vector2(0.5f, 0.5f);
        windowRect.anchoredPosition = Vector2.zero;
        windowRect.sizeDelta = new Vector2(1380f, 900f);

        Text title = CreateText("Title", window.transform, "敌人机制简介", 38, Color.white, TextAnchor.MiddleLeft);
        RectTransform titleRect = title.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.offsetMin = new Vector2(38f, -78f);
        titleRect.offsetMax = new Vector2(-150f, -18f);

        Button closeButton = CreateButton("CloseButton", window.transform, "关闭", 24,
            new Color(0.72f, 0.18f, 0.16f, 1f));
        RectTransform closeRect = closeButton.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(1f, 1f);
        closeRect.anchorMax = new Vector2(1f, 1f);
        closeRect.pivot = new Vector2(1f, 1f);
        closeRect.anchoredPosition = new Vector2(-24f, -18f);
        closeRect.sizeDelta = new Vector2(108f, 56f);
        closeButton.onClick.AddListener(() => gameObject.SetActive(false));

        GameObject scrollObject = new GameObject("ScrollView", typeof(Image), typeof(ScrollRect));
        scrollObject.transform.SetParent(window.transform, false);
        scrollObject.GetComponent<Image>().color = new Color(0.02f, 0.02f, 0.03f, 0.7f);
        RectTransform scrollRect = scrollObject.GetComponent<RectTransform>();
        scrollRect.anchorMin = Vector2.zero;
        scrollRect.anchorMax = Vector2.one;
        scrollRect.offsetMin = new Vector2(30f, 28f);
        scrollRect.offsetMax = new Vector2(-30f, -98f);

        GameObject viewport = new GameObject("Viewport", typeof(Image), typeof(RectMask2D));
        viewport.transform.SetParent(scrollObject.transform, false);
        viewport.GetComponent<Image>().color = Color.clear;
        RectTransform viewportRect = viewport.GetComponent<RectTransform>();
        Stretch(viewportRect, Vector2.zero, Vector2.zero);

        GameObject content = new GameObject("Content", typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        content.transform.SetParent(viewport.transform, false);
        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0f, 0f);
        VerticalLayoutGroup contentLayout = content.GetComponent<VerticalLayoutGroup>();
        contentLayout.padding = new RectOffset(28, 28, 24, 24);
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = true;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;
        content.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        Text body = CreateText("Mechanics", content.transform, BuildIntroductionText(), 23,
            new Color(0.92f, 0.93f, 0.96f), TextAnchor.UpperLeft);
        body.horizontalOverflow = HorizontalWrapMode.Wrap;
        body.verticalOverflow = VerticalWrapMode.Overflow;
        LayoutElement layout = body.gameObject.AddComponent<LayoutElement>();
        layout.preferredHeight = body.preferredHeight + 48f;

        ScrollRect scroll = scrollObject.GetComponent<ScrollRect>();
        scroll.viewport = viewportRect;
        scroll.content = contentRect;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 42f;
    }

    private GameObject CreateImage(string name, Transform parent, Color color)
    {
        GameObject obj = new GameObject(name, typeof(Image));
        obj.transform.SetParent(parent, false);
        obj.GetComponent<Image>().color = color;
        return obj;
    }

    private Text CreateText(string name, Transform parent, string value, int fontSize, Color color,
        TextAnchor alignment)
    {
        GameObject obj = new GameObject(name, typeof(Text));
        obj.transform.SetParent(parent, false);
        Text text = obj.GetComponent<Text>();
        text.text = value;
        text.font = _font;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        return text;
    }

    private Button CreateButton(string name, Transform parent, string label, int fontSize, Color color)
    {
        GameObject obj = new GameObject(name, typeof(Image), typeof(Button));
        obj.transform.SetParent(parent, false);
        obj.GetComponent<Image>().color = color;
        Text text = CreateText("Label", obj.transform, label, fontSize, Color.white, TextAnchor.MiddleCenter);
        Stretch(text.rectTransform, new Vector2(5f, 4f), new Vector2(-5f, -4f));
        return obj.GetComponent<Button>();
    }

    private static void Stretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    private static string BuildIntroductionText()
    {
        int bossSection = IntroductionText.IndexOf("三、Boss");
        string nonBossText = bossSection >= 0 ? IntroductionText.Substring(0, bossSection) : IntroductionText;
        return nonBossText + BossIntroductionText;
    }

    private const string BossIntroductionText = @"三、Boss：疯狂戴夫（本体 HP：120）

【核心机制：花盆】
初始拥有三个花盆：豌豆射手、坚果、向日葵。任意植物死亡时，疯狂戴夫消耗20点生命值，在对应花盆重新种植该植物；新植物满血，所有成长效果重置。连续复活仍会持续消耗生命。

植物1：豌豆射手（HP：35）
攻击前先进入【瞄准】状态：第1回合瞄准、不造成伤害；第2回合发射豌豆。
伤害为 10 + 5×N（N 为攻击次数，最大为3）：第1次10点、第2次15点、第3次20点、第4次及以后25点。攻击后 N+1；死亡并复活后 N 归零。

植物2：坚果（HP：40）
被动【坚硬外壳】：坚果存在时，其他植物受到的伤害降低一半。坚果死亡后，所有植物解除伤害减免，同时疯狂戴夫获得10点护盾。

植物3：向日葵（HP：30）
被动【阳光积累】：给我方增加2点易伤，最多叠加3层。向日葵死亡时，清除我方所有易伤层数。";

    private const string IntroductionText = @"一、普通怪

1. 咕咕嘎嘎（HP：48）
技能按顺序固定循环：蓄力——给自己增加2点力量；攻击——对我方造成6点基础伤害。

2. 我的刀盾（HP：12 × 3，共三只）
盾：获得5点护盾；刀：对我方造成6点基础伤害。两者随机（0.5 : 0.5）。

3. 你已急哭（HP：50）
虚弱：给我方2层虚弱；攻击：对我方造成8点基础伤害。顺序固定：先虚弱后攻击。

4. 刘华强（HP：55）
被动：每回合前进行保熟判定；我方生命值≥50%时基础伤害减半，低于50%时基础伤害加倍。
伤害：对我方造成8点基础伤害。

5. 爻一爻（HP：60）
每回合随机卜卦（0.5 : 0.5）。阳卦：造成6点基础伤害并获得3点力量；阴卦：造成4点基础伤害并获得10点格挡。
被动：连续三次相同卦时发动攻击，造成25点基础伤害。

6. 带派雨姐（HP：30）
带派不老铁：给玩家增加6层中毒。

7. 蔡徐坤（HP：55）
铁山靠：造成8点伤害；你干嘛~：自身获得10点防御；鸡你太美：使我方下回合减少能量。

二、精英怪

1. 奶龙大军
可爱奶龙（HP：50）：大肚肚——给敌方全体5点护盾。
大奶龙（HP：45）：唐笑——给玩家1层虚弱。
奶蝠（HP：30）：开杀——造成5 + 5×N点伤害（N为回合数）。

2. 疯狂星期四（HP：120）
回合一至三：给我方施加1层虚弱。
回合四：造成25 + 25×(N-1)点伤害（N为第N次疯狂星期四），之后重新循环。

三、Boss：疯狂戴夫（本体HP：120）
被动：初始拥有三个花盆，分别种植豌豆射手、坚果、向日葵。三个植物都存在时，Wabibabu——给我方1层虚弱。
任一植物死亡时，疯狂戴夫消耗40点生命值，召唤新的满血植物。

豌豆射手（HP：30）：造成10 + 10×N点伤害（N为攻击次数；复活后重置，第一次N为0）。
坚果（HP：40）：给疯狂戴夫10点护盾。
向日葵（HP：30）：给我方2层易伤。";
}
