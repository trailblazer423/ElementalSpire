using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using ElementalSpire.Cards;
using System.Linq;

public class MapManager : MonoBehaviour
{
    public static MapManager Instance;

    [Header("场景中所有地图节点")]
    public MapNode[] AllMapNodes;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // ========== 按钮事件统一按 OnXxxClicked 规范命名 ==========
    /// <summary>
    /// 返回主菜单按钮处理
    /// </summary>
    public void OnBackToMainMenuClicked()
    {
        SceneManager.LoadScene("MainMenuScene");
    }

    /// <summary>
    /// 游戏开局初始化流程：选元素 → 发10张基础牌 → 3次开局选牌 → 解锁节点1
    /// </summary>
    private IEnumerator GameStartFlow()
    {
        Debug.Log("[MapManager] ===== 初始化流程开始 =====");

        // 1. 直接赋值双元素，完全去掉UI等待
        ElementType eleA = ElementType.Fire;
        ElementType eleB = ElementType.Poison;
        GameManager.Instance.mainElementA = eleA;
        GameManager.Instance.mainElementB = eleB;
        Debug.Log($"[MapManager] 双元素已设置：{eleA} + {eleB}");

        // 2. 发放10张初始基础牌
        var starterCards = CardDeckLibrary.GetStarterDeck();
        foreach (var card in starterCards)
        {
            GameManager.Instance.AddCardToBag(card.cardId);
        }
        Debug.Log($"[MapManager] 初始牌发放完成，当前牌库数量：{GameManager.Instance.playerCardBag.Count}");

        // 3. 执行3次开局选牌
        Debug.Log("[MapManager] 开始第1次开局选牌（偏元素A）");
        yield return StartCoroutine(DoDraftSelect(
            CardDeckLibrary.GetInitialDraftPool(eleA, eleA),
            DraftPhase.Start));

        Debug.Log("[MapManager] 开始第2次开局选牌（偏元素B）");
        yield return StartCoroutine(DoDraftSelect(
            CardDeckLibrary.GetInitialDraftPool(eleB, eleB),
            DraftPhase.Start));

        Debug.Log("[MapManager] 开始第3次开局选牌（双元素混合）");
        yield return StartCoroutine(DoDraftSelect(
            CardDeckLibrary.GetInitialDraftPool(eleA, eleB),
            DraftPhase.Start));

        Debug.Log("[MapManager] 3次选牌全部完成");

        // 4. 解锁第1个节点
        UnlockNextNodes(0);
        Debug.Log("[MapManager] 将执行进入节点1");

        // 5. 标记初始化完成，刷新所有节点视图
        GameManager.Instance.gameInitialized = true;
        RefreshAllNodes();

        Debug.Log($"[MapManager] ===== 初始化流程结束 ===== 玩家牌库数量：{GameManager.Instance.playerCardBag.Count}");
    }

    /// <summary>
    /// 执行一次选牌（开局选牌/战斗奖励）
    /// </summary>
    private IEnumerator DoDraftSelect(IEnumerable<CardData> fullPool, DraftPhase phase, bool canSkip = true)
    {
        List<CardData> options = GetRandomCardsByRarity(
            fullPool.ToList(), 3, GameManager.Instance.currentFloor, phase);

        // 打印候选牌，方便调试验证池牌是否正确
        string cardNames = options.Count > 0
            ? string.Join("，", options.Select(c => c.cardName))
            : "无可用牌";
        Debug.Log($"[MapManager] 候选牌：{cardNames}");

        // 测试模式：默认选第一张，不等待UI
        CardData selectedCard = options.Count > 0 ? options[0] : null;

        if (selectedCard != null)
        {
            GameManager.Instance.AddCardToBag(selectedCard.cardId);
            Debug.Log($"[MapManager] 选中：{selectedCard.cardName}");
        }
        else
        {
            Debug.Log("[MapManager] 没有可选牌");
        }

        yield return null; // 只等待一帧，后续可改UI
    }

    /// <summary>
    /// 按稀有度权重从牌池中随机获取指定数量卡牌
    /// </summary>
    private List<CardData> GetRandomCardsByRarity(
        List<CardData> pool, int count, int floor, DraftPhase phase)
    {
        if (pool.Count <= count) return new List<CardData>(pool);

        // 按阶段设置稀有度权重，后续可调整参数
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

            // 通过随机数决定稀有度
            int total = common + rare + precious;
            int roll = Random.Range(0, total);
            string targetRarity;

            if (roll < common)
                targetRarity = CardDeckLibrary.Common;
            else if (roll < common + rare)
                targetRarity = CardDeckLibrary.Rare;
            else
                targetRarity = CardDeckLibrary.Precious;

            // 筛选对应稀有度的牌，没有就从全池兜底
            var rarityPool = remaining.Where(c => c.rarity == targetRarity).ToList();
            if (rarityPool.Count == 0)
                rarityPool = remaining;

            // 随机选一张，保证不重复
            CardData picked = rarityPool[Random.Range(0, rarityPool.Count)];
            result.Add(picked);
            remaining.Remove(picked);
        }

        return result;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "MapScene") return;

        EnsureRewardManager();
        EnsureGameManager();

        // 先打印状态，确认流程是否执行
        Debug.Log($"[MapManager] 地图加载完成：gameInitialized={GameManager.Instance.gameInitialized}");

        if (!GameManager.Instance.gameInitialized)
        {
            Debug.Log("[MapManager] 进入开局初始化流程");
            StartCoroutine(GameStartFlow());
            return; // 初始化完成前不执行后续逻辑
        }

        Debug.Log($"[MapManager] 地图加载：isBattleWin={GameManager.Instance?.isBattleWin}, currentFloor={GameManager.Instance?.currentFloor}, AllMapNodes数量={(AllMapNodes != null ? AllMapNodes.Length : 0)}");

        if (GameManager.Instance != null && GameManager.Instance.isBattleWin)
        {
            int winNodeId = GameManager.Instance.currentNodeId;
            bool isLastNode = GameManager.Instance.IsLastNodeOfFloor();
            Debug.Log($"[MapManager] 战斗胜利：winNodeId={winNodeId}, isLastNode={isLastNode}");

            foreach (var node in AllMapNodes)
            {
                if (node == null)
                {
                    Debug.LogWarning("[MapManager] AllMapNodes 中存在空引用，请检查 Inspector 配置");
                    continue;
                }

                if (node.NodeId == winNodeId)
                {
                    node.IsCleared = true;
                    RewardManager.Instance?.GrantReward(node.ClearReward);
                }
                else if (!isLastNode && node.NodeId == winNodeId + 1)
                {
                    node.IsUnlocked = true;
                }
            }

            if (isLastNode)
            {
                // 准备推进到下一层
                bool hasNextFloor = GameManager.Instance.AdvanceToNextFloor();
                if (!hasNextFloor)
                {
                    // 第3关通关，回到主菜单
                    Debug.Log("[MapManager] 全部3关通关，游戏胜利！");
                    SceneManager.LoadScene("MainMenuScene");
                    return;
                }

                // 进入下一层，重置所有节点（同一层10个节点）
                Debug.Log($"[MapManager] 进入第 {GameManager.Instance.currentFloor} 层");
                ResetNodesForNewFloor();
                RefreshAllNodes();
                return;
            }

            GameManager.Instance.isBattleWin = false;
        }

        RefreshAllNodes();
    }

    private void RefreshAllNodes()
    {
        foreach (var node in AllMapNodes)
        {
            if (node != null)
                node.RefreshView();
        }
    }

    /// <summary>
    /// 重置所有节点（同一层10个节点），并解锁最小NodeId的节点（节点1）
    /// </summary>
    private void ResetNodesForNewFloor()
    {
        int minNodeId = int.MaxValue;
        int resetCount = 0;

        foreach (var node in AllMapNodes)
        {
            if (node == null)
            {
                Debug.LogWarning("[MapManager] ResetNodesForNewFloor: 遇到空节点");
                continue;
            }

            node.IsCleared = false;
            node.IsUnlocked = false;
            resetCount++;

            if (node.NodeId < minNodeId)
                minNodeId = node.NodeId;
        }

        Debug.Log($"[MapManager] 重置了 {resetCount} 个节点，最小 NodeId={minNodeId}");

        if (minNodeId == int.MaxValue)
        {
            Debug.LogError("[MapManager] 没有找到任何有效节点！请检查 AllMapNodes 配置");
            return;
        }

        // 解锁最小NodeId的节点（即节点1）
        foreach (var node in AllMapNodes)
        {
            if (node != null && node.NodeId == minNodeId)
            {
                node.IsUnlocked = true;
                Debug.Log($"[MapManager] 已解锁节点 NodeId={node.NodeId}");
                break;
            }
        }
    }

    public void UnlockNextNodes(int currentNodeId)
    {
        foreach (var node in AllMapNodes)
        {
            if (node != null && node.NodeId == currentNodeId + 1)
            {
                node.IsUnlocked = true;
                node.RefreshView();
            }
        }
    }

    private void EnsureRewardManager()
    {
        if (RewardManager.Instance != null) return;
        new GameObject("RewardManager", typeof(RewardManager));
    }

    private void EnsureGameManager()
    {
        if (GameManager.Instance != null) return;
        var go = new GameObject("GameManager");
        go.AddComponent<GameManager>();
        Object.DontDestroyOnLoad(go);
        Debug.Log("[MapManager] GameManager 不存在，自动创建");
    }

    private enum DraftPhase
    {
        Start,      // 开局选牌
        Battle1_3,  // 1-3关战斗奖励
        Battle4_7,  // 4-7关战斗奖励
        Battle8_10  // 8-10关战斗奖励
    }

    /// <summary>
    /// 所有地图节点点击统一入口，占位方法，后续需补充完整逻辑
    /// </summary>
    public void OnNodeClicked(int nodeId, string nodeType)
    {
        // 临时数据：战斗节点先直接进入战斗，其他类型后续补充
        if (nodeType == "Normal" || nodeType == "Elite" || nodeType == "Boss")
        {
            GameManager.Instance.currentNodeId = nodeId;
            GameManager.Instance.currentNodeType = nodeType;
            SceneManager.LoadScene("BattleScene");
        }
    }
}
