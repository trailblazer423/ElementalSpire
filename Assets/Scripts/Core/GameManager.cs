using System.Collections;
using UnityEngine;
using System.Collections.Generic;
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
        drawPile.Clear();
        handCards.Clear();
        discardPile.Clear();
    }
}