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

        // 从战斗胜利返回时，更新节点进度
        if (GameManager.Instance.isBattleWin)
        {
            int winNodeId = GameManager.Instance.currentNodeId;

            foreach (var node in AllMapNodes)
            {
                // 标记当前节点为已通关
                if (node.NodeId == winNodeId)
                {
                    node.IsCleared = true;
                    // 调用全局奖励管理器发放通关奖励
                    RewardManager.Instance.GrantReward(node.ClearReward);
                }

                // 线性解锁：下一个ID的节点变为可点击
                if (node.NodeId == winNodeId + 1)
                {
                    node.IsUnlocked = true;
                }
            }

            // 重置战斗胜利标记，避免重复触发
            GameManager.Instance.isBattleWin = false;
        }

        // 刷新所有节点的显示状态
        foreach (var node in AllMapNodes)
        {
            node.RefreshView();
        }
    }

    /// <summary>
    /// 解锁指定节点的下一个节点，供节点脚本内部调用
    /// </summary>
    public void UnlockNextNodes(int currentNodeId)
    {
        foreach (var node in AllMapNodes)
        {
            if (node.NodeId == currentNodeId + 1)
            {
                node.IsUnlocked = true;
                node.RefreshView();
            }
        }
    }
}
