using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ElementalSpire.Cards;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 管理 run_save.json。所有写入都先落到同目录临时文件，再以原子替换提交。
/// </summary>
public static class RunSaveRepository
{
    private const string SaveFileName = "run_save.json";
    private static readonly object SyncRoot = new object();
#if UNITY_EDITOR
    private static string _editorSaveFilePathOverride;
#endif

    public static string SaveFilePath
    {
        get
        {
#if UNITY_EDITOR
            if (!string.IsNullOrEmpty(_editorSaveFilePathOverride))
                return _editorSaveFilePathOverride;
#endif
            return Path.Combine(Application.persistentDataPath, SaveFileName);
        }
    }

#if UNITY_EDITOR
    public static void SetEditorTestPath(string path)
    {
        _editorSaveFilePathOverride = path;
    }

    public static void ClearEditorTestPath()
    {
        _editorSaveFilePathOverride = null;
    }
#endif

    public static string SavePath
    {
        get { return SaveFilePath; }
    }

    public static bool HasValidSave
    {
        get
        {
            RunSaveData data;
            string error;
            return TryLoad(out data, out error) && IsUsable(data);
        }
    }

    /// <summary>
    /// 保存完整安全点。若当前处于 BattleScene，只刷新计时快照，绝不覆盖玩法进度。
    /// </summary>
    public static bool SaveSafePoint()
    {
        if (string.Equals(SceneManager.GetActiveScene().name, "BattleScene", StringComparison.Ordinal))
        {
            Debug.Log("[RunSaveRepository] BattleScene 中禁止覆盖安全点，仅刷新挑战计时。");
            return UpdateChallengeStateOnly();
        }

        GameManager gameManager = GameManager.Instance;
        if (gameManager == null)
        {
            Debug.LogError("[RunSaveRepository] GameManager 不存在，无法保存安全点。");
            return false;
        }

        if (gameManager.runEnded)
        {
            Debug.LogWarning("[RunSaveRepository] 本局已经结束，拒绝重新创建继续存档。");
            return false;
        }

        ChallengeRunTracker tracker = ChallengeRunTracker.EnsureExists();
        if (!tracker.HasActiveRun && !gameManager.runEnded)
            tracker.StartRun();

        RunSaveData data = gameManager.CaptureRunSaveData();
        data.challenge = tracker.CaptureSaveState();
        data.savedAtUtc = DateTime.UtcNow.ToString("o");

        string error;
        bool saved = TryWriteAtomic(data, out error);
        if (!saved)
            Debug.LogError("[RunSaveRepository] 安全点保存失败：" + error);
        return saved;
    }

    /// <summary>
    /// 只更新已有存档中的挑战计时/进度；卡组、生命、节点等安全点字段保持原样。
    /// </summary>
    public static bool UpdateChallengeStateOnly()
    {
        RunSaveData data;
        string error;
        if (!TryLoad(out data, out error) || !IsUsable(data))
        {
            Debug.LogWarning("[RunSaveRepository] 没有可更新的安全点：" + error);
            return false;
        }

        ChallengeRunTracker tracker = ChallengeRunTracker.Instance;
        if (tracker == null)
        {
            Debug.LogWarning("[RunSaveRepository] ChallengeRunTracker 不存在，未改写安全点。");
            return false;
        }

        data.challenge = tracker.CaptureSaveState();
        data.savedAtUtc = DateTime.UtcNow.ToString("o");
        bool saved = TryWriteAtomic(data, out error);
        if (!saved)
            Debug.LogError("[RunSaveRepository] 挑战计时更新失败：" + error);
        return saved;
    }

    public static bool TryRestoreLatest(out string error)
    {
        RunSaveData data;
        if (!TryLoad(out data, out error))
            return false;

        if (!IsUsable(data))
        {
            error = "存档已经结束或数据无效。";
            return false;
        }

        GameManager gameManager = GameManager.Instance;
        if (gameManager == null)
        {
            GameObject managerObject = new GameObject("GameManager");
            gameManager = managerObject.AddComponent<GameManager>();
        }

        try
        {
            gameManager.RestoreRunSaveData(data);
            ChallengeRunTracker tracker = ChallengeRunTracker.EnsureExists();
            if (data.challenge != null && data.challenge.hasActiveRun)
                tracker.RestoreSaveState(data.challenge, true);
            else
                tracker.StartRun();

            error = string.Empty;
            Debug.Log("[RunSaveRepository] 已恢复安全点：" + SaveFilePath);
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            Debug.LogError("[RunSaveRepository] 恢复安全点失败：" + ex);
            return false;
        }
    }

    public static bool TryLoad(out RunSaveData data, out string error)
    {
        lock (SyncRoot)
        {
            data = null;
            error = string.Empty;

            try
            {
                if (!File.Exists(SaveFilePath))
                {
                    error = "未找到 run_save.json。";
                    return false;
                }

                string json = File.ReadAllText(SaveFilePath, Encoding.UTF8);
                data = JsonUtility.FromJson<RunSaveData>(json);
                if (data == null)
                {
                    error = "存档 JSON 为空或无法解析。";
                    return false;
                }

                if (data.version <= 0 || data.version > RunSaveData.CurrentVersion)
                {
                    error = "不支持的存档版本：" + data.version;
                    data = null;
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                data = null;
                return false;
            }
        }
    }

    public static void DeleteSave()
    {
        lock (SyncRoot)
        {
            DeleteIfPresent(SaveFilePath);
            DeleteIfPresent(SaveFilePath + ".tmp");
            DeleteIfPresent(SaveFilePath + ".bak");
        }
    }

    public static void Delete()
    {
        DeleteSave();
    }

    private static bool IsUsable(RunSaveData data)
    {
        if (data == null
            || data.runEnded
            || !data.gameInitialized
            || !data.isInitialDraftDone
            || data.playerMaxHp <= 0
            || data.playerHp <= 0
            || data.playerHp > data.playerMaxHp
            || data.currentFloor != 1
            || data.currentNodeId < 0
            || data.currentNodeId > GameManager.NodesPerFloor
            || data.playerCardBag == null
            || data.clearedNodeIds == null
            || data.unlockedNodeIds == null
            || data.challenge == null
            || !data.challenge.hasActiveRun
            || data.challenge.recordSaved
            || !IsMainElement(data.mainElementA)
            || !IsMainElement(data.mainElementB)
            || data.mainElementA == data.mainElementB)
        {
            return false;
        }

        var cleared = new HashSet<int>();
        foreach (int nodeId in data.clearedNodeIds)
        {
            if (nodeId < 1 || nodeId > GameManager.NodesPerFloor || !cleared.Add(nodeId))
                return false;
        }

        var unlocked = new HashSet<int>();
        foreach (int nodeId in data.unlockedNodeIds)
        {
            if (nodeId < 1 || nodeId > GameManager.NodesPerFloor || !unlocked.Add(nodeId))
                return false;
        }

        int completedCount = 0;
        while (completedCount < GameManager.NodesPerFloor && cleared.Contains(completedCount + 1))
            completedCount++;
        if (completedCount != cleared.Count || completedCount >= GameManager.NodesPerFloor)
            return false;

        int expectedNodeId = completedCount + 1;
        if (!unlocked.Contains(expectedNodeId))
            return false;

        return data.currentNodeId <= expectedNodeId;
    }

    private static bool IsMainElement(ElementType element)
    {
        return element == ElementType.Fire
            || element == ElementType.Poison
            || element == ElementType.Water;
    }

    private static bool TryWriteAtomic(RunSaveData data, out string error)
    {
        lock (SyncRoot)
        {
            string path = SaveFilePath;
            string tempPath = path + ".tmp";
            string backupPath = path + ".bak";
            error = string.Empty;

            try
            {
                string directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory))
                    Directory.CreateDirectory(directory);

                DeleteIfPresent(tempPath);
                string json = JsonUtility.ToJson(data, true);
                File.WriteAllText(tempPath, json, new UTF8Encoding(false));

                if (File.Exists(path))
                {
                    DeleteIfPresent(backupPath);
                    File.Replace(tempPath, path, backupPath, true);
                    DeleteIfPresent(backupPath);
                }
                else
                {
                    File.Move(tempPath, path);
                }

                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
            finally
            {
                DeleteIfPresent(tempPath);
            }
        }
    }

    private static void DeleteIfPresent(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[RunSaveRepository] 清理文件失败 " + path + "：" + ex.Message);
        }
    }
}
