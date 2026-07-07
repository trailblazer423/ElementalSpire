using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ElementalSpire.Cards;

/// <summary>
/// 战斗管理器 - 负责战斗逻辑与卡牌系统，回合流程委托给 TurnManager
/// </summary>
public class BattleManager : MonoBehaviour
{
    [Header("战斗对象绑定")]
    [SerializeField] private GameObject _playerObject;
    [SerializeField] private GameObject _enemyObject;

    [Header("初始牌组")]
    [SerializeField] private DeckPreset _startingDeck = DeckPreset.Fire;

    // 组件缓存
    private currentEnergy _playerEnergy;
    private playerBlock _playerBlock;
    private playerHP _playerHP;
    private enemyBlock _enemyBlock;
    private enemyHP _enemyHP;
    private drawPile _drawPile;
    private handCards _handCards;
    private discardPile _discardPile;

    private TurnManager _turnManager;
    private DeckManager _deckManager;
    private CardEffectResolver _cardEffectResolver;
    private bool _isBattleOver = false;

    // ===== 事件 =====
    public event Action<string> OnBattleOver;
    /// <summary>手牌变化时触发（供 UI 刷新）</summary>
    public event Action OnHandChanged;
    /// <summary>战斗信息变化时触发（HP/能量/护盾）</summary>
    public event Action OnBattleInfoChanged;

    // ===== 公开属性 =====
    public GameObject PlayerObject => _playerObject;
    public GameObject EnemyObject => _enemyObject;
    public TurnManager TurnManager => _turnManager;
    public DeckManager DeckManager => _deckManager;
    public CardEffectResolver CardEffectResolver => _cardEffectResolver;
    public currentEnergy PlayerEnergy => _playerEnergy;
    public playerBlock PlayerBlock => _playerBlock;
    public bool IsBattleOver => _isBattleOver;

    // ==========================================
    //  生命周期
    // ==========================================

    void Awake()
    {
        CacheComponents();

        // 创建独立的 TurnManager
        GameObject turnObj = new GameObject("TurnManager", typeof(TurnManager));
        _turnManager = turnObj.GetComponent<TurnManager>();
        _turnManager.Initialize(_playerEnergy, _playerBlock, _enemyBlock);

        // 初始化牌组系统
        _deckManager = new DeckManager(_drawPile, _handCards, _discardPile);
        _deckManager.Initialize(_startingDeck);

        _cardEffectResolver = new CardEffectResolver(this, _deckManager, _playerEnergy, _playerBlock, _playerHP);

        // 监听回合事件
        _turnManager.OnDrawPhase += OnDrawPhase;
        _turnManager.OnDiscardPhase += OnDiscardPhase;
        _turnManager.OnEnemyActionStarted += OnEnemyTurnStart;
        _turnManager.OnEnemyActionEnded += OnEnemyTurnEnd;
        _turnManager.OnTurnStarted += OnTurnStarted;
    }

    void Start()
    {
        // 自动创建 UI（如果场景中没有 BattleUI）
        if (FindObjectOfType<BattleUI>() == null)
        {
            GameObject uiObj = new GameObject("BattleUI", typeof(BattleUI));
        }

        // 启动回合循环
        _turnManager.StartBattle();
    }

    void OnDestroy()
    {
        if (_turnManager != null)
        {
            _turnManager.OnDrawPhase -= OnDrawPhase;
            _turnManager.OnDiscardPhase -= OnDiscardPhase;
            _turnManager.OnEnemyActionStarted -= OnEnemyTurnStart;
            _turnManager.OnEnemyActionEnded -= OnEnemyTurnEnd;
            _turnManager.OnTurnStarted -= OnTurnStarted;
        }
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
            _drawPile = _playerObject.GetComponent<drawPile>();
            _handCards = _playerObject.GetComponent<handCards>();
            _discardPile = _playerObject.GetComponent<discardPile>();
        }

        if (_enemyObject != null)
        {
            _enemyBlock = _enemyObject.GetComponent<enemyBlock>();
            _enemyHP = _enemyObject.GetComponent<enemyHP>();
        }
    }

    /// <summary>
    /// 每回合开始时记录回合数
    /// </summary>
    private void OnTurnStarted(int turn)
    {
        Debug.Log($"[BattleManager] 第 {turn} 回合开始");
    }

    /// <summary>
    /// 抽牌阶段：抽 5 张牌
    /// </summary>
    private void OnDrawPhase()
    {
        _deckManager?.DrawCards(5);
        OnHandChanged?.Invoke();
    }

    /// <summary>
    /// 弃牌阶段：弃掉所有未使用的手牌
    /// </summary>
    private void OnDiscardPhase()
    {
        _deckManager?.DiscardAllHand();
        OnHandChanged?.Invoke();
    }

    // ==========================================
    //  敌人 AI（响应 TurnManager 事件）
    // ==========================================

    /// <summary>
    /// 敌人行动阶段开始 -> 启动 AI 协程
    /// </summary>
    private void OnEnemyTurnStart()
    {
        Debug.Log("[BattleManager] 敌人行动开始，启动 AI");
        StartCoroutine(EnemyAIAttack());
    }

    /// <summary>
    /// 敌人行动阶段结束
    /// </summary>
    private void OnEnemyTurnEnd()
    {
        Debug.Log("[BattleManager] 敌人行动结束");
    }

    /// <summary>
    /// 敌人简单AI：攻击玩家
    /// </summary>
    private IEnumerator EnemyAIAttack()
    {
        if (_enemyObject == null || _playerHP == null)
        {
            _turnManager?.SignalEnemyActionComplete();
            yield break;
        }

        // 简单攻击逻辑：造成相当于当前回合数的伤害（最低1点）
        int damage = Mathf.Max(1, _turnManager != null ? _turnManager.CurrentTurn : 1);

        yield return new WaitForSeconds(0.3f);

        _playerHP.TakeDamage(damage);
        Debug.Log($"[BattleManager] 敌人造成 {damage} 点伤害，玩家 HP: {_playerHP.CurrentHP}/{_playerHP.MaxHP}");

        OnBattleInfoChanged?.Invoke();

        // 检查玩家是否战败
        if (_playerHP.CurrentHP <= 0)
        {
            EndBattle("lose");
        }

        // 通知 TurnManager 敌人行动完成
        _turnManager?.SignalEnemyActionComplete();
    }

    // ==========================================
    //  公开方法
    // ==========================================

    /// <summary>
    /// 玩家对敌人造成伤害（由卡牌等调用）
    /// </summary>
    public void DealDamageToEnemy(int damage)
    {
        if (_enemyHP == null || _isBattleOver) return;

        _enemyHP.TakeDamage(damage);
        Debug.Log($"[BattleManager] 对敌人造成 {damage} 点伤害，敌人 HP: {_enemyHP.CurrentHP}/{_enemyHP.MaxHP}");

        OnBattleInfoChanged?.Invoke();

        if (_enemyHP.CurrentHP <= 0)
        {
            EndBattle("win");
        }
    }

    /// <summary>
    /// 打出一张牌
    /// </summary>
    public void PlayCard(CardData cardData)
    {
        if (_isBattleOver) return;
        if (_turnManager == null || _turnManager.CurrentPhase != BattlePhase.PlayerAction) return;
        if (_deckManager == null || !_deckManager.handCards.Contains(cardData)) return;

        // 检查能量
        if (_playerEnergy != null && !_playerEnergy.HasEnoughEnergy(cardData.cost))
        {
            Debug.Log($"[BattleManager] 能量不足: 需要 {cardData.cost}，当前 {_playerEnergy.CurrentEnergy}");
            return;
        }

        // 花费能量
        _playerEnergy?.SpendEnergy(cardData.cost);

        // 解析效果
        _cardEffectResolver?.ResolveEffect(cardData);

        // 从手牌移除（消耗或进弃牌堆）
        _deckManager.PlayCard(cardData);

        Debug.Log($"[BattleManager] 打出: {cardData.cardName}");

        // 通知 UI 刷新
        OnHandChanged?.Invoke();
    }

    /// <summary>
    /// 玩家受到伤害（供 CardEffectResolver 调用）
    /// </summary>
    public void DealDamageToPlayer(int damage)
    {
        if (_playerHP == null || _isBattleOver) return;
        _playerHP.TakeDamage(damage);
        OnBattleInfoChanged?.Invoke();
    }

    /// <summary>
    /// 结束战斗
    /// </summary>
    public void EndBattle(string result)
    {
        if (_isBattleOver) return;
        _isBattleOver = true;

        // 停止回合循环
        if (_turnManager != null)
            _turnManager.StopBattle();

        OnBattleOver?.Invoke(result);
        Debug.Log($"[BattleManager] 战斗结束！结果: {result}");
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;
        if (_turnManager != null)
        {
            UnityEditor.Handles.Label(transform.position,
                $"回合: {_turnManager.CurrentTurn}\n阶段: {_turnManager.CurrentPhase}");
        }
    }
#endif
}
