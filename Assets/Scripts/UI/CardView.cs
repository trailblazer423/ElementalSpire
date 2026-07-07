using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using ElementalSpire.Cards;

/// <summary>
/// 卡牌UI视图 - 显示单张手牌，支持点击打出
/// </summary>
public class CardView : MonoBehaviour, IPointerClickHandler
{
    [Header("卡牌数据")]
    private CardData _cardData;

    [Header("UI组件")]
    private Text _nameText;
    private Text _costText;
    private Text _descriptionText;
    private Image _background;

    private System.Action<CardData> _onClickCallback;

    // 元素颜色映射
    private static readonly Color FireColor = new Color(0.9f, 0.3f, 0.1f);
    private static readonly Color PoisonColor = new Color(0.3f, 0.7f, 0.1f);
    private static readonly Color WaterColor = new Color(0.2f, 0.5f, 0.9f);
    private static readonly Color ColorlessColor = new Color(0.6f, 0.6f, 0.6f);

    /// <summary>
    /// 创建卡牌视图
    /// </summary>
    public static CardView Create(Transform parent, CardData cardData, Font font, System.Action<CardData> onClickCallback)
    {
        GameObject obj = new GameObject($"CardView_{cardData.cardId}", typeof(Image), typeof(CardView));
        obj.transform.SetParent(parent, false);

        var cardView = obj.GetComponent<CardView>();
        cardView._cardData = cardData;
        cardView._onClickCallback = onClickCallback;
        cardView.BuildUI(font);
        return cardView;
    }

    private void BuildUI(Font font)
    {
        var rect = GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(160, 220);

        // 背景
        _background = GetComponent<Image>();
        _background.color = GetElementColor(_cardData.elementType);

        // 费用（左上角）
        _costText = CreateSubText("CostText", _cardData.cost.ToString(), 22, Color.white, font,
            new Vector2(-55, 85), new Vector2(40, 30));

        // 卡牌名
        _nameText = CreateSubText("NameText", _cardData.cardName, 16, Color.white, font,
            new Vector2(0, 50), new Vector2(140, 30));

        // 描述
        _descriptionText = CreateSubText("DescText", _cardData.description, 12, new Color(0.9f, 0.9f, 0.9f), font,
            new Vector2(0, -20), new Vector2(140, 100));

        // 如果是消耗牌，显示标记
        if (_cardData.exhaust)
        {
            var exhaustText = CreateSubText("ExhaustText", "消耗", 12, Color.red, font,
                new Vector2(0, -95), new Vector2(60, 20));
        }
    }

    private Text CreateSubText(string name, string text, int fontSize, Color color, Font font,
        Vector2 anchoredPos, Vector2 sizeDelta)
    {
        GameObject obj = new GameObject(name, typeof(Text));
        obj.transform.SetParent(transform, false);

        var txt = obj.GetComponent<Text>();
        txt.text = text;
        txt.fontSize = fontSize;
        txt.color = color;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.font = font;
        txt.horizontalOverflow = HorizontalWrapMode.Wrap;
        txt.verticalOverflow = VerticalWrapMode.Overflow;

        var rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = sizeDelta;

        return txt;
    }

    private Color GetElementColor(ElementType element)
    {
        switch (element)
        {
            case ElementType.Fire: return FireColor;
            case ElementType.Poison: return PoisonColor;
            case ElementType.Water: return WaterColor;
            default: return ColorlessColor;
        }
    }

    /// <summary>
    /// 点击卡牌
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        _onClickCallback?.Invoke(_cardData);
    }

    /// <summary>
    /// 更新卡牌可用状态
    /// </summary>
    public void SetInteractable(bool interactable)
    {
        _background.color = interactable
            ? GetElementColor(_cardData.elementType)
            : new Color(0.3f, 0.3f, 0.3f);
    }

    public CardData CardData => _cardData;
}
