using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ElementalSpire.Cards;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 统一处理开局选牌、战斗奖励、事件奖励、事件删牌与休息升级。
/// 奖励使用三选一；删牌和升级按玩家牌组中的实际索引展示，重复牌不会被合并。
/// </summary>
public class CardDraftManager : MonoBehaviour
{
    [Header("选牌 UI 引用")]
    public Button cardButton1;
    public Button cardButton2;
    public Button cardButton3;
    public Button skipButton;
    public TextMeshProUGUI progressText;

    private sealed class OwnedCardOption
    {
        public int BagIndex;
        public string SerializedCardId;
        public CardInstance CardInstance;
        public CardData CardData;
    }

    private List<CardData> _currentOptions = new List<CardData>();
    private readonly List<OwnedCardOption> _ownedOptions = new List<OwnedCardOption>();
    private readonly List<CardView> _cardViews = new List<CardView>();
    private TMP_FontAsset _chineseTmpFont;
    private GameObject _ownedSelectionRoot;
    private bool _isSelected;

    private void Start()
    {
        ResolveUiReferences();
        PrepareCardSlots();
        ConfigureChineseLabels();

        if (GameManager.Instance == null)
        {
            Debug.LogError("[CardDraftManager] GameManager 不存在，返回元素选择场景。");
            SceneManager.LoadScene("ElementSelectScene");
            return;
        }

        if (cardButton1 == null || cardButton2 == null || cardButton3 == null)
        {
            Debug.LogError("[CardDraftManager] 三个选牌按钮未绑定，无法开始选牌。");
            enabled = false;
            return;
        }

        if (skipButton != null)
            skipButton.onClick.AddListener(OnSkip);

        StartCoroutine(DraftFlow());
    }

    private void OnDestroy()
    {
        if (skipButton != null)
            skipButton.onClick.RemoveListener(OnSkip);

        ClearCardViews();
        ClearOwnedSelectionUi();
        _ownedOptions.Clear();
    }

    private IEnumerator DraftFlow()
    {
        GameManager gameManager = GameManager.Instance;
        ElementType elementA = gameManager.mainElementA;
        ElementType elementB = gameManager.mainElementB;
        GameManager.DraftMode mode = gameManager.currentDraftMode;

        switch (mode)
        {
            case GameManager.DraftMode.InitialDraft:
                if (!gameManager.isInitialDraftDone)
                {
                    SetProgressText("第 1 / 3 次开局选牌");
                    yield return DoOneRewardDraft(
                        CardDeckLibrary.GetInitialDraftPool(elementA, elementA), DraftPhase.Start);

                    SetProgressText("第 2 / 3 次开局选牌");
                    yield return DoOneRewardDraft(
                        CardDeckLibrary.GetInitialDraftPool(elementB, elementB), DraftPhase.Start);

                    SetProgressText("第 3 / 3 次开局选牌");
                    yield return DoOneRewardDraft(
                        CardDeckLibrary.GetInitialDraftPool(elementA, elementB), DraftPhase.Start);

                    gameManager.isInitialDraftDone = true;
                }

                // 开局流程结束后必须恢复为正常战斗奖励模式，避免首战后跳过奖励。
                gameManager.currentDraftMode = GameManager.DraftMode.BattleReward;
                SceneManager.LoadScene("MapScene");
                yield break;

            case GameManager.DraftMode.BattleReward:
                SetProgressText("战斗奖励：三选一");
                yield return DoOneRewardDraft(BuildBattleRewardPool(elementA, elementB), GetRewardPhase());
                break;

            case GameManager.DraftMode.EventReward:
                SetProgressText("事件奖励：三选一");
                yield return DoOneRewardDraft(BuildBattleRewardPool(elementA, elementB), GetRewardPhase());
                break;

            case GameManager.DraftMode.EventRemove:
                SetProgressText("选择要移除的卡牌");
                yield return DoOwnedCardDraft(upgradesOnly: false);
                break;

            case GameManager.DraftMode.RestUpgrade:
                SetProgressText("选择要升级的卡牌");
                yield return DoOwnedCardDraft(upgradesOnly: true);
                break;

            default:
                Debug.LogError($"[CardDraftManager] 未知选牌模式：{mode}，返回地图。");
                break;
        }

        gameManager.currentDraftMode = GameManager.DraftMode.BattleReward;
        RunFlowCoordinator.CompleteCurrentNodeAndReturnToMap();
    }

    private IEnumerable<CardData> BuildBattleRewardPool(ElementType elementA, ElementType elementB)
    {
        int stage = GetCurrentStage();
        return CardDeckLibrary.GetBattleRewardPool(elementA, stage)
            .Concat(CardDeckLibrary.GetBattleRewardPool(elementB, stage))
            .Where(card => card != null)
            .GroupBy(card => card.cardId)
            .Select(group => group.First());
    }

    private IEnumerator DoOneRewardDraft(IEnumerable<CardData> fullPool, DraftPhase phase)
    {
        _isSelected = false;
        SetFixedSlotsVisible(true);
        ClearOwnedSelectionUi();
        _ownedOptions.Clear();

        List<CardData> pool = fullPool == null
            ? new List<CardData>()
            : fullPool.Where(card => card != null).ToList();

        _currentOptions = GetRandomCardsByRarity(pool, 3, phase);
        if (_currentOptions.Count == 0)
        {
            Debug.LogWarning("[CardDraftManager] 当前牌池没有可选牌，本次按跳过处理。");
            FinishCurrentDraft();
            yield break;
        }

        SetDraftControlsInteractable(true);
        RefreshRewardCardViews();

        UnityEngine.Events.UnityAction firstAction = () => SelectRewardCard(0);
        UnityEngine.Events.UnityAction secondAction = () => SelectRewardCard(1);
        UnityEngine.Events.UnityAction thirdAction = () => SelectRewardCard(2);
        cardButton1.onClick.AddListener(firstAction);
        cardButton2.onClick.AddListener(secondAction);
        cardButton3.onClick.AddListener(thirdAction);

        yield return new WaitUntil(() => _isSelected);

        cardButton1.onClick.RemoveListener(firstAction);
        cardButton2.onClick.RemoveListener(secondAction);
        cardButton3.onClick.RemoveListener(thirdAction);
        ClearCardViews();
    }

    private IEnumerator DoOwnedCardDraft(bool upgradesOnly)
    {
        _isSelected = false;
        SetFixedSlotsVisible(false);
        ClearCardViews();
        BuildOwnedOptions(upgradesOnly);

        if (_ownedOptions.Count == 0)
        {
            Debug.LogWarning(upgradesOnly
                ? "[CardDraftManager] 当前牌组没有可升级卡牌，本次按跳过处理。"
                : "[CardDraftManager] 当前牌组没有可移除卡牌，本次按跳过处理。");
            FinishCurrentDraft();
            yield break;
        }

        BuildOwnedSelectionUi(upgradesOnly);
        SetDraftControlsInteractable(true);
        yield return new WaitUntil(() => _isSelected);
        ClearCardViews();
        ClearOwnedSelectionUi();
        _ownedOptions.Clear();
    }

    private void BuildOwnedOptions(bool upgradesOnly)
    {
        _ownedOptions.Clear();
        List<string> bag = GameManager.Instance.playerCardBag;
        if (bag == null)
            return;

        for (int bagIndex = 0; bagIndex < bag.Count; bagIndex++)
        {
            string serializedCardId = bag[bagIndex];
            CardInstance cardInstance = CardInstanceCodec.Decode(serializedCardId);
            CardData cardData = cardInstance?.GetCardData();
            if (cardInstance == null || cardData == null)
            {
                Debug.LogWarning($"[CardDraftManager] 跳过无法解析的牌组项 #{bagIndex + 1}：{serializedCardId}");
                continue;
            }

            if (upgradesOnly && (cardInstance.isUpgraded || !cardData.hasUpgrade))
                continue;

            _ownedOptions.Add(new OwnedCardOption
            {
                BagIndex = bagIndex,
                SerializedCardId = serializedCardId,
                CardInstance = cardInstance,
                CardData = cardData
            });
        }
    }

    private void SelectRewardCard(int optionIndex)
    {
        if (_isSelected || optionIndex < 0 || optionIndex >= _currentOptions.Count)
            return;

        CardData card = _currentOptions[optionIndex];
        GameManager.DraftMode mode = GameManager.Instance.currentDraftMode;
        if (mode == GameManager.DraftMode.InitialDraft
            || mode == GameManager.DraftMode.BattleReward
            || mode == GameManager.DraftMode.EventReward)
        {
            GameManager.Instance.AddCardToBag(CardInstanceCodec.Encode(card.cardId, false));
        }

        FinishCurrentDraft();
    }

    private void SelectOwnedCard(int optionIndex)
    {
        if (_isSelected || optionIndex < 0 || optionIndex >= _ownedOptions.Count)
            return;

        OwnedCardOption option = _ownedOptions[optionIndex];
        List<string> bag = GameManager.Instance.playerCardBag;
        if (bag == null || option.BagIndex < 0 || option.BagIndex >= bag.Count)
        {
            Debug.LogError("[CardDraftManager] 牌组在选择期间发生变化，无法完成操作。");
            return;
        }

        if (GameManager.Instance.currentDraftMode == GameManager.DraftMode.EventRemove)
        {
            // 必须按 occurrence/index 删除，不能 Remove(cardId)，否则重复牌会删错。
            bag.RemoveAt(option.BagIndex);
            Debug.Log($"[CardDraftManager] 已移除牌组第 {option.BagIndex + 1} 张：{option.SerializedCardId}");
        }
        else if (GameManager.Instance.currentDraftMode == GameManager.DraftMode.RestUpgrade)
        {
            CardInstance current = CardInstanceCodec.Decode(bag[option.BagIndex]);
            CardData data = current?.GetCardData();
            if (current == null || data == null || current.isUpgraded || !data.hasUpgrade)
            {
                Debug.LogWarning("[CardDraftManager] 所选卡牌当前不可升级，请重新选择。");
                return;
            }

            bag[option.BagIndex] = CardInstanceCodec.Encode(current.cardId, true);
            Debug.Log($"[CardDraftManager] 已升级牌组第 {option.BagIndex + 1} 张：{current.cardId}+");
        }

        FinishCurrentDraft();
    }

    private void OnSkip()
    {
        if (!_isSelected)
            FinishCurrentDraft();
    }

    private void RefreshRewardCardViews()
    {
        ClearCardViews();

        Button[] slots = { cardButton1, cardButton2, cardButton3 };
        Font cardFont = CardView.GetCompatibleFont();
        for (int index = 0; index < slots.Length; index++)
        {
            bool hasCard = index < _currentOptions.Count;
            slots[index].gameObject.SetActive(hasCard);
            if (!hasCard)
                continue;

            CardInstance cardInstance = new CardInstance(_currentOptions[index].cardId);
            CardView cardView = CardView.Create(slots[index].transform, cardInstance, cardFont, null);
            RectTransform cardRect = cardView.GetComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.anchoredPosition = Vector2.zero;
            cardRect.localScale = Vector3.one * 1.25f;

            CanvasGroup canvasGroup = cardView.gameObject.AddComponent<CanvasGroup>();
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            _cardViews.Add(cardView);
        }
    }

    private void BuildOwnedSelectionUi(bool upgradesOnly)
    {
        ClearOwnedSelectionUi();

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null && cardButton1 != null)
            canvas = cardButton1.GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("[CardDraftManager] 未找到 Canvas，无法显示牌组选择。");
            FinishCurrentDraft();
            return;
        }

        _ownedSelectionRoot = new GameObject("OwnedCardSelection", typeof(RectTransform));
        _ownedSelectionRoot.transform.SetParent(canvas.transform, false);
        RectTransform rootRect = _ownedSelectionRoot.GetComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.06f, 0.22f);
        rootRect.anchorMax = new Vector2(0.94f, 0.80f);
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        GameObject viewportObject = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        viewportObject.transform.SetParent(rootRect, false);
        RectTransform viewportRect = viewportObject.GetComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;
        Image viewportImage = viewportObject.GetComponent<Image>();
        viewportImage.color = new Color(0f, 0f, 0f, 0.16f);
        viewportObject.GetComponent<Mask>().showMaskGraphic = true;

        GameObject contentObject = new GameObject("Content", typeof(RectTransform));
        contentObject.transform.SetParent(viewportObject.transform, false);
        RectTransform contentRect = contentObject.GetComponent<RectTransform>();
        const float slotWidth = 205f;
        const float slotHeight = 285f;
        const float spacing = 18f;
        float contentWidth = Mathf.Max(viewportRect.rect.width,
            30f + _ownedOptions.Count * (slotWidth + spacing));
        contentRect.anchorMin = new Vector2(0f, 0.5f);
        contentRect.anchorMax = new Vector2(0f, 0.5f);
        contentRect.pivot = new Vector2(0f, 0.5f);
        contentRect.anchoredPosition = new Vector2(15f, 0f);
        contentRect.sizeDelta = new Vector2(contentWidth, slotHeight);

        ScrollRect scrollRect = _ownedSelectionRoot.AddComponent<ScrollRect>();
        scrollRect.viewport = viewportRect;
        scrollRect.content = contentRect;
        scrollRect.horizontal = true;
        scrollRect.vertical = false;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;
        scrollRect.scrollSensitivity = 36f;

        Font font = CardView.GetCompatibleFont();
        for (int optionIndex = 0; optionIndex < _ownedOptions.Count; optionIndex++)
        {
            OwnedCardOption option = _ownedOptions[optionIndex];
            GameObject slotObject = new GameObject(
                $"OwnedCard_{option.BagIndex}", typeof(RectTransform), typeof(Image), typeof(Button));
            slotObject.transform.SetParent(contentRect, false);
            RectTransform slotRect = slotObject.GetComponent<RectTransform>();
            slotRect.anchorMin = new Vector2(0f, 0.5f);
            slotRect.anchorMax = new Vector2(0f, 0.5f);
            slotRect.pivot = new Vector2(0f, 0.5f);
            slotRect.anchoredPosition = new Vector2(optionIndex * (slotWidth + spacing), 0f);
            slotRect.sizeDelta = new Vector2(slotWidth, slotHeight);

            Image slotImage = slotObject.GetComponent<Image>();
            slotImage.color = new Color(1f, 1f, 1f, 0.035f);
            slotObject.GetComponent<Button>().targetGraphic = slotImage;

            CardView cardView = CardView.Create(slotRect, option.CardInstance, font, null);
            RectTransform cardRect = cardView.GetComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.anchoredPosition = new Vector2(0f, 12f);
            cardRect.localScale = Vector3.one * 1.08f;
            CanvasGroup cardCanvasGroup = cardView.gameObject.AddComponent<CanvasGroup>();
            cardCanvasGroup.interactable = false;
            cardCanvasGroup.blocksRaycasts = false;
            _cardViews.Add(cardView);

            Text occurrenceLabel = CreateLegacyText(
                slotRect,
                "OccurrenceLabel",
                $"牌组第 {option.BagIndex + 1} 张",
                font,
                15,
                new Color(1f, 0.95f, 0.75f));
            RectTransform labelRect = occurrenceLabel.rectTransform;
            labelRect.anchorMin = new Vector2(0.5f, 0f);
            labelRect.anchorMax = new Vector2(0.5f, 0f);
            labelRect.pivot = new Vector2(0.5f, 0f);
            labelRect.anchoredPosition = new Vector2(0f, 2f);
            labelRect.sizeDelta = new Vector2(slotWidth, 28f);

            int capturedIndex = optionIndex;
            slotObject.GetComponent<Button>().onClick.AddListener(() => SelectOwnedCard(capturedIndex));
        }

        Debug.Log($"[CardDraftManager] 已显示 {_ownedOptions.Count} 张可{(upgradesOnly ? "升级" : "移除")}卡牌（保留重复项）。");
    }

    private static Text CreateLegacyText(
        Transform parent,
        string objectName,
        string value,
        Font font,
        int fontSize,
        Color color)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(parent, false);
        Text text = textObject.GetComponent<Text>();
        text.text = value;
        text.font = font;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = TextAnchor.MiddleCenter;
        text.raycastTarget = false;
        return text;
    }

    private void PrepareCardSlots()
    {
        foreach (Button button in new[] { cardButton1, cardButton2, cardButton3 })
        {
            if (button == null)
                continue;

            Image image = button.GetComponent<Image>();
            if (image != null)
            {
                Color color = image.color;
                color.a = 0f;
                image.color = color;
                image.raycastTarget = true;
            }

            foreach (TextMeshProUGUI text in button.GetComponentsInChildren<TextMeshProUGUI>(true))
                text.gameObject.SetActive(false);
            foreach (Text text in button.GetComponentsInChildren<Text>(true))
                text.gameObject.SetActive(false);
        }
    }

    private void ConfigureChineseLabels()
    {
        TextMeshProUGUI[] texts = FindObjectsOfType<TextMeshProUGUI>(true);
        _chineseTmpFont = texts
            .Select(text => text.font)
            .FirstOrDefault(font => font != null && font.name.Contains("KTGB2312"));

        ApplyChineseTmpFont(progressText);
        if (skipButton == null)
            return;

        TextMeshProUGUI skipTmp = skipButton.GetComponentInChildren<TextMeshProUGUI>(true);
        if (skipTmp != null)
        {
            skipTmp.text = "跳过";
            ApplyChineseTmpFont(skipTmp);
        }

        Text skipLegacy = skipButton.GetComponentInChildren<Text>(true);
        if (skipLegacy != null)
        {
            skipLegacy.text = "跳过";
            skipLegacy.font = CardView.GetCompatibleFont();
        }
    }

    private void ApplyChineseTmpFont(TextMeshProUGUI text)
    {
        if (text == null || _chineseTmpFont == null)
            return;

        text.font = _chineseTmpFont;
        text.fontSharedMaterial = _chineseTmpFont.material;
    }

    private void FinishCurrentDraft()
    {
        _isSelected = true;
        SetDraftControlsInteractable(false);
    }

    private void SetDraftControlsInteractable(bool interactable)
    {
        foreach (Button button in new[] { cardButton1, cardButton2, cardButton3 })
        {
            if (button != null)
                button.interactable = interactable && button.gameObject.activeSelf;
        }

        if (skipButton != null)
            skipButton.interactable = interactable;

        if (_ownedSelectionRoot != null)
        {
            foreach (Button button in _ownedSelectionRoot.GetComponentsInChildren<Button>(true))
                button.interactable = interactable;
        }
    }

    private void SetFixedSlotsVisible(bool visible)
    {
        foreach (Button button in new[] { cardButton1, cardButton2, cardButton3 })
        {
            if (button != null)
                button.gameObject.SetActive(visible);
        }
    }

    private void ClearCardViews()
    {
        foreach (CardView cardView in _cardViews)
        {
            if (cardView == null)
                continue;

            cardView.gameObject.SetActive(false);
            Destroy(cardView.gameObject);
        }

        _cardViews.Clear();
    }

    private void ClearOwnedSelectionUi()
    {
        if (_ownedSelectionRoot == null)
            return;

        _ownedSelectionRoot.SetActive(false);
        Destroy(_ownedSelectionRoot);
        _ownedSelectionRoot = null;
    }

    private void SetProgressText(string value)
    {
        if (progressText == null)
            return;

        ApplyChineseTmpFont(progressText);
        progressText.text = value;
    }

    private int GetCurrentStage()
    {
        if (GameManager.Instance == null)
            return 1;

        return Mathf.Clamp(
            GameManager.Instance.currentNodeId > 0
                ? GameManager.Instance.currentNodeId
                : GameManager.Instance.currentFloor,
            1,
            GameManager.NodesPerFloor);
    }

    private DraftPhase GetRewardPhase()
    {
        int stage = GetCurrentStage();
        if (stage <= 3) return DraftPhase.Battle1_3;
        if (stage <= 7) return DraftPhase.Battle4_7;
        return DraftPhase.Battle8_10;
    }

    private void ResolveUiReferences()
    {
        Button[] buttons = FindObjectsOfType<Button>(true);
        cardButton1 = cardButton1 != null ? cardButton1 : buttons.FirstOrDefault(button => button.name == "CardButton1");
        cardButton2 = cardButton2 != null ? cardButton2 : buttons.FirstOrDefault(button => button.name == "CardButton2");
        cardButton3 = cardButton3 != null ? cardButton3 : buttons.FirstOrDefault(button => button.name == "CardButton3");
        skipButton = skipButton != null ? skipButton : buttons.FirstOrDefault(button => button.name == "SkipButton");

        if (progressText == null)
        {
            progressText = FindObjectsOfType<TextMeshProUGUI>(true)
                .FirstOrDefault(text => text.gameObject.name == "ProgressText");
        }
    }

    private enum DraftPhase
    {
        Start,
        Battle1_3,
        Battle4_7,
        Battle8_10
    }

    private static List<CardData> GetRandomCardsByRarity(
        List<CardData> pool,
        int count,
        DraftPhase phase)
    {
        if (pool.Count <= count)
            return new List<CardData>(pool);

        int common;
        int rare;
        int precious;
        switch (phase)
        {
            case DraftPhase.Battle1_3:
                common = 75; rare = 25; precious = 0;
                break;
            case DraftPhase.Battle4_7:
                common = 55; rare = 35; precious = 10;
                break;
            case DraftPhase.Battle8_10:
                common = 35; rare = 40; precious = 25;
                break;
            default:
                common = 80; rare = 20; precious = 0;
                break;
        }

        List<CardData> result = new List<CardData>();
        List<CardData> remaining = new List<CardData>(pool);
        for (int index = 0; index < count && remaining.Count > 0; index++)
        {
            int roll = UnityEngine.Random.Range(0, common + rare + precious);
            string targetRarity = roll < common
                ? CardDeckLibrary.Common
                : roll < common + rare
                    ? CardDeckLibrary.Rare
                    : CardDeckLibrary.Precious;

            List<CardData> rarityPool = remaining
                .Where(card => card.rarity == targetRarity)
                .ToList();
            if (rarityPool.Count == 0)
                rarityPool = remaining;

            CardData picked = rarityPool[UnityEngine.Random.Range(0, rarityPool.Count)];
            result.Add(picked);
            remaining.Remove(picked);
        }

        return result;
    }
}
