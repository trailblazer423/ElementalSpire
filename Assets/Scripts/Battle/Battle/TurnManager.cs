using UnityEngine;
using System;
using System.Collections;

/// <summary>
/// 战斗回合阶段枚举
/// </summary>
public enum BattlePhase
{
    PlayerTurnStart,    // 玩家回合开始
    EnergyRefill,       // 获得基础能量
    DrawPhase,          // 抽牌
    PlayerAction,       // 我方行动
    DiscardPhase,       // 弃掉未使用的手牌
    EnemyAction,        // 敌人行动
    ShieldClear,        // 护盾清空
    TurnEnd             // 回合结束
}

/// <summary>
/// 回合管理器 - 独立管理回合阶段流程，与战斗逻辑解耦
/// </summary>
public class TurnManager : MonoBehaviour
{
    [Header("回合设置")]
    [SerializeField] private float _phaseDelay = 0.5f;

    // 回合机制需要的组件引用
    private currentEnergy _playerEnergy;
    private playerBlock _playerBlock;
    private enemyBlock _enemyBlock;

    private BattlePhase _currentPhase;
    private int _currentTurn = 1;
    private bool _isRunning = false;
    private bool _playerActionEnded = false;
    private bool _enemyActionComplete = false;
    private Coroutine _turnCoroutine;

    // ===== 事件系统（供 BattleManager / BattleUI 监听） =====
    public event Action<BattlePhase> OnPhaseChanged;
    public event Action<int> OnTurnStarted;
    public event Action OnEnergyRefilled;
    public event Action OnDrawPhase;
    public event Action OnPlayerActionStarted;
    public event Action OnPlayerActionEnded;
    public event Action OnDiscardPhase;
    public event Action OnEnemyActionStarted;
    public event Action OnEnemyActionEnded;
    public event Action OnShieldCleared;
    public event Action<int> OnTurnEnded;
    public event Action OnTurnLoopStopped;

    // ===== 公开属性 =====
    public BattlePhase CurrentPhase => _currentPhase;
    public int CurrentTurn => _currentTurn;
    public bool IsPlayerTurn => _currentPhase == BattlePhase.PlayerAction;
    public bool IsRunning => _isRunning;

    // ==========================================
    //  初始化与生命周期
    // ==========================================

    /// <summary>
    /// 初始化组件引用（由 BattleManager 调用）
    /// </summary>
    public void Initialize(currentEnergy playerEnergy, playerBlock playerBlock, enemyBlock enemyBlock)
    {
        _playerEnergy = playerEnergy;
        _playerBlock = playerBlock;
        _enemyBlock = enemyBlock;
    }

    /// <summary>
    /// 开始回合循环
    /// </summary>
    public void StartBattle()
    {
        if (_isRunning) return;
        _isRunning = true;
        _currentTurn = 1;
        _turnCoroutine = StartCoroutine(RunTurnLoop());
    }

    /// <summary>
    /// 玩家结束行动（由 UI 按钮调用）
    /// </summary>
    public void EndPlayerTurn()
    {
        if (_currentPhase == BattlePhase.PlayerAction && _isRunning)
            _playerActionEnded = true;
    }

    /// <summary>
    /// 敌人行动完成信号（由 BattleManager 的 AI 协程调用）
    /// </summary>
    public void SignalEnemyActionComplete()
    {
        _enemyActionComplete = true;
    }

    /// <summary>
    /// 停止回合循环（战斗结束时调用）
    /// </summary>
    public void StopBattle()
    {
        if (!_isRunning) return;
        _isRunning = false;
        if (_turnCoroutine != null)
        {
            StopCoroutine(_turnCoroutine);
            _turnCoroutine = null;
        }
        OnTurnLoopStopped?.Invoke();
    }

    // ==========================================
    //  核心回合循环
    // ==========================================

    private IEnumerator RunTurnLoop()
    {
        while (_isRunning)
        {
            // 1. 玩家回合开始
            yield return StartCoroutine(ExecutePhase_PlayerTurnStart());
            if (!_isRunning) yield break;

            // 2. 获得基础能量
            yield return StartCoroutine(ExecutePhase_EnergyRefill());
            if (!_isRunning) yield break;

            // 3. 抽牌
            yield return StartCoroutine(ExecutePhase_DrawPhase());
            if (!_isRunning) yield break;

            // 4. 我方行动
            yield return StartCoroutine(ExecutePhase_PlayerAction());
            if (!_isRunning) yield break;

            // 5. 弃掉未使用的手牌
            yield return StartCoroutine(ExecutePhase_DiscardPhase());
            if (!_isRunning) yield break;

            // 6. 敌人行动
            yield return StartCoroutine(ExecutePhase_EnemyAction());
            if (!_isRunning) yield break;

            // 7. 护盾清空
            yield return StartCoroutine(ExecutePhase_ShieldClear());
            if (!_isRunning) yield break;

            // 8. 回合结束
            yield return StartCoroutine(ExecutePhase_TurnEnd());

            _currentTurn++;
        }
    }

    // ==========================================
    //  各阶段执行
    // ==========================================

    private IEnumerator ExecutePhase_PlayerTurnStart()
    {
        SetPhase(BattlePhase.PlayerTurnStart);
        OnTurnStarted?.Invoke(_currentTurn);
        Debug.Log($"[TurnManager] === 第 {_currentTurn} 回合 ===");
        yield return new WaitForSeconds(_phaseDelay * 0.5f);
    }

    private IEnumerator ExecutePhase_EnergyRefill()
    {
        SetPhase(BattlePhase.EnergyRefill);

        if (_playerEnergy != null)
        {
            _playerEnergy.RefillEnergy();
            Debug.Log($"[TurnManager] 能量已恢复至 {_playerEnergy.CurrentEnergy}/{_playerEnergy.MaxEnergy}");
        }

        OnEnergyRefilled?.Invoke();
        yield return new WaitForSeconds(_phaseDelay * 0.5f);
    }

    private IEnumerator ExecutePhase_DrawPhase()
    {
        SetPhase(BattlePhase.DrawPhase);
        OnDrawPhase?.Invoke();
        Debug.Log("[TurnManager] 抽牌阶段");
        yield return new WaitForSeconds(_phaseDelay * 0.3f);
    }

    private IEnumerator ExecutePhase_PlayerAction()
    {
        SetPhase(BattlePhase.PlayerAction);
        _playerActionEnded = false;
        OnPlayerActionStarted?.Invoke();
        Debug.Log("[TurnManager] 我方行动阶段 - 等待操作...");

        while (!_playerActionEnded && _isRunning)
            yield return null;

        OnPlayerActionEnded?.Invoke();
        Debug.Log("[TurnManager] 我方行动结束");
        yield return new WaitForSeconds(_phaseDelay * 0.3f);
    }

    private IEnumerator ExecutePhase_DiscardPhase()
    {
        SetPhase(BattlePhase.DiscardPhase);
        OnDiscardPhase?.Invoke();
        Debug.Log("[TurnManager] 弃牌阶段");
        yield return new WaitForSeconds(_phaseDelay * 0.3f);
    }

    private IEnumerator ExecutePhase_EnemyAction()
    {
        SetPhase(BattlePhase.EnemyAction);
        OnEnemyActionStarted?.Invoke();
        Debug.Log("[TurnManager] 敌人行动阶段");

        // 等待 BattleManager 完成敌人 AI
        _enemyActionComplete = false;
        while (!_enemyActionComplete && _isRunning)
            yield return null;

        OnEnemyActionEnded?.Invoke();
        Debug.Log("[TurnManager] 敌人行动结束");
        yield return new WaitForSeconds(_phaseDelay * 0.5f);
    }

    private IEnumerator ExecutePhase_ShieldClear()
    {
        SetPhase(BattlePhase.ShieldClear);

        if (_playerBlock != null)
            _playerBlock.ResetBlock();
        if (_enemyBlock != null)
            _enemyBlock.ResetBlock();

        OnShieldCleared?.Invoke();
        Debug.Log("[TurnManager] 护盾已清空");
        yield return new WaitForSeconds(_phaseDelay * 0.5f);
    }

    private IEnumerator ExecutePhase_TurnEnd()
    {
        SetPhase(BattlePhase.TurnEnd);
        OnTurnEnded?.Invoke(_currentTurn);
        Debug.Log($"[TurnManager] 第 {_currentTurn} 回合结束");
        yield return new WaitForSeconds(_phaseDelay * 0.5f);
    }

    private void SetPhase(BattlePhase phase)
    {
        _currentPhase = phase;
        OnPhaseChanged?.Invoke(phase);
    }
}
