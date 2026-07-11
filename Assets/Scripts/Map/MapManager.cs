using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 单路线十节点地图控制器。
/// 节点顺序固定为：战斗、战斗、事件、休息、战斗、战斗、战斗、事件、休息、战斗。
/// 所有跨场景进度都由 GameManager 保存；场景对象只负责显示与入口校验。
/// </summary>
public class MapManager : MonoBehaviour
{
    public static MapManager Instance;

    [Header("场景中所有地图节点")]
    public MapNode[] AllMapNodes;

    private static readonly string[] NodeTypes =
    {
        string.Empty,
        "Normal",
        "Normal",
        "Event",
        "Rest",
        "Normal",
        "Normal",
        "Normal",
        "Event",
        "Rest",
        "Normal"
    };

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        EnsureGameManager();

        // main 曾把旧事件二选一面板误设为默认激活；事件现在有独立场景，地图中必须关闭它。
        GameObject legacyEventPanel = GameObject.Find("EventChoicePanel");
        if (legacyEventPanel != null)
            legacyEventPanel.SetActive(false);
    }

    private void Start()
    {
        ConfigureFixedNodeTypes();
        InitializeMapIfNeeded();
        RefreshAllNodes();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public static string GetNodeType(int nodeId)
    {
        return nodeId >= 1 && nodeId <= GameManager.NodesPerFloor
            ? NodeTypes[nodeId]
            : string.Empty;
    }

    public static string GetSceneForNode(int nodeId)
    {
        string nodeType = GetNodeType(nodeId);
        if (nodeType == "Event") return "EventScene";
        if (nodeType == "Rest") return "RestScene";
        return "BattleScene";
    }

    /// <summary>
    /// 当前唯一允许挑战的节点。已完成节点永远不会再次成为入口。
    /// </summary>
    public static int GetExpectedNodeId(GameManager gameManager)
    {
        if (gameManager == null)
            return 0;

        for (int nodeId = 1; nodeId <= GameManager.NodesPerFloor; nodeId++)
        {
            if (!gameManager.IsNodeCleared(nodeId))
                return nodeId;
        }

        return 0;
    }

    public void OnNodeClicked(int nodeId, string ignoredSceneNodeType)
    {
        GameManager gameManager = GameManager.Instance;
        if (gameManager == null)
        {
            Debug.LogError("[MapManager] GameManager 不存在，无法进入节点。");
            return;
        }

        if (nodeId < 1 || nodeId > GameManager.NodesPerFloor)
        {
            Debug.LogWarning($"[MapManager] 非法节点 {nodeId}，已忽略。");
            return;
        }

        int expectedNodeId = GetExpectedNodeId(gameManager);
        if (gameManager.IsNodeCleared(nodeId)
            || !gameManager.IsNodeUnlocked(nodeId)
            || nodeId != expectedNodeId)
        {
            Debug.LogWarning(
                $"[MapManager] 节点 {nodeId} 不可进入：cleared={gameManager.IsNodeCleared(nodeId)}, "
                + $"unlocked={gameManager.IsNodeUnlocked(nodeId)}, expected={expectedNodeId}。");
            RefreshAllNodes();
            return;
        }

        int previousFloor = gameManager.currentFloor;
        int previousNodeId = gameManager.currentNodeId;
        string previousNodeType = gameManager.currentNodeType;
        bool previousBattleWin = gameManager.isBattleWin;
        string previousBattleResult = gameManager.lastBattleResult;

        gameManager.currentFloor = 1;
        gameManager.currentNodeId = nodeId;
        gameManager.currentNodeType = GetNodeType(nodeId);
        gameManager.isBattleWin = false;
        gameManager.lastBattleResult = string.Empty;

        // 进入节点前写入地图安全点。战斗/事件/休息中的退出只能回到这份快照。
        if (!RunSaveRepository.SaveSafePoint())
        {
            gameManager.currentFloor = previousFloor;
            gameManager.currentNodeId = previousNodeId;
            gameManager.currentNodeType = previousNodeType;
            gameManager.isBattleWin = previousBattleWin;
            gameManager.lastBattleResult = previousBattleResult;
            Debug.LogError($"[MapManager] 节点 {nodeId} 的战前安全点保存失败，已取消场景切换。请检查磁盘后重试。");
            RefreshAllNodes();
            return;
        }

        string targetScene = GetSceneForNode(nodeId);
        Debug.Log($"[MapManager] 进入节点 {nodeId} ({gameManager.currentNodeType}) -> {targetScene}");
        SceneManager.LoadScene(targetScene);
    }

    public void OnBackToMainMenuClicked()
    {
        ChallengeRunTracker tracker = ChallengeRunTracker.EnsureExists();
        tracker.SuspendRun();
        bool saveRequired = GameManager.Instance != null && GameManager.Instance.gameInitialized;
        if (saveRequired && !RunSaveRepository.UpdateChallengeStateOnly())
        {
            tracker.ResumeRun();
            Debug.LogError("[MapManager] 挑战计时保存失败，已取消返回主菜单。");
            return;
        }
        SceneManager.LoadScene("MainMenuScene");
    }

    private void InitializeMapIfNeeded()
    {
        GameManager gameManager = GameManager.Instance;
        if (gameManager == null)
            return;

        if (!gameManager.gameInitialized)
        {
            gameManager.currentFloor = 1;
            gameManager.ResetMapProgressForCurrentFloor();
            gameManager.MarkNodeUnlocked(1);
            gameManager.currentNodeId = 0;
            gameManager.currentNodeType = string.Empty;
            gameManager.gameInitialized = true;
            gameManager.currentDraftMode = GameManager.DraftMode.BattleReward;

            RunSaveRepository.SaveSafePoint();
            Debug.Log("[MapManager] 已建立首个地图安全点并解锁节点1。");
            return;
        }

        // 兼容旧数据或异常场景直开：若仍有未完成节点但没有入口，只恢复唯一合法入口。
        int expectedNodeId = GetExpectedNodeId(gameManager);
        if (expectedNodeId > 0 && !gameManager.IsNodeUnlocked(expectedNodeId))
        {
            gameManager.MarkNodeUnlocked(expectedNodeId);
            RunSaveRepository.SaveSafePoint();
        }
    }

    private void ConfigureFixedNodeTypes()
    {
        if (AllMapNodes == null)
            return;

        foreach (MapNode node in AllMapNodes)
        {
            if (node != null)
                node.NodeType = GetNodeType(node.NodeId);
        }
    }

    private void RefreshAllNodes()
    {
        GameManager gameManager = GameManager.Instance;
        if (gameManager == null || AllMapNodes == null)
            return;

        foreach (MapNode node in AllMapNodes)
        {
            if (node == null)
                continue;

            node.NodeType = GetNodeType(node.NodeId);
            node.IsCleared = gameManager.IsNodeCleared(node.NodeId);
            node.IsUnlocked = gameManager.IsNodeUnlocked(node.NodeId);
            node.RefreshView();
        }
    }

    private static void EnsureGameManager()
    {
        if (GameManager.Instance == null)
            new GameObject("GameManager").AddComponent<GameManager>();
    }
}
