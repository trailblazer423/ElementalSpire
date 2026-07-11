using System;
using System.Collections.Generic;
using ElementalSpire.Cards;

/// <summary>
/// 一局挑战的可恢复安全点。该结构有意不包含任何战斗现场数据。
/// </summary>
[Serializable]
public sealed class RunSaveData
{
    public const int CurrentVersion = 1;

    public int version = CurrentVersion;
    public string savedAtUtc = string.Empty;

    public int playerHp;
    public int playerMaxHp;
    public List<string> playerCardBag = new List<string>();

    public int currentFloor = 1;
    public int currentNodeId;
    public string currentNodeType = string.Empty;
    public List<int> clearedNodeIds = new List<int>();
    public List<int> unlockedNodeIds = new List<int>();

    public ElementType mainElementA = ElementType.None;
    public ElementType mainElementB = ElementType.None;
    public bool gameInitialized;
    public bool isInitialDraftDone;

    public GameManager.DraftMode currentDraftMode = GameManager.DraftMode.InitialDraft;
    public bool pendingEventToClear;
    public bool pendingNodeCompletion;
    public bool runEnded;

    public ChallengeRunSaveState challenge = new ChallengeRunSaveState();
}

/// <summary>挑战计时与榜单进度的可恢复快照。</summary>
[Serializable]
public sealed class ChallengeRunSaveState
{
    public string startTimeUtc = string.Empty;
    public double activePlaySeconds;
    public int highestFloor = 1;
    public int highestProgress;
    public bool hasActiveRun;
    public bool recordSaved;
}
