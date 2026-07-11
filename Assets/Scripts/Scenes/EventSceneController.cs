using System;
using ElementalSpire.Cards;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 事件节点：在“获得一张奖励牌”和“移除一张现有牌”之间二选一。
/// 具体选牌在 CardDraftScene 中完成，完成后由 RunFlowCoordinator 统一结算节点。
/// </summary>
public sealed class EventSceneController : ChoiceSceneControllerBase
{
    private bool _choiceCommitted;

    private void Start()
    {
        BuildChoiceScreen("神秘事件", "选择本次事件的效果");
        CreateChoiceButton(
            "RewardChoiceButton",
            "获得卡牌\n从战斗奖励牌池三选一",
            new Vector2(-230f, -30f),
            ChooseReward);

        Button removeButton = CreateChoiceButton(
            "RemoveChoiceButton",
            "移除卡牌\n从当前牌组选择一张",
            new Vector2(230f, -30f),
            ChooseRemove);

        bool canRemove = GameManager.Instance != null
            && GameManager.Instance.playerCardBag != null
            && GameManager.Instance.playerCardBag.Count > 0;
        removeButton.interactable = canRemove;
        if (!canRemove)
            SetButtonLabel(removeButton, "移除卡牌\n当前牌组为空");
    }

    public void ChooseReward()
    {
        if (!TryCommitChoice())
            return;

        GameManager.Instance.currentDraftMode = GameManager.DraftMode.EventReward;
        GameManager.Instance.pendingEventToClear = false;
        GameManager.Instance.pendingNodeCompletion = true;
        SceneManager.LoadScene("CardDraftScene");
    }

    public void ChooseRemove()
    {
        if (GameManager.Instance == null
            || GameManager.Instance.playerCardBag == null
            || GameManager.Instance.playerCardBag.Count == 0)
        {
            Debug.LogWarning("[EventSceneController] 当前牌组为空，不能执行移除卡牌事件。");
            return;
        }

        if (!TryCommitChoice())
            return;

        GameManager.Instance.currentDraftMode = GameManager.DraftMode.EventRemove;
        GameManager.Instance.pendingEventToClear = false;
        GameManager.Instance.pendingNodeCompletion = true;
        SceneManager.LoadScene("CardDraftScene");
    }

    private bool TryCommitChoice()
    {
        if (_choiceCommitted)
            return false;

        if (GameManager.Instance == null)
        {
            Debug.LogError("[EventSceneController] GameManager 不存在，无法处理事件。");
            return false;
        }

        _choiceCommitted = true;
        SetAllChoicesInteractable(false);
        return true;
    }
}

/// <summary>
/// Event/Rest 共用的轻量运行时 UI。可直接挂在只有 Camera 的最小场景中；
/// 若场景已经带背景 Canvas，则保留原背景，仅在其上创建选择面板。
/// </summary>
public abstract class ChoiceSceneControllerBase : MonoBehaviour
{
    private RectTransform _panel;
    private Font _font;

    protected void BuildChoiceScreen(string title, string description)
    {
        EnsureEventSystem();
        _font = CardView.GetCompatibleFont();
        Canvas canvas = FindObjectOfType<Canvas>();
        bool createdCanvas = canvas == null;
        if (createdCanvas)
            canvas = CreateCanvas();

        GameObject rootObject = new GameObject("RuntimeChoiceUI", typeof(RectTransform));
        rootObject.transform.SetParent(canvas.transform, false);
        RectTransform rootRect = rootObject.GetComponent<RectTransform>();
        Stretch(rootRect);

        Text sceneHeader = CreateText(rootRect, "SceneHeader", title, 44, Color.white);
        RectTransform sceneHeaderRect = sceneHeader.rectTransform;
        sceneHeaderRect.anchorMin = new Vector2(0.08f, 0.84f);
        sceneHeaderRect.anchorMax = new Vector2(0.92f, 0.99f);
        sceneHeaderRect.offsetMin = Vector2.zero;
        sceneHeaderRect.offsetMax = Vector2.zero;
        sceneHeader.fontStyle = FontStyle.Bold;

        if (createdCanvas)
        {
            Image fallbackBackground = CreateImage(rootRect, "FallbackBackground",
                new Color(0.35f, 0.20f, 0.10f, 1f));
            Stretch(fallbackBackground.rectTransform);
        }

        Image panelImage = CreateImage(rootRect, "ChoicePanel", new Color(0.96f, 0.91f, 0.78f, 0.96f));
        _panel = panelImage.rectTransform;
        _panel.anchorMin = new Vector2(0.13f, 0.18f);
        _panel.anchorMax = new Vector2(0.87f, 0.82f);
        _panel.offsetMin = Vector2.zero;
        _panel.offsetMax = Vector2.zero;

        Text titleText = CreateText(_panel, "Prompt", description, 30, new Color(0.2f, 0.12f, 0.06f));
        RectTransform titleRect = titleText.rectTransform;
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -42f);
        titleRect.sizeDelta = new Vector2(-60f, 100f);
    }

    protected Button CreateChoiceButton(
        string objectName,
        string label,
        Vector2 anchoredPosition,
        Action onClick)
    {
        if (_panel == null)
            throw new InvalidOperationException("BuildChoiceScreen must be called before creating buttons.");

        GameObject buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(_panel, false);
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.anchoredPosition = anchoredPosition;
        buttonRect.sizeDelta = new Vector2(350f, 150f);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.36f, 0.23f, 0.12f, 0.95f);

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.88f, 0.58f, 1f);
        colors.pressedColor = new Color(0.82f, 0.70f, 0.42f, 1f);
        colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.6f);
        button.colors = colors;
        button.onClick.AddListener(() => onClick?.Invoke());

        Text labelText = CreateText(buttonRect, "Label", label, 25, Color.white);
        Stretch(labelText.rectTransform);
        labelText.rectTransform.offsetMin = new Vector2(18f, 10f);
        labelText.rectTransform.offsetMax = new Vector2(-18f, -10f);
        return button;
    }

    protected void SetButtonLabel(Button button, string label)
    {
        if (button == null)
            return;

        Text text = button.GetComponentInChildren<Text>(true);
        if (text != null)
            text.text = label;
    }

    protected void SetAllChoicesInteractable(bool interactable)
    {
        if (_panel == null)
            return;

        foreach (Button button in _panel.GetComponentsInChildren<Button>(true))
            button.interactable = interactable;
    }

    private Canvas CreateCanvas()
    {
        GameObject canvasObject = new GameObject(
            "RuntimeChoiceCanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    private static void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null)
            return;

        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    private Image CreateImage(Transform parent, string objectName, Color color)
    {
        GameObject imageObject = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        return image;
    }

    private Text CreateText(
        Transform parent,
        string objectName,
        string value,
        int fontSize,
        Color color)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(parent, false);
        Text text = textObject.GetComponent<Text>();
        text.text = value;
        text.font = _font != null ? _font : CardView.GetCompatibleFont();
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = TextAnchor.MiddleCenter;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        return text;
    }

    private static void Stretch(RectTransform rectTransform)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
    }
}
