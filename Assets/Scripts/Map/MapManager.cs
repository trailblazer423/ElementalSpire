using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

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
    /// 进入战斗按钮点击
    /// </summary>
    public void OnEnterBattleClicked()
    {
        SceneManager.LoadScene("BattleScene");
    }

    /// <summary>
    /// 返回主菜单按钮点击
    /// </summary>
    public void OnBackToMainMenuClicked()
    {
        SceneManager.LoadScene("MainMenuScene");
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
