using UnityEngine;
using System;
using System.Collections;

/// <summary>
/// 战斗回合阶段枚举。
/// </summary>
public enum BattlePhase
{
    PlayerTurnStart,
    EnergyRefill,
    DrawPhase,
    PlayerAction,
    DiscardPhase,
    PoisonTickPhase,
    EnemyAction,
    ShieldClear,
    TurnEnd
}

/// <summary>
/// 回合管理器 - 独立管理回合阶段流程，与战斗逻辑解耦。
/// </summary>
public class TurnManager : MonoBehaviour
{
    [Header("回合设置")]
    [SerializeField] private float _phaseDelay = 0.5f;

    private currentEnergy _playerEnergy;
    private playerBlock _playerBlock;
    private enemyBlock _enemyBlock;
    private PlayerState _playerState;

    private BattlePhase _currentPhase;
    private int _currentTurn = 1;
    private bool _isRunning = false;
    private bool _playerActionEnded = false;
    private bool _enemyActionComplete = false;
    private Coroutine _turnCoroutine;

    public event Action<BattlePhase> OnPhaseChanged;
    public event Action<int> OnTurnStarted;
    public event Action OnEnergyRefilled;
    public event Action OnDrawPhase;
    public event Action OnPlayerActionStarted;
    public event Action OnPlayerActionEnded;
    public event Action OnDiscardPhase;
    public event Action OnPoisonTickPhase;
    public event Action OnEnemyActionStarted;
    public event Action OnEnemyActionEnded;
    public event Action OnShieldCleared;
    public event Action<int> OnTurnEnded;
    public event Action OnTurnLoopStopped;

    public BattlePhase CurrentPhase => _currentPhase;
    public int CurrentTurn => _currentTurn;
    public bool IsPlayerTurn => _currentPhase == BattlePhase.PlayerAction;
    public bool IsRunning => _isRunning;

    public void Initialize(currentEnergy playerEnergy, playerBlock playerBlock, enemyBlock enemyBlock, PlayerState playerState = null)
    {
        _playerEnergy = playerEnergy;
        _playerBlock = playerBlock;
        _enemyBlock = enemyBlock;
        _playerState = playerState;
    }

    public void StartBattle()
    {
        if (_isRunning) return;
        _isRunning = true;
        _currentTurn = 1;
        _turnCoroutine = StartCoroutine(RunTurnLoop());
    }

    public void EndPlayerTurn()
    {
        if (_currentPhase == BattlePhase.PlayerAction && _isRunning)
            _playerActionEnded = true;
    }

    public void SignalEnemyActionComplete()
    {
        _enemyActionComplete = true;
    }

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

    private IEnumerator RunTurnLoop()
    {
        while (_isRunning)
        {
            yield return StartCoroutine(ExecutePhase_PlayerTurnStart());
            if (!_isRunning) yield break;

            yield return StartCoroutine(ExecutePhase_EnergyRefill());
            if (!_isRunning) yield break;

            yield return StartCoroutine(ExecutePhase_DrawPhase());
            if (!_isRunning) yield break;

            yield return StartCoroutine(ExecutePhase_PlayerAction());
            if (!_isRunning) yield break;

            yield return StartCoroutine(ExecutePhase_DiscardPhase());
            if (!_isRunning) yield break;

            yield return StartCoroutine(ExecutePhase_PoisonTickPhase());
            if (!_isRunning) yield break;

            yield return StartCoroutine(ExecutePhase_EnemyAction());
            if (!_isRunning) yield break;

            yield return StartCoroutine(ExecutePhase_ShieldClear());
            if (!_isRunning) yield break;

            yield return StartCoroutine(ExecutePhase_TurnEnd());

            _currentTurn++;
        }
    }

    private IEnumerator ExecutePhase_PlayerTurnStart()
    {
        SetPhase(BattlePhase.PlayerTurnStart);
        _playerState?.ResetTurnFlags();
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

    private IEnumerator ExecutePhase_PoisonTickPhase()
    {
        SetPhase(BattlePhase.PoisonTickPhase);
        OnPoisonTickPhase?.Invoke();
        Debug.Log("[TurnManager] 中毒结算阶段");
        yield return new WaitForSeconds(_phaseDelay * 0.3f);
    }

    private IEnumerator ExecutePhase_EnemyAction()
    {
        SetPhase(BattlePhase.EnemyAction);
        _enemyActionComplete = false;
        OnEnemyActionStarted?.Invoke();
        Debug.Log("[TurnManager] 敌人行动阶段");

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
        {
            if (_playerState != null && _playerState.BarricadeActive)
            {
                Debug.Log($"[TurnManager] 壁垒生效，保留玩家护盾 {_playerBlock.CurrentBlock}");
            }
            else
            {
                _playerBlock.ResetBlock();
                Debug.Log("[TurnManager] 玩家护盾已清空");
            }
        }

        OnShieldCleared?.Invoke();
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
