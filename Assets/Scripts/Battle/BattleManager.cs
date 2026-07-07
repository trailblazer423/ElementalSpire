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
    PlayerAction,       // 我方行动
    EnemyAction,        // 敌人行动
    ShieldClear,        // 护盾清空
    TurnEnd             // 回合结束
}

/// <summary>
/// 回合制战斗管理器 - 管理战斗的核心回合流程
/// </summary>
public class BattleManager : MonoBehaviour
{
    [Header("战斗对象绑定")]
    [SerializeField] private GameObject _playerObject;
    [SerializeField] private GameObject _enemyObject;

    [Header("回合设置")]
    [SerializeField] private float _phaseDelay = 0.5f;

    // 组件缓存
    private currentEnergy _playerEnergy;
    private playerBlock _playerBlock;
    private playerHP _playerHP;
    private enemyBlock _enemyBlock;
    private enemyHP _enemyHP;

    private BattlePhase _currentPhase;
    private int _currentTurn = 1;
    private bool _isBattleOver = false;
    private bool _playerActionEnded = false;
    private Coroutine _turnCoroutine;

    // ===== 事件系统 =====
    public event Action<BattlePhase> OnPhaseChanged;
    public event Action<int> OnTurnStarted;
    public event Action OnEnergyRefilled;
    public event Action OnPlayerActionStarted;
    public event Action OnPlayerActionEnded;
    public event Action OnEnemyActionStarted;
    public event Action OnEnemyActionEnded;
    public event Action OnShieldCleared;
    public event Action<int> OnTurnEnded;
    public event Action<string> OnBattleOver;  // "win" 或 "lose"

    // 对外暴露的对象引用（供UI等使用）
    public GameObject PlayerObject => _playerObject;
    public GameObject EnemyObject => _enemyObject;

    public BattlePhase CurrentPhase => _currentPhase;
    public int CurrentTurn => _currentTurn;
    public bool IsPlayerTurn => _currentPhase == BattlePhase.PlayerAction;
    public bool IsBattleOver => _isBattleOver;

    void Awake()
    {
        CacheComponents();
    }

    void Start()
    {
        // 自动创建 UI（如果场景中没有 BattleUI）
        if (FindObjectOfType<BattleUI>() == null)
        {
            GameObject uiObj = new GameObject("BattleUI", typeof(BattleUI));
            // BattleUI 的 Awake 会自动找到本 BattleManager
        }

        StartBattle();
    }

    /// <summary>
    /// 缓存必要的组件引用
    /// </summary>
    private void CacheComponents()
    {
        if (_playerObject != null)
        {
            _playerEnergy = _playerObject.GetComponent<currentEnergy>();
            _playerBlock = _playerObject.GetComponent<playerBlock>();
            _playerHP = _playerObject.GetComponent<playerHP>();
        }

        if (_enemyObject != null)
        {
            _enemyBlock = _enemyObject.GetComponent<enemyBlock>();
            _enemyHP = _enemyObject.GetComponent<enemyHP>();
        }
    }

    /// <summary>
    /// 开始战斗
    /// </summary>
    public void StartBattle()
    {
        if (_isBattleOver) return;
        _currentTurn = 1;
        _turnCoroutine = StartCoroutine(RunTurnLoop());
    }

    /// <summary>
    /// 核心回合循环协程
    /// </summary>
    private IEnumerator RunTurnLoop()
    {
        while (!_isBattleOver)
        {
            yield return StartCoroutine(ExecutePhase_PlayerTurnStart());
            if (_isBattleOver) yield break;

            yield return StartCoroutine(ExecutePhase_EnergyRefill());
            if (_isBattleOver) yield break;

            yield return StartCoroutine(ExecutePhase_PlayerAction());
            if (_isBattleOver) yield break;

            yield return StartCoroutine(ExecutePhase_EnemyAction());
            if (_isBattleOver) yield break;

            yield return StartCoroutine(ExecutePhase_ShieldClear());
            if (_isBattleOver) yield break;

            yield return StartCoroutine(ExecutePhase_TurnEnd());

            _currentTurn++;
        }
    }

    // ==========================================
    //  各阶段执行
    // ==========================================

    /// <summary>
    /// 阶段1: 玩家回合开始
    /// </summary>
    private IEnumerator ExecutePhase_PlayerTurnStart()
    {
        SetPhase(BattlePhase.PlayerTurnStart);
        OnTurnStarted?.Invoke(_currentTurn);
        Debug.Log($"=== 第 {_currentTurn} 回合 ===");

        yield return new WaitForSeconds(_phaseDelay * 0.5f);
    }

    /// <summary>
    /// 阶段2: 获得基础能量
    /// </summary>
    private IEnumerator ExecutePhase_EnergyRefill()
    {
        SetPhase(BattlePhase.EnergyRefill);

        if (_playerEnergy != null)
        {
            _playerEnergy.RefillEnergy();
            Debug.Log($"能量已恢复至 {_playerEnergy.CurrentEnergy}/{_playerEnergy.MaxEnergy}");
        }

        OnEnergyRefilled?.Invoke();
        yield return new WaitForSeconds(_phaseDelay * 0.5f);
    }

    /// <summary>
    /// 阶段3: 我方行动 - 等待玩家操作
    /// </summary>
    private IEnumerator ExecutePhase_PlayerAction()
    {
        SetPhase(BattlePhase.PlayerAction);
        _playerActionEnded = false;
        OnPlayerActionStarted?.Invoke();
        Debug.Log("我方行动阶段 - 等待操作...");

        // 等待玩家点击结束回合或完成操作
        while (!_playerActionEnded && !_isBattleOver)
        {
            yield return null;
        }

        OnPlayerActionEnded?.Invoke();
        Debug.Log("我方行动结束");
        yield return new WaitForSeconds(_phaseDelay * 0.3f);
    }

    /// <summary>
    /// 阶段4: 敌人行动
    /// </summary>
    private IEnumerator ExecutePhase_EnemyAction()
    {
        SetPhase(BattlePhase.EnemyAction);
        OnEnemyActionStarted?.Invoke();
        Debug.Log("敌人行动阶段");

        // 敌人AI：简单攻击
        yield return StartCoroutine(EnemyAIAttack());

        OnEnemyActionEnded?.Invoke();
        Debug.Log("敌人行动结束");
        yield return new WaitForSeconds(_phaseDelay);
    }

    /// <summary>
    /// 阶段5: 护盾清空
    /// </summary>
    private IEnumerator ExecutePhase_ShieldClear()
    {
        SetPhase(BattlePhase.ShieldClear);

        if (_playerBlock != null)
        {
            _playerBlock.ResetBlock();
        }
        if (_enemyBlock != null)
        {
            _enemyBlock.ResetBlock();
        }

        OnShieldCleared?.Invoke();
        Debug.Log("护盾已清空");
        yield return new WaitForSeconds(_phaseDelay * 0.5f);
    }

    /// <summary>
    /// 阶段6: 回合结束
    /// </summary>
    private IEnumerator ExecutePhase_TurnEnd()
    {
        SetPhase(BattlePhase.TurnEnd);
        OnTurnEnded?.Invoke(_currentTurn);
        Debug.Log($"第 {_currentTurn} 回合结束");
        yield return new WaitForSeconds(_phaseDelay * 0.5f);
    }

    // ==========================================
    //  敌人 AI
    // ==========================================

    /// <summary>
    /// 敌人简单AI：攻击玩家
    /// </summary>
    private IEnumerator EnemyAIAttack()
    {
        if (_enemyObject == null || _playerHP == null)
            yield break;

        // 简单攻击逻辑：造成相当于敌人回合数的伤害（最低1点）
        int damage = Mathf.Max(1, _currentTurn);
        
        // 动画/等待效果
        yield return new WaitForSeconds(_phaseDelay * 0.5f);

        _playerHP.TakeDamage(damage);
        Debug.Log($"敌人造成 {damage} 点伤害，玩家剩余 HP: {_playerHP.CurrentHP}/{_playerHP.MaxHP}");

        // 检查玩家是否战败
        if (_playerHP.CurrentHP <= 0)
        {
            EndBattle("lose");
        }
    }

    // ==========================================
    //  公开方法
    // ==========================================

    /// <summary>
    /// 玩家结束自己的行动回合
    /// 由UI按钮或输入调用
    /// </summary>
    public void EndPlayerTurn()
    {
        if (_currentPhase == BattlePhase.PlayerAction && !_isBattleOver)
        {
            _playerActionEnded = true;
        }
    }

    /// <summary>
    /// 玩家对敌人造成伤害（由卡牌等调用）
    /// </summary>
    public void DealDamageToEnemy(int damage)
    {
        if (_enemyHP == null || _isBattleOver) return;

        _enemyHP.TakeDamage(damage);
        Debug.Log($"对敌人造成 {damage} 点伤害，敌人剩余 HP: {_enemyHP.CurrentHP}/{_enemyHP.MaxHP}");

        if (_enemyHP.CurrentHP <= 0)
        {
            EndBattle("win");
        }
    }

    /// <summary>
    /// 结束战斗
    /// </summary>
    public void EndBattle(string result)
    {
        if (_isBattleOver) return;
        _isBattleOver = true;

        if (_turnCoroutine != null)
        {
            StopCoroutine(_turnCoroutine);
            _turnCoroutine = null;
        }

        OnBattleOver?.Invoke(result);
        Debug.Log($"战斗结束！结果: {result}");
    }

    /// <summary>
    /// 设置当前阶段并触发事件
    /// </summary>
    private void SetPhase(BattlePhase phase)
    {
        _currentPhase = phase;
        OnPhaseChanged?.Invoke(phase);
    }

#if UNITY_EDITOR
    // ==========================================
    //  Gizmos：编辑器可视化
    // ==========================================

    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;

        UnityEditor.Handles.Label(transform.position, $"回合: {_currentTurn}\n阶段: {_currentPhase}");
    }
#endif
}
