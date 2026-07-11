using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

/// <summary>
/// 挑战记录的本地仓储。使用独立 JSON 文件，避免旧 Mono.Data.Sqlite 桩程序集和原生库
/// 在 Unity 2022 编辑器/玩家之间产生 ABI 差异；接口与排行榜调用方保持不变。
/// </summary>
public static class ChallengeRecordRepository
{
    private const string DatabaseFileName = "challenge_records.json";
    private const int CurrentVersion = 1;
    private static readonly object SyncRoot = new object();
#if UNITY_EDITOR
    private static string _editorDatabasePathOverride;
#endif

    [Serializable]
    private sealed class ChallengeRecordStore
    {
        public int version = CurrentVersion;
        public long nextId = 1;
        public List<ChallengeRecord> records = new List<ChallengeRecord>();
    }

    public static string DatabasePath
    {
        get
        {
#if UNITY_EDITOR
            if (!string.IsNullOrEmpty(_editorDatabasePathOverride))
                return _editorDatabasePathOverride;
#endif
            return Path.Combine(Application.persistentDataPath, DatabaseFileName);
        }
    }

#if UNITY_EDITOR
    public static void SetEditorTestPath(string path)
    {
        _editorDatabasePathOverride = path;
    }

    public static void ClearEditorTestPath()
    {
        _editorDatabasePathOverride = null;
    }
#endif

    public static void Save(ChallengeRecord record)
    {
        if (record == null)
            throw new ArgumentNullException(nameof(record));

        lock (SyncRoot)
        {
            ChallengeRecordStore store = LoadStoreForWrite();
            long assignedId = Math.Max(1L, store.nextId);
            store.nextId = assignedId + 1L;

            ChallengeRecord storedRecord = CloneRecord(record);
            storedRecord.Id = assignedId;
            store.records.Add(storedRecord);
            WriteAtomic(store);
            record.Id = assignedId;
        }
    }

    public static List<ChallengeRecord> LoadLeaderboard(int limit = 20)
    {
        lock (SyncRoot)
        {
            ChallengeRecordStore store;
            string error;
            if (!TryLoadStore(out store, out error))
            {
                if (File.Exists(DatabasePath))
                    Debug.LogError("[ChallengeRecordRepository] 排行榜文件读取失败：" + error);
                return new List<ChallengeRecord>();
            }

            return store.records
                .Where(item => item != null)
                .OrderByDescending(item => item.HighestProgress)
                .ThenBy(item => Math.Max(0, item.DurationSeconds))
                .ThenByDescending(item => ParseTimestamp(item.EndTime))
                .Take(Mathf.Max(1, limit))
                .Select(CloneRecord)
                .ToList();
        }
    }

    private static ChallengeRecordStore LoadStoreForWrite()
    {
        ChallengeRecordStore store;
        string error;
        if (TryLoadStore(out store, out error))
            return store;

        if (File.Exists(DatabasePath))
            throw new InvalidDataException("排行榜文件已损坏，拒绝覆盖：" + error);

        return new ChallengeRecordStore();
    }

    private static bool TryLoadStore(out ChallengeRecordStore store, out string error)
    {
        store = null;
        error = string.Empty;
        try
        {
            if (!File.Exists(DatabasePath))
            {
                store = new ChallengeRecordStore();
                return true;
            }

            string json = File.ReadAllText(DatabasePath, Encoding.UTF8);
            store = JsonUtility.FromJson<ChallengeRecordStore>(json);
            if (store == null || store.version != CurrentVersion || store.records == null)
            {
                error = "版本不支持或 JSON 结构无效。";
                store = null;
                return false;
            }

            long highestId = store.records
                .Where(item => item != null)
                .Select(item => Math.Max(0L, item.Id))
                .DefaultIfEmpty(0L)
                .Max();
            store.nextId = Math.Max(store.nextId, highestId + 1L);
            return true;
        }
        catch (Exception exception)
        {
            error = exception.Message;
            store = null;
            return false;
        }
    }

    private static void WriteAtomic(ChallengeRecordStore store)
    {
        string path = DatabasePath;
        string tempPath = path + ".tmp";
        string backupPath = path + ".bak";
        string directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        try
        {
            DeleteIfPresent(tempPath);
            string json = JsonUtility.ToJson(store, true);
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
        }
        finally
        {
            DeleteIfPresent(tempPath);
        }
    }

    private static ChallengeRecord CloneRecord(ChallengeRecord source)
    {
        return new ChallengeRecord
        {
            Id = source.Id,
            StartTime = source.StartTime,
            EndTime = source.EndTime,
            DurationSeconds = source.DurationSeconds,
            HighestFloor = source.HighestFloor,
            HighestProgress = source.HighestProgress,
            IsWin = source.IsWin
        };
    }

    private static DateTime ParseTimestamp(string value)
    {
        DateTime parsed;
        return DateTime.TryParse(value, out parsed) ? parsed : DateTime.MinValue;
    }

    private static void DeleteIfPresent(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (Exception exception)
        {
            Debug.LogWarning("[ChallengeRecordRepository] 清理临时文件失败：" + exception.Message);
        }
    }
}
