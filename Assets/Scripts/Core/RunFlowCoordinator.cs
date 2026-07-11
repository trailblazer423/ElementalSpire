using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 关卡结算的唯一入口。
/// 将“完成节点、解锁下一节点、挑战进度、临时存档”保持为一个不可拆分的流程，
/// 避免各个奖励/事件/休息场景分别维护一套状态。
/// </summary>
public static class RunFlowCoordinator
{
    public const int FinalNodeId = GameManager.NodesPerFloor;

    /// <summary>
    /// 完成普通节点并回到地图。CardDraft、Event、Rest 的所有完成/跳过路径都调用此方法。
    /// 第 10 节点属于终局，若误入本方法会安全结束本局而不会解锁节点 11。
    /// </summary>
    public static void CompleteCurrentNodeAndReturnToMap()
    {
        GameManager gameManager = GameManager.Instance;
        if (gameManager == null)
        {
            Debug.LogError("[RunFlowCoordinator] GameManager 不存在，无法完成节点。");
            SceneManager.LoadScene("MainMenuScene");
            return;
        }

        int nodeId = gameManager.currentNodeId;
        if (nodeId <= 0 || nodeId > FinalNodeId)
        {
            Debug.LogError($"[RunFlowCoordinator] 非法节点 ID：{nodeId}，拒绝写入进度。");
            SceneManager.LoadScene("MapScene");
            return;
        }

        if (nodeId == FinalNodeId)
        {
            Debug.LogWarning("[RunFlowCoordinator] 第 10 节点进入了普通结算入口，改按终局胜利处理。");
            EndRunFromBattle(true);
            SceneManager.LoadScene("MainMenuScene");
            return;
        }

        gameManager.MarkNodeCleared(nodeId);
        gameManager.MarkNodeUnlocked(nodeId + 1);
        NormalizeAfterNodeCompletion(gameManager);

        ChallengeRunTracker tracker = ChallengeRunTracker.EnsureExists();
        tracker.MarkProgress(gameManager.currentFloor, nodeId);
        if (!TrySaveCompletedSafePoint())
        {
            Debug.LogError(
                $"[RunFlowCoordinator] 节点 {nodeId} 已在内存中结算，但安全点连续保存失败。"
                + "为避免丢失进度，已停止场景切换；请检查磁盘空间或权限后重试。");
            return;
        }

        Debug.Log($"[RunFlowCoordinator] 节点 {nodeId} 已完成，节点 {nodeId + 1} 已解锁并保存安全点。");
        SceneManager.LoadScene("MapScene");
    }

    /// <summary>
    /// 战斗终局入口：任意战败或第 10 关胜利时调用。
    /// 本方法只结算记录并删除继续存档，不主动切换场景，由战斗结果 UI 决定何时返回主菜单。
    /// </summary>
    public static void EndRunFromBattle(bool isWin)
    {
        GameManager gameManager = GameManager.Instance;
        if (gameManager != null)
        {
            if (isWin)
            {
                int nodeId = Mathf.Clamp(gameManager.currentNodeId, 1, FinalNodeId);
                gameManager.MarkNodeCleared(nodeId);
                ChallengeRunTracker.EnsureExists().MarkProgress(gameManager.currentFloor, nodeId);
            }

            gameManager.pendingEventToClear = false;
            gameManager.pendingNodeCompletion = false;
            gameManager.runEnded = true;
            gameManager.isBattleWin = isWin;
            gameManager.lastBattleResult = isWin ? "win" : "lose";
            gameManager.currentDraftMode = GameManager.DraftMode.BattleReward;
        }

        ChallengeRunTracker.EnsureExists().EndRun(isWin);
        RunSaveRepository.DeleteSave();
        Debug.Log($"[RunFlowCoordinator] 本局已结束：{(isWin ? "胜利" : "失败")}，继续存档已删除。");
    }

    private static void NormalizeAfterNodeCompletion(GameManager gameManager)
    {
        gameManager.pendingEventToClear = false;
        gameManager.pendingNodeCompletion = false;
        gameManager.runEnded = false;
        gameManager.isBattleWin = false;
        gameManager.lastBattleResult = string.Empty;
        gameManager.currentDraftMode = GameManager.DraftMode.BattleReward;
    }

    private static bool TrySaveCompletedSafePoint()
    {
        const int maxAttempts = 3;
        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            if (RunSaveRepository.SaveSafePoint())
                return true;

            Debug.LogWarning($"[RunFlowCoordinator] 安全点保存失败，第 {attempt}/{maxAttempts} 次。");
        }

        return false;
    }
}
