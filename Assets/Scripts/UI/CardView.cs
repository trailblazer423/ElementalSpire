using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using ElementalSpire.Cards;

/// <summary>
/// 卡牌UI视图 - 显示单张手牌，支持点击打出或选择元素打出。
/// </summary>
public class CardView : MonoBehaviour, IPointerClickHandler
{
    private CardInstance _cardInstance;
    private CardData _cardData;

    private Text _nameText;
    private Text _costText;
    private Text _descriptionText;
    private Image _background;
    private Button[] _elementButtons = new Button[0];
    private bool _interactable = true;

    private System.Action<CardInstance, ElementType> _onClickCallback;

    private static readonly Color FireColor = new Color(0.9f, 0.3f, 0.1f);
    private static readonly Color PoisonColor = new Color(0.3f, 0.7f, 0.1f);
    private static readonly Color WaterColor = new Color(0.2f, 0.5f, 0.9f);
    private static readonly Color ColorlessColor = new Color(0.6f, 0.6f, 0.6f);

    public static CardView Create(Transform parent, CardInstance cardInstance, Font font, System.Action<CardInstance, ElementType> onClickCallback)
    {
        CardData cardData = cardInstance?.GetCardData();
        GameObject obj = new GameObject($"CardView_{cardData?.cardId ?? "unknown"}", typeof(Image), typeof(CardView));
        obj.transform.SetParent(parent, false);

        var cardView = obj.GetComponent<CardView>();
        cardView._cardInstance = cardInstance;
        cardView._cardData = cardData;
        cardView._onClickCallback = onClickCallback;
        cardView.BuildUI(font);
        return cardView;
    }

    private void BuildUI(Font font)
    {
        var rect = GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(160, 220);

        _background = GetComponent<Image>();
        _background.color = GetElementColor(_cardData != null ? _cardData.elementType : ElementType.None);

        if (_cardData == null || _cardInstance == null)
        {
            _costText = CreateSubText("CostText", "?", 18, Color.white, font,
                new Vector2(0, 88), new Vector2(140, 28));
            _nameText = CreateSubText("NameText", "未知卡牌", 16, Color.white, font,
                new Vector2(0, 58), new Vector2(140, 30));
            _descriptionText = CreateSubText("DescText", "缺少卡牌数据", 12, new Color(0.9f, 0.9f, 0.9f), font,
                new Vector2(0, -18), new Vector2(140, 104));
            return;
        }

        string costText = _cardInstance.GetWaterCost() > 0
            ? $"{_cardInstance.GetEnergyCost()}能/{_cardInstance.GetWaterCost()}水"
            : _cardInstance.GetEnergyCost().ToString();
        _costText = CreateSubText("CostText", costText, 18, Color.white, font,
            new Vector2(0, 88), new Vector2(140, 28));

        string cardName = _cardInstance.isUpgraded ? _cardData.cardName + "+" : _cardData.cardName;
        _nameText = CreateSubText("NameText", cardName, 16, Color.white, font,
            new Vector2(0, 58), new Vector2(140, 30));

        string description = _cardInstance.isUpgraded && !string.IsNullOrEmpty(_cardData.upgradeDescription)
            ? _cardData.upgradeDescription
            : _cardData.description;
        _descriptionText = CreateSubText("DescText", description, 12, new Color(0.9f, 0.9f, 0.9f), font,
            new Vector2(0, -18), new Vector2(140, _cardData.chooseElement ? 78 : 104));

        if (_cardData.exhaust)
        {
            CreateSubText("ExhaustText", "消耗", 12, Color.red, font,
                new Vector2(0, _cardData.chooseElement ? -70 : -95), new Vector2(60, 20));
        }

        if (_cardData.chooseElement)
            BuildElementButtons(font);
    }

    private void BuildElementButtons(Font font)
    {
        _elementButtons = new Button[3];
        CreateElementButton(0, "火", ElementType.Fire, FireColor, font, new Vector2(-50, -92));
        CreateElementButton(1, "毒", ElementType.Poison, PoisonColor, font, new Vector2(0, -92));
        CreateElementButton(2, "水", ElementType.Water, WaterColor, font, new Vector2(50, -92));
    }

    private void CreateElementButton(int index, string label, ElementType element, Color color, Font font, Vector2 anchoredPos)
    {
        GameObject obj = new GameObject(label + "Button", typeof(Image), typeof(Button));
        obj.transform.SetParent(transform, false);

        var rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = new Vector2(42, 26);

        obj.GetComponent<Image>().color = color;
        var button = obj.GetComponent<Button>();
        button.onClick.AddListener(() =>
        {
            if (_interactable)
                _onClickCallback?.Invoke(_cardInstance, element);
        });
        _elementButtons[index] = button;

        GameObject textObj = new GameObject("Text", typeof(Text));
        textObj.transform.SetParent(obj.transform, false);
        var txt = textObj.GetComponent<Text>();
        txt.text = label;
        txt.fontSize = 14;
        txt.color = Color.white;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.font = font;

        var textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
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

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!_interactable || _cardData == null || _cardData.chooseElement)
            return;

        _onClickCallback?.Invoke(_cardInstance, ElementType.None);
    }

    public void SetInteractable(bool interactable)
    {
        _interactable = interactable;
        _background.color = interactable
            ? GetElementColor(_cardData != null ? _cardData.elementType : ElementType.None)
            : new Color(0.3f, 0.3f, 0.3f);

        foreach (var button in _elementButtons)
        {
            if (button != null)
                button.interactable = interactable;
        }
    }

    public CardInstance CardInstance => _cardInstance;
    public CardData CardData => _cardData;
}
