using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ElementalSpire.Cards;

/// <summary>
/// 战斗管理器 - 负责战斗逻辑与卡牌系统，回合流程委托给 TurnManager。
/// </summary>
public class BattleManager : MonoBehaviour
{
    [Header("战斗对象绑定")]
    [SerializeField] private GameObject _playerObject;
    [SerializeField] private GameObject _enemyObject;

    [Header("初始牌组")]
    [SerializeField] private DeckPreset _startingDeck = DeckPreset.All;
    [SerializeField] private bool _useDemoMixedDeck = true;

    [Header("场景流转")]
    [SerializeField] private bool _returnToMapOnWin = false;
    [SerializeField] private string _mapSceneName = "MapScene";
    [SerializeField] private float _returnToMapDelay = 1.0f;

    private currentEnergy _playerEnergy;
    private playerBlock _playerBlock;
    private playerHP _playerHP;
    private enemyBlock _enemyBlock;
    private enemyHP _enemyHP;
    private drawPile _drawPile;
    private handCards _handCards;
    private discardPile _discardPile;
    private PlayerState _playerState;
    private EnemyState _enemyState;
    private EnemyAI _enemyAI;

    private TurnManager _turnManager;
    private DeckManager _deckManager;
    private CardEffectResolver _cardEffectResolver;
    private bool _isBattleOver = false;
    private bool _returningToMap = false;

    public event Action<string> OnBattleOver;
    public event Action OnHandChanged;
    public event Action OnBattleInfoChanged;
    public event Action<string> OnBattleLog;

    public GameObject PlayerObject => _playerObject;
    public GameObject EnemyObject => _enemyObject;
    public TurnManager TurnManager => _turnManager;
    public DeckManager DeckManager => _deckManager;
    public CardEffectResolver CardEffectResolver => _cardEffectResolver;
    public currentEnergy PlayerEnergy => _playerEnergy;
    public playerBlock PlayerBlock => _playerBlock;
    public PlayerState PlayerState => _playerState;
    public EnemyState EnemyState => _enemyState;
    public bool IsBattleOver => _isBattleOver;

    void Awake()
    {
        CacheComponents();

        _playerState?.ResetCombatState();
        _enemyState?.ResetCombatState();

        GameObject turnObj = new GameObject("TurnManager", typeof(TurnManager));
        _turnManager = turnObj.GetComponent<TurnManager>();
        _turnManager.Initialize(_playerEnergy, _playerBlock, _enemyBlock, _playerState);

        _deckManager = new DeckManager(_drawPile, _handCards, _discardPile);
        _deckManager.Initialize(BuildInitialDeck(), !_useDemoMixedDeck);

        _cardEffectResolver = new CardEffectResolver(this, _deckManager, _playerEnergy, _playerBlock, _playerHP, _playerState);

        _turnManager.OnDrawPhase += OnDrawPhase;
        _turnManager.OnDiscardPhase += OnDiscardPhase;
        _turnManager.OnPoisonTickPhase += OnPoisonTickPhase;
        _turnManager.OnEnemyActionStarted += OnEnemyTurnStart;
        _turnManager.OnEnemyActionEnded += OnEnemyTurnEnd;
        _turnManager.OnTurnStarted += OnTurnStarted;
    }

    void Start()
    {
        if (FindObjectOfType<BattleUI>() == null)
        {
            new GameObject("BattleUI", typeof(BattleUI));
        }

        LogBattleEvent(_useDemoMixedDeck
            ? "演示混合牌组已启用：开局可直接测试火/毒/水反应。"
            : $"起始牌组：{_startingDeck}");
        _turnManager.StartBattle();
    }

    void OnDestroy()
    {
        if (_turnManager != null)
        {
            _turnManager.OnDrawPhase -= OnDrawPhase;
            _turnManager.OnDiscardPhase -= OnDiscardPhase;
            _turnManager.OnPoisonTickPhase -= OnPoisonTickPhase;
            _turnManager.OnEnemyActionStarted -= OnEnemyTurnStart;
            _turnManager.OnEnemyActionEnded -= OnEnemyTurnEnd;
            _turnManager.OnTurnStarted -= OnTurnStarted;
        }

        _cardEffectResolver?.Dispose();
    }

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
            _playerState = _playerObject.GetComponent<PlayerState>();
        }

        if (_enemyObject != null)
        {
            _enemyBlock = _enemyObject.GetComponent<enemyBlock>();
            _enemyHP = _enemyObject.GetComponent<enemyHP>();
            _enemyState = _enemyObject.GetComponent<EnemyState>();
            _enemyAI = _enemyObject.GetComponent<EnemyAI>();
        }
    }

    private IEnumerable<CardData> BuildInitialDeck()
    {
        var gameManager = GameManager.Instance;
        if (!_useDemoMixedDeck && gameManager != null && gameManager.playerCardBag != null && gameManager.playerCardBag.Count > 0)
        {
            var deck = CardDeckLibrary.GetStarterDeck().ToList();
            var allCards = CardDeckLibrary.GetAllCards();
            foreach (string cardId in gameManager.playerCardBag)
            {
                CardData card = allCards.FirstOrDefault(data => data.cardId == cardId);
                if (card != null)
                    deck.Add(card);
                else
                    Debug.LogWarning($"[BattleManager] 未找到奖励卡牌ID: {cardId}");
            }

            if (deck.Count > 0)
                return deck;
        }

        if (_useDemoMixedDeck)
        {
            return new[]
            {
                CardDeckLibrary.GetCardById("fire_hilt_strike"),
                CardDeckLibrary.GetCardById("water_blade"),
                CardDeckLibrary.GetCardById("poison_blade"),
                CardDeckLibrary.GetCardById("water_curtain"),
                CardDeckLibrary.GetCardById("color_blank_strike"),
                CardDeckLibrary.GetCardById("poison_sneak_needle"),
                CardDeckLibrary.GetCardById("fire_double_strike"),
                CardDeckLibrary.GetCardById("water_gather"),
                CardDeckLibrary.GetCardById("poison_smoke_bomb"),
                CardDeckLibrary.GetCardById("color_neutral_arrow"),
                CardDeckLibrary.GetCardById("fire_bloodletting"),
                CardDeckLibrary.GetCardById("water_surge"),
                CardDeckLibrary.GetCardById("poison_roll"),
                CardDeckLibrary.GetCardById("fire_perfect_strike"),
                CardDeckLibrary.GetCardById("water_wave_guard"),
            };
        }

        return CardDeckLibrary.GetCardsByDeckPreset(_startingDeck);
    }

    private void OnTurnStarted(int turn)
    {
        _cardEffectResolver?.ClearElementAttachmentAtTurnStart();

        if (_playerState != null)
        {
            if (_playerState.DemonFormPowerPerTurn > 0)
            {
                _playerState.AddPower(_playerState.DemonFormPowerPerTurn);
                LogBattleEvent($"恶魔形态：获得 {_playerState.DemonFormPowerPerTurn} 点力量。");
            }

            if (_playerState.WaterResonanceActive)
            {
                _playerState.AddWater(1);
                LogBattleEvent($"水脉共鸣：获得1点水源，当前水源 {_playerState.WaterSource}。");
            }
        }

        OnBattleInfoChanged?.Invoke();
        LogBattleEvent($"第 {turn} 回合开始。");
    }

    private void OnDrawPhase()
    {
        _deckManager?.DrawCards(5);
        OnHandChanged?.Invoke();
    }

    private void OnDiscardPhase()
    {
        _deckManager?.DiscardAllHand();
        OnHandChanged?.Invoke();
    }

    private void OnPoisonTickPhase()
    {
        if (_enemyState == null || _enemyHP == null) return;

        int poisonDamage = _enemyState.TriggerPoisonTick();
        if (poisonDamage > 0)
        {
            _enemyHP.TakeDamage(poisonDamage);
            LogBattleEvent($"中毒结算：造成 {poisonDamage} 点伤害，剩余中毒 {_enemyState.PoisonStacks}。");
            OnBattleInfoChanged?.Invoke();

            // 检查敌人是否被毒死
            if (_enemyHP.CurrentHP <= 0)
            {
                EndBattle("win");
            }
        }
    }

    private void OnEnemyTurnStart()
    {
        LogBattleEvent("敌人行动开始。");
        StartCoroutine(EnemyAIAttack());
    }

    private void OnEnemyTurnEnd()
    {
        LogBattleEvent("敌人行动结束。");
    }

    private IEnumerator EnemyAIAttack()
    {
        if (_enemyObject == null || _playerHP == null)
        {
            _turnManager?.SignalEnemyActionComplete();
            yield break;
        }

        if (_enemyAI != null)
        {
            _enemyAI.ExecuteTurn();
        }
        else
        {
            int damage = Mathf.Max(1, _turnManager != null ? _turnManager.CurrentTurn : 1);
            _playerHP.TakeDamage(damage);
            LogBattleEvent($"敌人造成 {damage} 点伤害。");
        }

        yield return new WaitForSeconds(0.3f);

        OnBattleInfoChanged?.Invoke();

        if (_playerHP.CurrentHP <= 0)
        {
            EndBattle("lose");
        }

        _turnManager?.SignalEnemyActionComplete();
    }

    public void DealDamageToEnemy(int damage)
    {
        if (_enemyHP == null || _isBattleOver) return;

        _enemyHP.TakeDamage(damage);
        LogBattleEvent($"敌人受到 {damage} 点伤害，HP {_enemyHP.CurrentHP}/{_enemyHP.MaxHP}。");

        OnBattleInfoChanged?.Invoke();

        if (_enemyHP.CurrentHP <= 0)
        {
            EndBattle("win");
        }
    }

    public void PlayCard(CardData cardData)
    {
        PlayCard(cardData, ElementType.None);
    }

    public void PlayCard(CardData cardData, ElementType chosenElement)
    {
        if (_isBattleOver) return;
        if (_turnManager == null || _turnManager.CurrentPhase != BattlePhase.PlayerAction) return;
        if (_deckManager == null || !_deckManager.handCards.Contains(cardData)) return;

        if (!CanAffordCard(cardData))
        {
            LogBattleEvent($"资源不足：无法打出 {cardData.cardName}。");
            return;
        }

        _playerEnergy?.SpendEnergy(cardData.cost);
        SpendWaterCost(cardData);

        LogBattleEvent($"打出 {cardData.cardName}。");
        _cardEffectResolver?.ResolveEffect(cardData, chosenElement);
        _deckManager.PlayCard(cardData);

        OnHandChanged?.Invoke();
        OnBattleInfoChanged?.Invoke();
    }

    public bool CanAffordCard(CardData cardData)
    {
        if (cardData == null) return false;

        bool hasEnergy = _playerEnergy == null || _playerEnergy.HasEnoughEnergy(cardData.cost);
        bool hasWater = _playerState == null || _playerState.HasEnoughWater(cardData.waterCost);
        return hasEnergy && hasWater;
    }

    private void SpendWaterCost(CardData cardData)
    {
        if (cardData == null || cardData.waterCost <= 0 || _playerState == null)
            return;

        if (!_playerState.SpendWater(cardData.waterCost))
            return;

        LogBattleEvent($"消耗 {cardData.waterCost} 点水源，当前水源 {_playerState.WaterSource}。");

        if (_playerState.LakeEchoBlockPerWater > 0)
        {
            int block = _playerState.LakeEchoBlockPerWater * cardData.waterCost;
            _playerBlock?.AddBlock(block);
            LogBattleEvent($"星湖回响：获得 {block} 点格挡。");
        }

        if (_playerState.WaterResonanceActive && !_playerState.WaterResonanceUsedThisTurn)
        {
            _playerState.MarkWaterResonanceUsed();
            _deckManager.DrawCards(1);
            LogBattleEvent("水脉共鸣：本回合首次消耗水源，抽1张牌。");
        }
    }

    public void DealDamageToPlayer(int damage)
    {
        if (_playerHP == null || _isBattleOver) return;
        _playerHP.TakeDamage(damage);
        LogBattleEvent($"玩家受到 {damage} 点伤害，HP {_playerHP.CurrentHP}/{_playerHP.MaxHP}。");
        OnBattleInfoChanged?.Invoke();

        if (_playerHP.CurrentHP <= 0)
            EndBattle("lose");
    }

    public void EndBattle(string result)
    {
        if (_isBattleOver) return;
        _isBattleOver = true;

        if (_turnManager != null)
            _turnManager.StopBattle();

        Debug.Log($"[BattleManager] EndBattle({result}), isBattleWin即将设为{result == "win"}，GameManager.Instance={(GameManager.Instance != null ? "存在" : "为空")}");

        SyncGameManagerBattleResult(result);
        OnBattleOver?.Invoke(result);
        LogBattleEvent(result == "win" ? "战斗胜利。" : "战斗失败。");

        if (result == "win" && _returnToMapOnWin && GameManager.Instance != null && !_returningToMap)
        {
            StartCoroutine(ReturnToMapAfterDelay());
        }
    }

    private void SyncGameManagerBattleResult(string result)
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("[BattleManager] SyncGameManagerBattleResult: GameManager.Instance 为空！");
            return;
        }

        GameManager.Instance.isBattleWin = result == "win";
        Debug.Log($"[BattleManager] SyncGameManagerBattleResult: 已设置 isBattleWin={GameManager.Instance.isBattleWin}");

        if (_playerHP != null)
            GameManager.Instance.playerHp = _playerHP.CurrentHP;
        if (_playerEnergy != null)
            GameManager.Instance.currentEnergy = _playerEnergy.CurrentEnergy;
        if (_playerBlock != null)
            GameManager.Instance.playerBlock = _playerBlock.CurrentBlock;
    }

    private IEnumerator ReturnToMapAfterDelay()
    {
        _returningToMap = true;
        yield return new WaitForSeconds(_returnToMapDelay);
        SceneManager.LoadScene(_mapSceneName);
    }

    public void LogBattleEvent(string message)
    {
        if (string.IsNullOrEmpty(message)) return;

        string formatted = "[Battle] " + message;
        Debug.Log(formatted);
        OnBattleLog?.Invoke(message);
    }

    public void NotifyHandChanged()
    {
        OnHandChanged?.Invoke();
    }

    public void NotifyBattleInfoChanged()
    {
        OnBattleInfoChanged?.Invoke();
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




