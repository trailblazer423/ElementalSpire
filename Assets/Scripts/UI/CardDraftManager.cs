using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ElementalSpire.Cards;
using System.Linq;

public class CardDraftManager : MonoBehaviour
{
    [Header("选牌UI引用")]
    public Button cardButton1;
    public Button cardButton2;
    public Button cardButton3;
    public Button skipButton;
    public TextMeshProUGUI progressText;// 显示“第1/3次”

    private List<CardData> _currentOptions;
    private readonly List<CardView> _cardViews = new List<CardView>();
    private TMP_FontAsset _chineseTmpFont;
    private bool _isSelected;

    void Start()
    {
        ResolveUiReferences();
        PrepareCardSlots();
        ConfigureChineseLabels();

        if (GameManager.Instance == null)
        {
            Debug.LogError("[CardDraftManager] GameManager 不存在，返回选元素场景。");
            UnityEngine.SceneManagement.SceneManager.LoadScene("ElementSelectScene");
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

    void OnDestroy()
    {
        if (skipButton != null)
            skipButton.onClick.RemoveListener(OnSkip);

        ClearCardViews();
    }

    // 开局执行三次选牌；战斗胜利后执行一次奖励选牌。
    IEnumerator DraftFlow()
    {
        ElementType eleA = GameManager.Instance.mainElementA;
        ElementType eleB = GameManager.Instance.mainElementB;
        GameManager.DraftMode mode = GameManager.Instance.currentDraftMode;

        switch (mode)
        {
            // 1. 开局三次选牌（原逻辑不变）
            case GameManager.DraftMode.InitialDraft:
                if (!GameManager.Instance.isInitialDraftDone)
                {
                    SetProgressText("第 1 / 3 次开局选牌");
                    yield return StartCoroutine(DoOneDraft(
                        CardDeckLibrary.GetInitialDraftPool(eleA, eleA), DraftPhase.Start));
                    SetProgressText("第 2 / 3 次开局选牌");
                    yield return StartCoroutine(DoOneDraft(
                        CardDeckLibrary.GetInitialDraftPool(eleB, eleB), DraftPhase.Start));
                    SetProgressText("第 3 / 3 次开局选牌");
                    yield return StartCoroutine(DoOneDraft(
                        CardDeckLibrary.GetInitialDraftPool(eleA, eleB), DraftPhase.Start));
                    GameManager.Instance.isInitialDraftDone = true;
                }
                break;

            // 2. 战斗奖励选牌（原逻辑不变）
            case GameManager.DraftMode.BattleReward:
                SetProgressText("战斗奖励：三选一");
                IEnumerable<CardData> rewardPool = CardDeckLibrary
                    .GetBattleRewardPool(eleA, GameManager.Instance.currentFloor)
                    .Concat(CardDeckLibrary.GetBattleRewardPool(eleB, GameManager.Instance.currentFloor))
                    .GroupBy(card => card.cardId)
                    .Select(group => group.First());
                yield return StartCoroutine(DoOneDraft(rewardPool, GetRewardPhase()));
                break;

            // 3. 事件奖励选牌（新增）
            case GameManager.DraftMode.EventReward:
                SetProgressText("事件奖励：三选一");
                IEnumerable<CardData> eventRewardPool = CardDeckLibrary
                    .GetBattleRewardPool(eleA, GameManager.Instance.currentFloor)
                    .Concat(CardDeckLibrary.GetBattleRewardPool(eleB, GameManager.Instance.currentFloor))
                    .GroupBy(card => card.cardId)
                    .Select(group => group.First());
                yield return StartCoroutine(DoOneDraft(eventRewardPool, GetRewardPhase()));
                break;

            // 4. 事件移除选牌（新增）
            case GameManager.DraftMode.EventRemove:
                SetProgressText("选择要舍弃的卡牌");
                // 从玩家牌库取出所有卡牌数据
                List<CardData> playerCards = new List<CardData>();
                foreach (string cardId in GameManager.Instance.playerCardBag)
                {
                    CardData card = CardDeckLibrary.GetCardDataById(cardId);
                    if (card != null) playerCards.Add(card);
                }
                // 同卡去重，只显示不同的卡牌
                playerCards = playerCards
                    .GroupBy(c => c.cardId)
                    .Select(g => g.First())
                    .ToList();
                yield return StartCoroutine(DoOneDraft(playerCards, DraftPhase.Battle1_3));
                break;
        }

        // 选牌结束，返回地图
        UnityEngine.SceneManagement.SceneManager.LoadScene("MapScene");
    }

    // 执行单次三选一
    IEnumerator DoOneDraft(IEnumerable<CardData> fullPool, DraftPhase phase)
    {
        _isSelected = false;

        _currentOptions = GetRandomCardsByRarity(
            fullPool.ToList(), 3, GameManager.Instance.currentFloor, phase);

        if (_currentOptions.Count == 0)
        {
            Debug.LogWarning("[CardDraftManager] 当前牌池没有可选牌，直接返回地图。");
            yield break;
        }

        SetDraftControlsInteractable(true);
        RefreshCardViews();

        UnityEngine.Events.UnityAction firstAction = () => SelectCard(0);
        UnityEngine.Events.UnityAction secondAction = () => SelectCard(1);
        UnityEngine.Events.UnityAction thirdAction = () => SelectCard(2);
        cardButton1.onClick.AddListener(firstAction);
        cardButton2.onClick.AddListener(secondAction);
        cardButton3.onClick.AddListener(thirdAction);

        yield return new WaitUntil(() => _isSelected);

        cardButton1.onClick.RemoveListener(firstAction);
        cardButton2.onClick.RemoveListener(secondAction);
        cardButton3.onClick.RemoveListener(thirdAction);

        ClearCardViews();
    }

    // 点击某张卡牌
    void SelectCard(int index)
    {
        if (_isSelected || index < 0 || index >= _currentOptions.Count) return;
        CardData card = _currentOptions[index];
        GameManager.DraftMode mode = GameManager.Instance.currentDraftMode;

        switch (mode)
        {
            case GameManager.DraftMode.InitialDraft:
            case GameManager.DraftMode.BattleReward:
            case GameManager.DraftMode.EventReward:
                GameManager.Instance.AddCardToBag(card.cardId);
                break;

            case GameManager.DraftMode.EventRemove:
                GameManager.Instance.RemoveCardFromBag(card.cardId);
                break;
        }

        FinishCurrentDraft();
    }

    // 点击跳过
    void OnSkip()
    {
        if (_isSelected) return;
        FinishCurrentDraft();
    }

    // 使用战斗中的 CardView 绘制奖励卡；三个旧 Button 只保留为透明点击锚点。
    void RefreshCardViews()
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

    private void SetProgressText(string value)
    {
        if (progressText != null)
        {
            ApplyChineseTmpFont(progressText);
            progressText.text = value;
        }
    }

    private DraftPhase GetRewardPhase()
    {
        int nodeId = GameManager.Instance.currentNodeId;
        if (nodeId <= 3) return DraftPhase.Battle1_3;
        if (nodeId <= 7) return DraftPhase.Battle4_7;
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

    // ===== 以下完全照搬MapManager的稀有度随机，不用改 =====
    private enum DraftPhase
    {
        Start,
        Battle1_3,
        Battle4_7,
        Battle8_10
    }

    private List<CardData> GetRandomCardsByRarity(
        List<CardData> pool, int count, int floor, DraftPhase phase)
    {
        if (pool.Count <= count) return new List<CardData>(pool);

        (int common, int rare, int precious) = phase switch
        {
            DraftPhase.Start => (80, 20, 0),
            DraftPhase.Battle1_3 => (75, 25, 0),
            DraftPhase.Battle4_7 => (55, 35, 10),
            DraftPhase.Battle8_10 => (35, 40, 25),
            _ => (80, 20, 0)
        };

        List<CardData> result = new List<CardData>();
        List<CardData> remaining = new List<CardData>(pool);

        for (int i = 0; i < count; i++)
        {
            if (remaining.Count == 0) break;

            int total = common + rare + precious;
            int roll = Random.Range(0, total);
            string targetRarity;

            if (roll < common)
                targetRarity = CardDeckLibrary.Common;
            else if (roll < common + rare)
                targetRarity = CardDeckLibrary.Rare;
            else
                targetRarity = CardDeckLibrary.Precious;

            var rarityPool = remaining.Where(c => c.rarity == targetRarity).ToList();
            if (rarityPool.Count == 0)
                rarityPool = remaining;

            CardData picked = rarityPool[Random.Range(0, rarityPool.Count)];
            result.Add(picked);
            remaining.Remove(picked);
        }

        return result;
    }
}
