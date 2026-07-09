using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using ElementalSpire.Cards;
using System.Linq;
/// <summary>
/// 全局游戏管理器，单例模式，跨场景不销毁
/// 存放所有全局共享的玩家数据、关卡流转数据
/// </summary>
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


    
    /// <summary>当前所处的关卡层数，从1开始</summary>
    public int currentFloor = 1;
    /// <summary>每层包含的节点数量（节点 ID 1~10 为一层）</summary>
    public const int NodesPerFloor = 10;
    /// <summary>游戏总关卡层数</summary>
    public const int MaxFloor = 3;

    [Header("元素与全局进度")]
    /// <summary>本局选中的主元素A</summary>
    public ElementType mainElementA;
    /// <summary>本局选中的主元素B</summary>
    public ElementType mainElementB;
    /// <summary>本局是否已完成开局初始化（选元素+发牌+开局选牌）</summary>
    public bool gameInitialized = false;

    private readonly HashSet<int> clearedNodeIds = new HashSet<int>();
    private readonly HashSet<int> unlockedNodeIds = new HashSet<int>();


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
        drawPile.Clear();
        handCards.Clear();
        discardPile.Clear();

        gameInitialized = false;
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
        return currentNodeId % NodesPerFloor == 0;
    }

    /// <summary>
    /// 判断是否为最后一层
    /// </summary>
    public bool IsLastFloor()
    {
        return currentFloor >= MaxFloor;
    }

    /// <summary>
    /// 推进到下一层。返回 true = 成功推进，false = 全通关，游戏胜利
    /// </summary>
    public bool AdvanceToNextFloor()
    {
        if (IsLastFloor())
        {
            // 第3关通关，回到 floor=1
            currentFloor = 1;
            isBattleWin = false;
            return false;
        }

        currentFloor++;
        isBattleWin = false;
        return true;
    }

    /// <summary>
    /// 重置一局挑战的运行时数据，主菜单重新开始时调用。
    /// </summary>
    public void ResetRunState()
    {
        playerHp = playerMaxHp;
        playerBlock = 0;
        currentEnergy = maxEnergy;
        currentFloor = 1;
        currentNodeId = 0;
        currentNodeType = string.Empty;
        isBattleWin = false;
        gameInitialized = false;
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