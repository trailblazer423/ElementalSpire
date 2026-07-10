using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using ElementalSpire.Cards;
using System.Linq;
using UnityEngine.UI;

public class MapManager : MonoBehaviour
{
    [Header("事件节点UI")]
    public GameObject eventChoicePanel;
    public Button btnGetCardReward;
    public Button btnRemoveCard;


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
            return;
        }

        if (btnGetCardReward != null)
            btnGetCardReward.onClick.AddListener(OnEventGetCardClicked);
        if (btnRemoveCard != null)
            btnRemoveCard.onClick.AddListener(OnEventRemoveCardClicked);
    }

    /// <summary>
    /// 点击「获得卡牌」：进入事件奖励选牌
    /// </summary>
    private void OnEventGetCardClicked()
    {
        eventChoicePanel?.SetActive(false);
        GameManager.Instance.currentDraftMode = GameManager.DraftMode.EventReward;
        GameManager.Instance.pendingEventToClear = true;
        SceneManager.LoadScene("CardDraftScene");
    }

    /// <summary>
    /// 点击「舍弃卡牌」：进入事件移除选牌
    /// </summary>
    private void OnEventRemoveCardClicked()
    {
        if (GameManager.Instance.playerCardBag.Count == 0)
        {
            Debug.LogWarning("[MapManager] 牌库为空，无法舍弃卡牌");
            return;
        }
        eventChoicePanel?.SetActive(false);
        GameManager.Instance.currentDraftMode = GameManager.DraftMode.EventRemove;
        GameManager.Instance.pendingEventToClear = true;
        SceneManager.LoadScene("CardDraftScene");
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

        if (GameManager.Instance.pendingEventToClear)
        {
            GameManager.Instance.pendingEventToClear = false;
            int nodeId = GameManager.Instance.currentNodeId;
            bool isLast = GameManager.Instance.IsLastNodeOfFloor();

            // 标记当前事件节点已完成
            GameManager.Instance.MarkNodeCleared(nodeId);
            foreach (var node in AllMapNodes)
            {
                if (node != null && node.NodeId == nodeId)
                    node.IsCleared = true;
            }

            // 解锁下一个节点
            if (!isLast)
            {
                UnlockNextNodes(nodeId);
            }
            else
            {
                bool hasNext = GameManager.Instance.AdvanceToNextFloor();
                if (!hasNext)
                {
                    ChallengeRunTracker.EnsureExists().EndRun(true);
                    SceneManager.LoadScene("MainMenuScene");
                    return;
                }
                ResetNodesForNewFloor();
            }
        }

        // 先打印状态，确认流程是否执行
        Debug.Log($"[MapManager] 地图加载完成：gameInitialized={GameManager.Instance.gameInitialized}");

       
        Debug.Log($"[MapManager] 地图加载：isBattleWin={GameManager.Instance?.isBattleWin}, currentFloor={GameManager.Instance?.currentFloor}, AllMapNodes数量={(AllMapNodes != null ? AllMapNodes.Length : 0)}");

        // ========== 新增：首次进入地图时初始化节点 ==========
        bool isFirstEnter = !GameManager.Instance.gameInitialized && !GameManager.Instance.isBattleWin;
        if (isFirstEnter)
        {
            Debug.Log("[MapManager] 新游戏首次进入地图，初始化首个节点");
            // 初始化当前楼层进度，解锁首个节点
            GameManager.Instance.ResetMapProgressForCurrentFloor();
            ResetNodesForNewFloor();
            GameManager.Instance.gameInitialized = true; // 标记游戏已初始化，避免重复执行
        }
        // ==================================================



        if (GameManager.Instance != null && GameManager.Instance.isBattleWin)
        {
            int winNodeId = GameManager.Instance.currentNodeId;
            bool isLastNode = GameManager.Instance.IsLastNodeOfFloor();
            Debug.Log($"[MapManager] 战斗胜利：winNodeId={winNodeId}, isLastNode={isLastNode}");
            ChallengeRunTracker.EnsureExists().MarkProgress(GameManager.Instance.currentFloor, winNodeId);

            foreach (var node in AllMapNodes)
            {
                if (node == null)
                {
                    Debug.LogWarning("[MapManager] AllMapNodes 中存在空引用，请检查 Inspector 配置");
                    continue;
                }

                if (node.NodeId == winNodeId)
                {
                    GameManager.Instance.MarkNodeCleared(winNodeId);
                    node.IsCleared = true;
                    RewardManager.Instance?.GrantReward(node.ClearReward);
                }
                else if (!isLastNode && node.NodeId == winNodeId + 1)
                {
                    GameManager.Instance.MarkNodeUnlocked(winNodeId + 1);
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
                    ChallengeRunTracker.EnsureExists().EndRun(true);
                    SceneManager.LoadScene("MainMenuScene");
                    return;
                }

                ChallengeRunTracker.EnsureExists().MarkProgress(GameManager.Instance.currentFloor, 0);

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

    private void ApplyPersistentNodeState()
    {
        if (GameManager.Instance == null || AllMapNodes == null)
            return;

        foreach (var node in AllMapNodes)
        {
            if (node == null)
                continue;

            node.IsCleared = GameManager.Instance.IsNodeCleared(node.NodeId);
            node.IsUnlocked = GameManager.Instance.IsNodeUnlocked(node.NodeId);
        }
    }

    private void RefreshAllNodes()
    {
        ApplyPersistentNodeState();

        if (AllMapNodes == null)
        {
            Debug.LogWarning("[MapManager] AllMapNodes 未配置，无法刷新地图节点");
            return;
        }

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

        if (GameManager.Instance != null)
            GameManager.Instance.ResetMapProgressForCurrentFloor();

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
                if (GameManager.Instance != null)
                    GameManager.Instance.MarkNodeUnlocked(minNodeId);

                node.IsUnlocked = true;
                Debug.Log($"[MapManager] 已解锁节点 NodeId={node.NodeId}");
                break;
            }
        }
    }

    public void UnlockNextNodes(int currentNodeId)
    {
        int nextNodeId = currentNodeId + 1;

        if (GameManager.Instance != null)
            GameManager.Instance.MarkNodeUnlocked(nextNodeId);

        foreach (var node in AllMapNodes)
        {
            if (node != null && node.NodeId == nextNodeId)
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
    /// 所有地图节点点击统一入口。
    /// </summary>
    public void OnNodeClicked(int nodeId, string nodeType)
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("[MapManager] GameManager 不存在，无法处理节点点击");
            return;
        }

        if (!GameManager.Instance.IsNodeUnlocked(nodeId))
        {
            Debug.LogWarning($"[MapManager] 节点 {nodeId} 尚未解锁，忽略点击");
            return;
        }

        GameManager.Instance.currentNodeId = nodeId;
        GameManager.Instance.currentNodeType = nodeType;
        ChallengeRunTracker.EnsureExists().MarkProgress(GameManager.Instance.currentFloor, nodeId);

        if (IsBattleNodeType(nodeType))
        {
            SceneManager.LoadScene("BattleScene");
            return;
        }

        Debug.LogWarning($"[MapManager] 节点 {nodeId} 的类型 {nodeType} 暂未接入，未进入战斗");
    }

    private bool IsBattleNodeType(string nodeType)
    {
        return string.IsNullOrEmpty(nodeType)
            || nodeType == "Normal"
            || nodeType == "Elite"
            || nodeType == "Boss";
    }
}
