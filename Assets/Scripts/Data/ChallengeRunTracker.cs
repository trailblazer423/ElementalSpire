using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 跟踪单局挑战。计时仅累计应用处于本局游玩状态的时间，退出到主菜单期间不累计。
/// </summary>
public class ChallengeRunTracker : MonoBehaviour
{
    public static ChallengeRunTracker Instance { get; private set; }

    private DateTime _startTimeUtc;
    private int _highestFloor;
    private int _highestProgress;
    private bool _hasActiveRun;
    private bool _recordSaved;
    private double _accumulatedActiveSeconds;
    private double _activeSegmentStartedAt;
    private bool _activeSegmentRunning;
    private bool _applicationPaused;
    private bool _applicationUnfocused;
    private bool _resumeAfterApplicationSuspend;

    public bool HasActiveRun { get { return _hasActiveRun; } }
    public int HighestFloor { get { return _highestFloor; } }
    public int HighestProgress { get { return _highestProgress; } }
    public double ActivePlaySeconds { get { return GetActivePlaySeconds(); } }
    public DateTime StartTimeUtc { get { return _startTimeUtc; } }

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
        _startTimeUtc = DateTime.UtcNow;
        _highestFloor = 1;
        _highestProgress = 0;
        _hasActiveRun = true;
        _recordSaved = false;
        _accumulatedActiveSeconds = 0d;
        _activeSegmentStartedAt = Time.realtimeSinceStartupAsDouble;
        _activeSegmentRunning = true;
        _applicationPaused = false;
        _applicationUnfocused = false;
        _resumeAfterApplicationSuspend = false;
        MarkProgressFromGameManager();
        Debug.Log("[ChallengeRunTracker] Run started at " + FormatTime(_startTimeUtc.ToLocalTime()));
    }

    public void ResetTracking()
    {
        _startTimeUtc = default(DateTime);
        _highestFloor = 1;
        _highestProgress = 0;
        _hasActiveRun = false;
        _recordSaved = false;
        _accumulatedActiveSeconds = 0d;
        _activeSegmentStartedAt = 0d;
        _activeSegmentRunning = false;
        _applicationPaused = false;
        _applicationUnfocused = false;
        _resumeAfterApplicationSuspend = false;
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

        int safeFloor = 1;
        int safeNodeId = Mathf.Clamp(nodeId, 0, GameManager.NodesPerFloor);

        _highestFloor = Mathf.Max(_highestFloor, safeFloor);
        _highestProgress = Mathf.Max(_highestProgress, safeNodeId);
    }

    /// <summary>暂停主动计时；退出到主菜单前调用。</summary>
    public void SuspendRun()
    {
        if (!_hasActiveRun || !_activeSegmentRunning)
            return;

        _accumulatedActiveSeconds = GetActivePlaySeconds();
        _activeSegmentRunning = false;
        _activeSegmentStartedAt = 0d;
    }

    /// <summary>继续安全点后恢复主动计时。</summary>
    public void ResumeRun()
    {
        if (!_hasActiveRun || _activeSegmentRunning)
            return;

        _activeSegmentStartedAt = Time.realtimeSinceStartupAsDouble;
        _activeSegmentRunning = true;
    }

    public ChallengeRunSaveState CaptureSaveState()
    {
        return new ChallengeRunSaveState
        {
            startTimeUtc = _startTimeUtc == default(DateTime) ? string.Empty : _startTimeUtc.ToString("o"),
            activePlaySeconds = Math.Max(0d, GetActivePlaySeconds()),
            highestFloor = Mathf.Max(1, _highestFloor),
            highestProgress = Mathf.Clamp(_highestProgress, 0, GameManager.NodesPerFloor),
            hasActiveRun = _hasActiveRun,
            recordSaved = _recordSaved
        };
    }

    public void RestoreSaveState(ChallengeRunSaveState state, bool resumeRun)
    {
        if (state == null)
            throw new ArgumentNullException(nameof(state));

        DateTime parsedStart;
        if (!DateTime.TryParse(state.startTimeUtc, null, System.Globalization.DateTimeStyles.RoundtripKind, out parsedStart))
            parsedStart = DateTime.UtcNow;

        _startTimeUtc = parsedStart.Kind == DateTimeKind.Utc ? parsedStart : parsedStart.ToUniversalTime();
        _accumulatedActiveSeconds = Math.Max(0d, state.activePlaySeconds);
        _highestFloor = 1;
        _highestProgress = Mathf.Clamp(state.highestProgress, 0, GameManager.NodesPerFloor);
        _hasActiveRun = state.hasActiveRun;
        _recordSaved = state.recordSaved;
        _activeSegmentRunning = false;
        _activeSegmentStartedAt = 0d;
        _applicationPaused = false;
        _applicationUnfocused = false;
        _resumeAfterApplicationSuspend = false;

        if (resumeRun && _hasActiveRun && !_recordSaved)
            ResumeRun();
    }

    public void EndRun(bool isWin)
    {
        if (_recordSaved)
            return;

        if (!_hasActiveRun)
        {
            Debug.LogWarning("[ChallengeRunTracker] 没有进行中的挑战，忽略终局记录请求。");
            RunSaveRepository.DeleteSave();
            return;
        }

        SuspendRun();

        DateTime endTimeUtc = DateTime.UtcNow;
        int durationSeconds = Mathf.Max(0, Mathf.RoundToInt((float)_accumulatedActiveSeconds));
        ChallengeRecord record = new ChallengeRecord
        {
            StartTime = FormatTime(_startTimeUtc.ToLocalTime()),
            EndTime = FormatTime(endTimeUtc.ToLocalTime()),
            DurationSeconds = durationSeconds,
            HighestFloor = 1,
            HighestProgress = Mathf.Clamp(_highestProgress, 0, GameManager.NodesPerFloor),
            IsWin = isWin
        };

        bool recordSaved = false;
        try
        {
            ChallengeRecordRepository.Save(record);
            recordSaved = true;
            Debug.Log("[ChallengeRunTracker] Saved challenge record id=" + record.Id
                + ", progress=" + record.HighestProgress
                + ", activeSeconds=" + record.DurationSeconds
                + ", result=" + record.ResultText
                + ", db=" + ChallengeRecordRepository.DatabasePath);
        }
        catch (Exception ex)
        {
            Debug.LogError("[ChallengeRunTracker] Failed to save challenge record: " + ex.Message);
        }

        _recordSaved = recordSaved;
        _hasActiveRun = false;
        _activeSegmentRunning = false;

        if (GameManager.Instance != null)
            GameManager.Instance.runEnded = true;

        RunSaveRepository.DeleteSave();
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

    private double GetActivePlaySeconds()
    {
        if (!_activeSegmentRunning)
            return Math.Max(0d, _accumulatedActiveSeconds);

        double segmentSeconds = Math.Max(0d, Time.realtimeSinceStartupAsDouble - _activeSegmentStartedAt);
        return Math.Max(0d, _accumulatedActiveSeconds + segmentSeconds);
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        _applicationPaused = pauseStatus;
        HandleApplicationActivityChanged();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        _applicationUnfocused = !hasFocus;
        HandleApplicationActivityChanged();
    }

    private void OnApplicationQuit()
    {
        if (!_hasActiveRun)
            return;

        SuspendRun();
        RunSaveRepository.UpdateChallengeStateOnly();
    }

    private void HandleApplicationActivityChanged()
    {
        if (!_hasActiveRun)
            return;

        bool suspended = _applicationPaused || _applicationUnfocused;
        if (suspended)
        {
            if (_activeSegmentRunning)
                _resumeAfterApplicationSuspend = true;
            SuspendRun();
            RunSaveRepository.UpdateChallengeStateOnly();
            return;
        }

        if (_resumeAfterApplicationSuspend
            && !string.Equals(SceneManager.GetActiveScene().name, "MainMenuScene", StringComparison.Ordinal))
        {
            ResumeRun();
        }
        _resumeAfterApplicationSuspend = false;
    }

    private static string FormatTime(DateTime time)
    {
        return time.ToString("yyyy-MM-dd HH:mm:ss");
    }
}
