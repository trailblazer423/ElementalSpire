using System.Collections;
using UnityEngine;
using System;
using System.Collections.Generic;
using ElementalSpire.Cards;
using System.Linq;
/// <summary>
/// 全局游戏管理器，单例模式，跨场景不销毁
/// 存放所有全局共享的玩家数据、关卡流转数据
/// </summary>
///
public class GameManager : MonoBehaviour
{
    // 全局单例
    public static GameManager Instance;

    [Header("玩家核心属性")]
    /// <summary>当前生命值</summary>
    public int playerHp;
    /// <summary>最大生命值上限</summary>
    public int playerMaxHp;
    /// <summary>玩家卡牌背包，存储所有已获得的卡牌ID（全局永久牌组）</summary>
    public List<string> playerCardBag = new List<string>();

    [Header("战斗运行时状态")]
    /// <summary>当前格挡值</summary>
    public int playerBlock;
    /// <summary>当前剩余能量</summary>
    public int currentEnergy;
    /// <summary>每回合最大能量上限</summary>
    public int maxEnergy;
    /// <summary>抽牌堆</summary>
    public List<string> drawPile = new List<string>();
    /// <summary>当前手牌</summary>
    public List<string> handCards = new List<string>();
    /// <summary>弃牌堆</summary>
    public List<string> discardPile = new List<string>();

    [Header("关卡流转数据（地图<->战斗通信用）")]
    /// <summary>当前选中的地图节点ID</summary>
    public int currentNodeId;
    /// <summary>当前节点类型：Normal/Elite/Boss/Rest/Event/Reward</summary>
    public string currentNodeType;
    /// <summary>本次战斗是否胜利，战斗组赋值，地图组读取</summary>
    public bool isBattleWin;
    /// <summary>上一场战斗结果：空字符串 / win / lose</summary>
    public string lastBattleResult = string.Empty;



    /// <summary>当前所处的关卡层数，从1开始</summary>
    public int currentFloor = 1;
    /// <summary>每层包含的节点数量（节点 ID 1~10 为一层）</summary>
    public const int NodesPerFloor = 10;
    /// <summary>当前版本为单层十节点挑战。</summary>
    public const int MaxFloor = 1;

    [Header("元素与全局进度")]
    /// <summary>本局选中的主元素A</summary>
    public ElementType mainElementA;
    /// <summary>本局选中的主元素B</summary>
    public ElementType mainElementB;
    /// <summary>本局地图节点是否已经完成首次初始化</summary>
    public bool gameInitialized = false;
    /// <summary>本局是否已经完成开局选牌；用于区分开局选牌与战斗奖励选牌</summary>
    public bool isInitialDraftDone = false;

    private readonly HashSet<int> clearedNodeIds = new HashSet<int>();
    private readonly HashSet<int> unlockedNodeIds = new HashSet<int>();

    /// <summary>
    /// 选牌场景的工作模式
    /// </summary>
    public enum DraftMode
    {
        InitialDraft,    // 开局选牌
        BattleReward,    // 战斗奖励选牌
        EventReward,     // 事件奖励：选一张加入永久牌库
        EventRemove,     // 事件移除：选一张从永久牌库删除
        RestUpgrade      // 休息节点升级：从永久牌库选择一张升级
    }

    [Header("选牌场景临时状态")]
    public DraftMode currentDraftMode;
    public bool pendingEventToClear = false; // 事件节点返回地图时结算用
    /// <summary>事件、休息或奖励完成后，由地图统一消费的一次性节点结算标记。</summary>
    public bool pendingNodeCompletion = false;
    /// <summary>本局是否已经终结；终结状态不能作为可继续的安全点。</summary>
    public bool runEnded = false;

    private void Awake()
    {
        // 单例校验：全局唯一，切场景不销毁
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            // 初始化默认数值
            InitDefaultData();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 初始化游戏开局的默认数值
    /// </summary>
    private void InitDefaultData()
    {
        currentDraftMode = DraftMode.InitialDraft;
        pendingEventToClear = false;
        pendingNodeCompletion = false;
        runEnded = false;


        playerMaxHp = 100;
        playerHp = playerMaxHp;
        playerBlock = 0;
        maxEnergy = 3; // 规则默认每回合3点能量
        currentEnergy = maxEnergy;
        // 初始卡组可由卡牌组后续配置
        playerCardBag.Clear();
        // 战斗牌堆初始化清空，战斗开始时再洗入
        currentFloor = 1;
        currentNodeId = 0;
        currentNodeType = string.Empty;
        isBattleWin = false;
        lastBattleResult = string.Empty;
        drawPile.Clear();
        handCards.Clear();
        discardPile.Clear();

        gameInitialized = false;
        isInitialDraftDone = false;
        mainElementA = ElementType.None;
        mainElementB = ElementType.None;
        clearedNodeIds.Clear();
        unlockedNodeIds.Clear();

    }

    /// <summary>
    /// 当前节点是否为当前层的最后一节，节点 ID % NodesPerFloor == 0 时为true
    /// 层内包含同一层节点（ID 1~10），每关的第10节会触发推进
    /// </summary>
    public bool IsLastNodeOfFloor()
    {
        return currentNodeId > 0 && currentNodeId % NodesPerFloor == 0;
    }

    /// <summary>
    /// 判断是否为最后一层。当前版本固定只有一层。
    /// </summary>
    public bool IsLastFloor()
    {
        return currentFloor >= MaxFloor;
    }

    /// <summary>
    /// 当前版本不再推进到下一层。完成第 10 节后返回 false，并保持最终进度。
    /// </summary>
    public bool AdvanceToNextFloor()
    {
        runEnded = true;
        isBattleWin = false;
        return false;
    }

    /// <summary>
    /// 重置一局挑战的运行时数据，主菜单重新开始时调用。
    /// </summary>
    public void ResetRunState()
    {

        BattleStatistics.EnsureExists().ResetForNewRun();

        currentDraftMode = DraftMode.InitialDraft;
        pendingEventToClear = false;
        pendingNodeCompletion = false;
        runEnded = false;

        playerMaxHp = 100;
        playerHp = playerMaxHp;
        playerBlock = 0;
        maxEnergy = 3;
        currentEnergy = maxEnergy;
        currentFloor = 1;
        currentNodeId = 0;
        currentNodeType = string.Empty;
        isBattleWin = false;
        lastBattleResult = string.Empty;
        gameInitialized = false;
        isInitialDraftDone = false;
        mainElementA = ElementType.None;
        mainElementB = ElementType.None;

        playerCardBag.Clear();
        drawPile.Clear();
        handCards.Clear();
        discardPile.Clear();
        clearedNodeIds.Clear();
        unlockedNodeIds.Clear();
    }

    /// <summary>
    /// 进入新层时清空本层地图节点进度。
    /// </summary>
    public void ResetMapProgressForCurrentFloor()
    {
        currentNodeId = 0;
        currentNodeType = string.Empty;
        clearedNodeIds.Clear();
        unlockedNodeIds.Clear();
    }

    public void MarkNodeCleared(int nodeId)
    {
        if (nodeId > 0)
            clearedNodeIds.Add(nodeId);
    }

    public void MarkNodeUnlocked(int nodeId)
    {
        if (nodeId > 0)
            unlockedNodeIds.Add(nodeId);
    }

    public bool IsNodeCleared(int nodeId)
    {
        return clearedNodeIds.Contains(nodeId);
    }

    public bool IsNodeUnlocked(int nodeId)
    {
        return unlockedNodeIds.Contains(nodeId);
    }

    /// <summary>返回当前安全进度中已完成节点的有序副本。</summary>
    public List<int> GetClearedNodeIds()
    {
        return clearedNodeIds.OrderBy(id => id).ToList();
    }

    /// <summary>返回当前安全进度中已解锁节点的有序副本。</summary>
    public List<int> GetUnlockedNodeIds()
    {
        return unlockedNodeIds.OrderBy(id => id).ToList();
    }

    /// <summary>
    /// 捕获可跨进程恢复的安全点。战斗中的手牌、抽牌堆、弃牌堆等运行时状态不会写入。
    /// </summary>
    public RunSaveData CaptureRunSaveData()
    {
        return new RunSaveData
        {
            version = RunSaveData.CurrentVersion,
            savedAtUtc = DateTime.UtcNow.ToString("o"),
            playerHp = Mathf.Clamp(playerHp, 0, Mathf.Max(1, playerMaxHp)),
            playerMaxHp = Mathf.Max(1, playerMaxHp),
            playerCardBag = new List<string>(playerCardBag ?? new List<string>()),
            currentFloor = 1,
            currentNodeId = Mathf.Clamp(currentNodeId, 0, NodesPerFloor),
            currentNodeType = currentNodeType ?? string.Empty,
            mainElementA = mainElementA,
            mainElementB = mainElementB,
            gameInitialized = gameInitialized,
            isInitialDraftDone = isInitialDraftDone,
            clearedNodeIds = GetClearedNodeIds(),
            unlockedNodeIds = GetUnlockedNodeIds(),
            currentDraftMode = DraftMode.BattleReward,
            pendingEventToClear = false,
            pendingNodeCompletion = false,
            runEnded = false
        };
    }

    /// <summary>用安全点替换本局状态，并清空所有不可恢复的战斗运行时状态。</summary>
    public void RestoreRunSaveData(RunSaveData data)
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data));

        playerMaxHp = Mathf.Max(1, data.playerMaxHp);
        playerHp = Mathf.Clamp(data.playerHp, 0, playerMaxHp);
        playerCardBag = data.playerCardBag != null
            ? new List<string>(data.playerCardBag.Where(id => !string.IsNullOrEmpty(id)))
            : new List<string>();

        currentFloor = 1;
        currentNodeId = Mathf.Clamp(data.currentNodeId, 0, NodesPerFloor);
        currentNodeType = data.currentNodeType ?? string.Empty;
        mainElementA = data.mainElementA;
        mainElementB = data.mainElementB;
        gameInitialized = data.gameInitialized;
        isInitialDraftDone = data.isInitialDraftDone;
        // 安全点永远恢复到可从地图继续的稳定状态，不信任 JSON 中可能残留的场景瞬态。
        currentDraftMode = DraftMode.BattleReward;
        pendingEventToClear = false;
        pendingNodeCompletion = false;
        runEnded = false;

        clearedNodeIds.Clear();
        if (data.clearedNodeIds != null)
        {
            foreach (int nodeId in data.clearedNodeIds)
            {
                if (nodeId > 0 && nodeId <= NodesPerFloor)
                    clearedNodeIds.Add(nodeId);
            }
        }

        unlockedNodeIds.Clear();
        if (data.unlockedNodeIds != null)
        {
            foreach (int nodeId in data.unlockedNodeIds)
            {
                if (nodeId > 0 && nodeId <= NodesPerFloor)
                    unlockedNodeIds.Add(nodeId);
            }
        }

        // 只恢复安全点，不恢复任何战斗现场。
        isBattleWin = false;
        lastBattleResult = string.Empty;
        playerBlock = 0;
        maxEnergy = 3;
        currentEnergy = maxEnergy;
        drawPile.Clear();
        handCards.Clear();
        discardPile.Clear();
    }

    /// <summary>
    /// 将一张卡牌加入玩家永久牌库（存cardId）
    /// </summary>
    public void AddCardToBag(string cardId)
    {
        if (!string.IsNullOrEmpty(cardId))
            playerCardBag.Add(cardId);
    }

    /// <summary>
    /// 从玩家永久牌库删除一张卡牌
    /// </summary>
    public bool RemoveCardFromBag(string cardId)
    {
        return playerCardBag.Remove(cardId);
    }

}
