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
    private bool _isSelected;

    void Start()
    {
        skipButton.onClick.AddListener(OnSkip);
        StartCoroutine(DraftFlow());
    }

    // 完整的3次选牌流程
    IEnumerator DraftFlow()
    {
        ElementType eleA = GameManager.Instance.mainElementA;
        ElementType eleB = GameManager.Instance.mainElementB;

        // 第1次：偏向元素A
        progressText.text = "第 1 / 3 次选牌";
        yield return StartCoroutine(DoOneDraft(
            CardDeckLibrary.GetInitialDraftPool(eleA, eleA)));

        // 第2次：偏向元素B
        progressText.text = "第 2 / 3 次选牌";
        yield return StartCoroutine(DoOneDraft(
            CardDeckLibrary.GetInitialDraftPool(eleB, eleB)));

        // 第3次：双元素混合
        progressText.text = "第 3 / 3 次选牌";
        yield return StartCoroutine(DoOneDraft(
            CardDeckLibrary.GetInitialDraftPool(eleA, eleB)));

        // 全部选完，标记初始化完成，跳转地图场景
        GameManager.Instance.gameInitialized = true;
        UnityEngine.SceneManagement.SceneManager.LoadScene("MapScene");
    }

    // 执行单次三选一
    IEnumerator DoOneDraft(IEnumerable<CardData> fullPool)
    {
        _isSelected = false;

        // 按稀有度权重随机出3张候选牌（算法和你MapManager里完全一样）
        _currentOptions = GetRandomCardsByRarity(
            fullPool.ToList(), 3, GameManager.Instance.currentFloor, DraftPhase.Start);

        // 更新3个按钮的卡牌显示
        RefreshCardButtons();

        // 绑定按钮点击事件
        cardButton1.onClick.AddListener(() => { SelectCard(0); });
        cardButton2.onClick.AddListener(() => { SelectCard(1); });
        cardButton3.onClick.AddListener(() => { SelectCard(2); });

        // 等待玩家选择或跳过
        yield return new WaitUntil(() => _isSelected);

        // 移除本次监听，避免下次选牌重复触发
        cardButton1.onClick.RemoveAllListeners();
        cardButton2.onClick.RemoveAllListeners();
        cardButton3.onClick.RemoveAllListeners();
    }

    // 点击某张卡牌
    void SelectCard(int index)
    {
        if (index < 0 || index >= _currentOptions.Count) return;
        CardData card = _currentOptions[index];
        GameManager.Instance.AddCardToBag(card.cardId);
        _isSelected = true;
    }

    // 点击跳过
    void OnSkip()
    {
        _isSelected = true;
    }

    // 更新三个按钮的卡牌文字显示
    void RefreshCardButtons()
    {
        TextMeshProUGUI t1 = cardButton1.GetComponentInChildren<TextMeshProUGUI>();
        TextMeshProUGUI t2 = cardButton2.GetComponentInChildren<TextMeshProUGUI>();
        TextMeshProUGUI t3 = cardButton3.GetComponentInChildren<TextMeshProUGUI>();

        t1.text = _currentOptions.Count > 0 ? _currentOptions[0].cardName : "";
        t2.text = _currentOptions.Count > 1 ? _currentOptions[1].cardName : "";
        t3.text = _currentOptions.Count > 2 ? _currentOptions[2].cardName : "";
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
