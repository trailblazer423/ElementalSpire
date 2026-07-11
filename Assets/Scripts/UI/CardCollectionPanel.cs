using System;
using System.Collections.Generic;
using ElementalSpire.Cards;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Read-only card collection browser shared by the permanent deck and combat piles.
/// Cards are rendered by CardView so this panel cannot drift from the battle card face.
/// </summary>
public sealed class CardCollectionPanel : MonoBehaviour
{
    private readonly List<CardView> _cardViews = new List<CardView>();

    private Text _titleText;
    private Text _emptyText;
    private RectTransform _content;
    private Action _closeRequested;
    private Font _font;

    public void Initialize(Font font)
    {
        _font = font != null ? font : CardView.GetCompatibleFont();
        BuildUi();
        gameObject.SetActive(false);
    }

    public void Show(string title, IReadOnlyList<CardInstance> cards, Action closeRequested)
    {
        _closeRequested = closeRequested;
        ClearCards();

        int count = cards != null ? cards.Count : 0;
        _titleText.text = $"{title}（{count} 张）";
        _emptyText.gameObject.SetActive(count == 0);

        if (cards != null)
        {
            foreach (CardInstance card in cards)
            {
                if (card == null)
                    continue;

                CardView cardView = CardView.Create(_content, card, _font, null);
                var canvasGroup = cardView.gameObject.AddComponent<CanvasGroup>();
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
                _cardViews.Add(cardView);
            }
        }

        gameObject.SetActive(true);
        transform.SetAsLastSibling();
    }

    public void Hide()
    {
        gameObject.SetActive(false);
        ClearCards();
        _closeRequested = null;
    }

    private void BuildUi()
    {
        Image backdrop = gameObject.GetComponent<Image>();
        if (backdrop == null)
            backdrop = gameObject.AddComponent<Image>();
        backdrop.color = new Color(0f, 0f, 0f, 0.72f);

        RectTransform rootRect = gameObject.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        GameObject panel = CreateImage("Window", transform, new Color(0.08f, 0.09f, 0.12f, 0.98f));
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(1540f, 850f);

        _titleText = CreateText("Title", panel.transform, string.Empty, 34, Color.white, TextAnchor.MiddleLeft);
        RectTransform titleRect = _titleText.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.offsetMin = new Vector2(42f, -82f);
        titleRect.offsetMax = new Vector2(-130f, -18f);

        Button closeButton = CreateButton("CloseButton", panel.transform, "关闭", 24,
            new Color(0.72f, 0.18f, 0.16f, 1f));
        RectTransform closeRect = closeButton.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(1f, 1f);
        closeRect.anchorMax = new Vector2(1f, 1f);
        closeRect.pivot = new Vector2(1f, 1f);
        closeRect.anchoredPosition = new Vector2(-24f, -18f);
        closeRect.sizeDelta = new Vector2(100f, 58f);
        closeButton.onClick.AddListener(RequestClose);

        GameObject scrollObject = new GameObject("ScrollView", typeof(Image), typeof(ScrollRect));
        scrollObject.transform.SetParent(panel.transform, false);
        scrollObject.GetComponent<Image>().color = new Color(0.02f, 0.02f, 0.025f, 0.65f);
        RectTransform scrollRectTransform = scrollObject.GetComponent<RectTransform>();
        scrollRectTransform.anchorMin = Vector2.zero;
        scrollRectTransform.anchorMax = Vector2.one;
        scrollRectTransform.offsetMin = new Vector2(30f, 28f);
        scrollRectTransform.offsetMax = new Vector2(-30f, -98f);

        GameObject viewport = new GameObject("Viewport", typeof(Image), typeof(RectMask2D));
        viewport.transform.SetParent(scrollObject.transform, false);
        Image viewportImage = viewport.GetComponent<Image>();
        viewportImage.color = Color.clear;
        viewportImage.raycastTarget = true;
        RectTransform viewportRect = viewport.GetComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;

        GameObject contentObject = new GameObject("Content", typeof(GridLayoutGroup), typeof(ContentSizeFitter));
        contentObject.transform.SetParent(viewport.transform, false);
        _content = contentObject.GetComponent<RectTransform>();
        _content.anchorMin = new Vector2(0f, 1f);
        _content.anchorMax = new Vector2(1f, 1f);
        _content.pivot = new Vector2(0.5f, 1f);
        _content.anchoredPosition = Vector2.zero;
        _content.sizeDelta = Vector2.zero;

        GridLayoutGroup grid = contentObject.GetComponent<GridLayoutGroup>();
        grid.padding = new RectOffset(45, 45, 28, 28);
        grid.cellSize = new Vector2(160f, 220f);
        grid.spacing = new Vector2(45f, 28f);
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.UpperCenter;
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 7;

        ContentSizeFitter fitter = contentObject.GetComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect scrollRect = scrollObject.GetComponent<ScrollRect>();
        scrollRect.viewport = viewportRect;
        scrollRect.content = _content;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 42f;

        _emptyText = CreateText("EmptyText", viewport.transform, "这里还没有卡牌", 30,
            new Color(0.78f, 0.78f, 0.8f, 1f), TextAnchor.MiddleCenter);
        RectTransform emptyRect = _emptyText.rectTransform;
        emptyRect.anchorMin = Vector2.zero;
        emptyRect.anchorMax = Vector2.one;
        emptyRect.offsetMin = Vector2.zero;
        emptyRect.offsetMax = Vector2.zero;
    }

    private void RequestClose()
    {
        _closeRequested?.Invoke();
    }

    private void ClearCards()
    {
        foreach (CardView cardView in _cardViews)
        {
            if (cardView != null)
                Destroy(cardView.gameObject);
        }

        _cardViews.Clear();
    }

    private GameObject CreateImage(string objectName, Transform parent, Color color)
    {
        GameObject obj = new GameObject(objectName, typeof(Image));
        obj.transform.SetParent(parent, false);
        obj.GetComponent<Image>().color = color;
        return obj;
    }

    private Text CreateText(string objectName, Transform parent, string value, int fontSize,
        Color color, TextAnchor alignment)
    {
        GameObject obj = new GameObject(objectName, typeof(Text));
        obj.transform.SetParent(parent, false);
        Text text = obj.GetComponent<Text>();
        text.text = value;
        text.font = _font;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        return text;
    }

    private Button CreateButton(string objectName, Transform parent, string label, int fontSize, Color color)
    {
        GameObject obj = new GameObject(objectName, typeof(Image), typeof(Button));
        obj.transform.SetParent(parent, false);
        obj.GetComponent<Image>().color = color;

        Text text = CreateText("Label", obj.transform, label, fontSize, Color.white, TextAnchor.MiddleCenter);
        RectTransform textRect = text.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        return obj.GetComponent<Button>();
    }
}
