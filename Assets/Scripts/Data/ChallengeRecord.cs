using System;

[Serializable]
public class ChallengeRecord
{
    public long Id;
    public string StartTime;
    public string EndTime;
    public int DurationSeconds;
    public int HighestFloor;
    public int HighestProgress;
    public bool IsWin;

    public string ResultText
    {
        get { return IsWin ? "Win" : "Lose"; }
    }

    public string DurationText
    {
        get
        {
            TimeSpan duration = TimeSpan.FromSeconds(Math.Max(0, DurationSeconds));
            if (duration.TotalHours >= 1)
                return string.Format("{0}h {1}m {2}s", (int)duration.TotalHours, duration.Minutes, duration.Seconds);
            if (duration.TotalMinutes >= 1)
                return string.Format("{0}m {1}s", duration.Minutes, duration.Seconds);
            return string.Format("{0}s", duration.Seconds);
        }
    }
}