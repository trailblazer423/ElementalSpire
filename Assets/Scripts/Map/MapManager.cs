using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using ElementalSpire.Cards;
using System.Linq;

public class MapManager : MonoBehaviour
{
    // 全局单例，节点脚本可通过 MapManager.Instance 调用
    public static MapManager Instance;

    [Header("场景中所有地图节点")]
    public MapNode[] AllMapNodes;

    private void Awake()
    {
        // 单例校验
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

    // ========== 按钮点击方法（符合 OnXxxClicked 规范）==========
    /// <summary>
    /// 返回主菜单按钮点击
    /// </summary>
    public void OnBackToMainMenuClicked()
    {
        SceneManager.LoadScene("MainMenuScene");
    }

    /// <summary>
    /// 游戏开局完整流程：选元素 → 发10张基础牌 → 3次自选 → 解锁第1关
    /// </summary>
    private IEnumerator GameStartFlow()
    {
        // 1. 弹出元素选择UI，等待玩家选2个元素
        ElementType eleA = ElementType.None;
        ElementType eleB = ElementType.None;
        bool elementSelected = false;

        // 和UI同学约定接口：玩家选完后回调赋值
        // 示例：UIManager.Instance.ShowElementSelectPanel((a, b) => { ... });
        // 暂时可以先用默认值测试，后续替换成真实UI调用
        eleA = ElementType.Fire;
        eleB = ElementType.Poison;
        elementSelected = true;

        yield return new WaitUntil(() => elementSelected);

        // 2. 保存选中元素到全局
        GameManager.Instance.mainElementA = eleA;
        GameManager.Instance.mainElementB = eleB;

        // 3. 发放10张初始无色牌（直接调用卡牌库现成方法）
        var starterCards = CardDeckLibrary.GetStarterDeck();
        foreach (var card in starterCards)
        {
            GameManager.Instance.AddCardToBag(card.cardId);
        }

        // 4. 执行3次开局选牌（按推荐规则：偏A → 偏B → 混合）
        // 第1次：主元素A池
        yield return StartCoroutine(DoDraftSelect(
            CardDeckLibrary.GetInitialDraftPool(eleA, eleA),
            DraftPhase.Start));

        // 第2次：主元素B池
        yield return StartCoroutine(DoDraftSelect(
            CardDeckLibrary.GetInitialDraftPool(eleB, eleB),
            DraftPhase.Start));

        // 第3次：双元素混合池
        yield return StartCoroutine(DoDraftSelect(
            CardDeckLibrary.GetInitialDraftPool(eleA, eleB),
            DraftPhase.Start));

        // 5. 解锁第1个节点（ID=1）
        UnlockNextNodes(0);

        // 6. 标记初始化完成，刷新所有节点视图
        GameManager.Instance.gameInitialized = true;
        RefreshAllNodes();
    }

    /// <summary>
    /// 执行一次三选一卡牌自选，可跳过
    /// </summary>
    private IEnumerator DoDraftSelect(IEnumerable<CardData> fullPool, DraftPhase phase, bool canSkip = true)
    {
        // 按稀有度权重随机抽3张候选牌
        List<CardData> options = GetRandomCardsByRarity(
            fullPool.ToList(), 3, GameManager.Instance.currentFloor, phase);

        CardData selectedCard = null;
        bool selectDone = false;

        // 调用UI显示三选一，玩家选择后回调
        // 示例：UIManager.Instance.ShowCardSelectPanel(options, canSkip, card => { ... });
        // 测试阶段可以默认选第一张，跳过返回null
        selectedCard = options.Count > 0 ? options[0] : null;
        selectDone = true;

        yield return new WaitUntil(() => selectDone);

        // 没跳过就加入永久牌库
        if (selectedCard != null)
        {
            GameManager.Instance.AddCardToBag(selectedCard.cardId);
        }
    }

    /// <summary>
    /// 按稀有度权重从牌池中随机抽取指定数量卡牌
    /// </summary>
    private List<CardData> GetRandomCardsByRarity(
        List<CardData> pool, int count, int floor, DraftPhase phase)
    {
        if (pool.Count <= count) return new List<CardData>(pool);

        // 按阶段设置稀有度权重，完全对应规则表
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

            // 随机决定本次稀有度
            int total = common + rare + precious;
            int roll = Random.Range(0, total);
            string targetRarity;

            if (roll < common)
                targetRarity = CardDeckLibrary.Common;
            else if (roll < common + rare)
                targetRarity = CardDeckLibrary.Rare;
            else
                targetRarity = CardDeckLibrary.Precious;

            // 筛选对应稀有度的牌，没有就从全部里兜底
            var rarityPool = remaining.Where(c => c.rarity == targetRarity).ToList();
            if (rarityPool.Count == 0)
                rarityPool = remaining;

            // 随机抽一张，避免重复
            CardData picked = rarityPool[Random.Range(0, rarityPool.Count)];
            result.Add(picked);
            remaining.Remove(picked);
        }

        return result;
    }

    // ========== 地图核心逻辑 ==========
    /// <summary>
    /// 场景加载完成时触发，战斗胜利返回时自动更新进度
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 只在加载地图场景时生效
        if (scene.name != "MapScene") return;

        EnsureRewardManager();
        EnsureGameManager();

        if (!GameManager.Instance.gameInitialized)
        {
            StartCoroutine(GameStartFlow());
            return; // 初始化完成前不执行后续逻辑
        }

        Debug.Log($"[MapManager] 地图加载，isBattleWin={GameManager.Instance?.isBattleWin}, currentFloor={GameManager.Instance?.currentFloor}, AllMapNodes数量={(AllMapNodes != null ? AllMapNodes.Length : 0)}");

        if (GameManager.Instance != null && GameManager.Instance.isBattleWin)
        {
            int winNodeId = GameManager.Instance.currentNodeId;
            bool isLastNode = GameManager.Instance.IsLastNodeOfFloor();
            Debug.Log($"[MapManager] 处理胜利，winNodeId={winNodeId}, isLastNode={isLastNode}");

            foreach (var node in AllMapNodes)
            {
                if (node == null)
                {
                    Debug.LogWarning("[MapManager] AllMapNodes 中存在空引用！请检查 Inspector 数组");
                    continue;
                }

                if (node.NodeId == winNodeId)
                {
                    node.IsCleared = true;
                    // 调用全局奖励管理器发放通关奖励
                    RewardManager.Instance.GrantReward(node.ClearReward);
                }
                else if (!isLastNode && node.NodeId == winNodeId + 1)
                {
                    node.IsUnlocked = true;
                }
            }

            if (isLastNode)
            {
                // 尝试推进到下一关
                bool hasNextFloor = GameManager.Instance.AdvanceToNextFloor();
                if (!hasNextFloor)
                {
                    // 第3关通关，回到主菜单
                    Debug.Log("[MapManager] 全部3关通关，游戏胜利！");
                    SceneManager.LoadScene("MainMenuScene");
                    return;
                }

                // 有下一关，重置所有节点（复用同一组10个节点）
                Debug.Log($"[MapManager] 进入第 {GameManager.Instance.currentFloor} 关");
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
    /// 重置所有节点（复用同一组10个节点），解锁最小NodeId的节点（节点1）。
    /// </summary>
    private void ResetNodesForNewFloor()
    {
        int minNodeId = int.MaxValue;
        int resetCount = 0;

        foreach (var node in AllMapNodes)
        {
            if (node == null)
            {
                Debug.LogWarning("[MapManager] ResetNodesForNewFloor: 遇到空引用");
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
            Debug.LogError("[MapManager] 没有找到任何有效节点！请检查 AllMapNodes 数组");
            return;
        }

        // 解锁最小NodeId的节点（即节点1）
        foreach (var node in AllMapNodes)
        {
            if (node != null && node.NodeId == minNodeId)
            {
                node.IsUnlocked = true;
                Debug.Log($"[MapManager] 解锁节点 NodeId={node.NodeId}");
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
        Debug.Log("[MapManager] GameManager 不存在，已自动创建");
    }

    private enum DraftPhase
    {
        Start,      // 开局选牌
        Battle1_3,  // 1-3关战斗奖励
        Battle4_7,  // 4-7关战斗奖励
        Battle8_10  // 8-10关战斗奖励
    }

    /// <summary>
    /// 所有地图节点点击的统一入口（占位方法，后续步骤补全逻辑）
    /// </summary>
    public void OnNodeClicked(int nodeId, string nodeType)
    {
        // 临时兼容：战斗节点照旧切场景，其他类型先不处理
        if (nodeType == "Normal" || nodeType == "Elite" || nodeType == "Boss")
        {
            GameManager.Instance.currentNodeId = nodeId;
            GameManager.Instance.currentNodeType = nodeType;
            SceneManager.LoadScene("BattleScene");
        }
    }
}
