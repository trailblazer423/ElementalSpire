using System;
using System.Collections.Generic;
using UnityEngine;

public class ChallengeRunTracker : MonoBehaviour
{
    public static ChallengeRunTracker Instance { get; private set; }

    private DateTime _startTime;
    private int _highestFloor;
    private int _highestProgress;
    private bool _hasActiveRun;
    private bool _recordSaved;

    public bool HasActiveRun { get { return _hasActiveRun; } }
    public int HighestFloor { get { return _highestFloor; } }
    public int HighestProgress { get { return _highestProgress; } }

    public static ChallengeRunTracker EnsureExists()
    {
        if (Instance != null)
            return Instance;

        ChallengeRunTracker existing = FindObjectOfType<ChallengeRunTracker>();
        if (existing != null)
            return existing;

        GameObject go = new GameObject("ChallengeRunTracker");
        return go.AddComponent<ChallengeRunTracker>();
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    public void StartRun()
    {
        _startTime = DateTime.Now;
        _highestFloor = 1;
        _highestProgress = 0;
        _hasActiveRun = true;
        _recordSaved = false;
        MarkProgressFromGameManager();
        Debug.Log("[ChallengeRunTracker] Run started at " + FormatTime(_startTime));
    }

    public void MarkProgressFromGameManager()
    {
        GameManager gameManager = GameManager.Instance;
        if (gameManager == null)
            return;

        MarkProgress(gameManager.currentFloor, gameManager.currentNodeId);
    }

    public void MarkProgress(int floor, int nodeId)
    {
        if (!_hasActiveRun)
            StartRun();

        int safeFloor = Mathf.Max(1, floor);
        int safeNodeId = Mathf.Clamp(nodeId, 0, GameManager.NodesPerFloor);
        int progress = (safeFloor - 1) * GameManager.NodesPerFloor + safeNodeId;

        _highestFloor = Mathf.Max(_highestFloor, safeFloor);
        _highestProgress = Mathf.Max(_highestProgress, progress);
    }

    public void EndRun(bool isWin)
    {
        if (_recordSaved)
            return;

        if (!_hasActiveRun)
            StartRun();

        MarkProgressFromGameManager();

        DateTime endTime = DateTime.Now;
        int durationSeconds = Mathf.Max(0, Mathf.RoundToInt((float)(endTime - _startTime).TotalSeconds));
        ChallengeRecord record = new ChallengeRecord
        {
            StartTime = FormatTime(_startTime),
            EndTime = FormatTime(endTime),
            DurationSeconds = durationSeconds,
            HighestFloor = Mathf.Max(1, _highestFloor),
            HighestProgress = Mathf.Max(0, _highestProgress),
            IsWin = isWin
        };

        try
        {
            ChallengeRecordRepository.Save(record);
            Debug.Log("[ChallengeRunTracker] Saved challenge record id=" + record.Id
                + ", progress=" + record.HighestProgress
                + ", result=" + record.ResultText
                + ", db=" + ChallengeRecordRepository.DatabasePath);
        }
        catch (Exception ex)
        {
            Debug.LogError("[ChallengeRunTracker] Failed to save challenge record: " + ex.Message);
        }

        _recordSaved = true;
        _hasActiveRun = false;
    }

    public List<ChallengeRecord> LoadLeaderboard(int limit = 20)
    {
        try
        {
            return ChallengeRecordRepository.LoadLeaderboard(limit);
        }
        catch (Exception ex)
        {
            Debug.LogError("[ChallengeRunTracker] Failed to load leaderboard: " + ex.Message);
            return new List<ChallengeRecord>();
        }
    }

    private static string FormatTime(DateTime time)
    {
        return time.ToString("yyyy-MM-dd HH:mm:ss");
    }
}