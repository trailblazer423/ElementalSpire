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

    [Header("敌人配置")]
    [SerializeField] private EnemyData[] _normalEnemyPool;  // 普通/精英敌人池
    [SerializeField] private EnemyData _bossEnemyData;       // Boss 敌人数据
    [SerializeField] private GameObject _enemyPrefab;         // 多敌人模式生成用

    [Header("初始牌组")]
    [SerializeField] private DeckPreset _startingDeck = DeckPreset.All;
    [SerializeField] private bool _useDemoMixedDeck = true;

    [Header("场景流转")]
    [SerializeField] private bool _returnToMapOnWin = false;
    [SerializeField] private string _mapSceneName = "MapScene";
    [SerializeField] private string _rewardSceneName = "CardDraftScene";
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
    private EnemyController _enemyController;
    private MultiEnemyManager _multiEnemyManager;
    private bool _isDaoDunEncounter;

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
    public MultiEnemyManager MultiEnemyManager => _multiEnemyManager;
    public bool IsBattleOver => _isBattleOver;

    void Awake()
    {
        EnsureGameManager();
        _multiEnemyManager = GetComponent<MultiEnemyManager>();
        CacheComponents();

        // 从 GameManager 读取角色状态到战斗组件
        if (GameManager.Instance != null)
        {
            if (_playerHP != null)
                _playerHP.CurrentHP = GameManager.Instance.playerHp;

            var maxHpComp = _playerObject?.GetComponent<playerMaxHP>();
            if (maxHpComp != null)
                maxHpComp.maxHP = GameManager.Instance.playerMaxHp;

            var maxEnergyComp = _playerObject?.GetComponent<maxEnergy>();
            if (maxEnergyComp != null)
                maxEnergyComp.energyMax = GameManager.Instance.maxEnergy;

            if (_playerEnergy != null)
                _playerEnergy.CurrentEnergy = GameManager.Instance.currentEnergy;

            // 格挡值每场战斗重置为0，不继承
        }

        // 根据地图节点选择敌人数据
        SelectEnemyData();

        _playerState?.ResetCombatState();
        _enemyState?.ResetCombatState();

        GameObject turnObj = new GameObject("TurnManager", typeof(TurnManager));
        _turnManager = turnObj.GetComponent<TurnManager>();
        _turnManager.Initialize(_playerEnergy, _playerBlock, _enemyBlock, _playerState);

        // 先注册事件，这样即使牌组初始化失败，UI 和回合流程也能正常工作
        _turnManager.OnDrawPhase += OnDrawPhase;
        _turnManager.OnDiscardPhase += OnDiscardPhase;
        _turnManager.OnPoisonTickPhase += OnPoisonTickPhase;
        _turnManager.OnEnemyActionStarted += OnEnemyTurnStart;
        _turnManager.OnEnemyActionEnded += OnEnemyTurnEnd;
        _turnManager.OnTurnStarted += OnTurnStarted;

        if (_drawPile == null || _handCards == null || _discardPile == null)
        {
            Debug.LogError("[BattleManager] drawPile/handCards/discardPile 组件缺失，无法初始化牌组！请确保 Player 对象上挂载了这些脚本。");
            return;
        }

        _deckManager = new DeckManager(_drawPile, _handCards, _discardPile);
        _deckManager.Initialize(BuildInitialDeck(), shuffle: true);

        _cardEffectResolver = new CardEffectResolver(this, _deckManager, _playerEnergy, _playerBlock, _playerHP, _playerState);
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

        // 确保意图已生成并通知 UI 刷新（解决 Awake 时序问题）
        if (_enemyController != null && _enemyController.enemyData != null)
            _enemyController.DecideNextIntent();
        StartCoroutine(DelayedRefreshUI());
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
        // 自动查找 Player 对象（如果 Inspector 未赋值）
        if (_playerObject == null)
            _playerObject = GameObject.FindGameObjectWithTag("Player");
        if (_playerObject == null)
            _playerObject = GameObject.Find("Player");

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
        else
        {
            Debug.LogError("[BattleManager] 未找到 Player 对象！请确保场景中有 Tag 为 \"Player\" 或名称为 \"Player\" 的游戏对象。");
        }

        // 自动查找 Enemy 对象（如果 Inspector 未赋值）
        if (_enemyObject == null)
            _enemyObject = GameObject.FindGameObjectWithTag("Enemy");
        if (_enemyObject == null)
            _enemyObject = GameObject.Find("Enemy");

        if (_enemyObject != null)
        {
            _enemyBlock = _enemyObject.GetComponent<enemyBlock>();
            _enemyHP = _enemyObject.GetComponent<enemyHP>();
            _enemyState = _enemyObject.GetComponent<EnemyState>();
            _enemyAI = _enemyObject.GetComponent<EnemyAI>();
            _enemyController = _enemyObject.GetComponent<EnemyController>();
        }
        else
        {
            Debug.LogError("[BattleManager] 未找到 Enemy 对象！请确保场景中有 Tag 为 \"Enemy\" 或名称为 \"Enemy\" 的游戏对象。");
        }
    }

    private IEnumerable<CardInstance> BuildInitialDeck()
    {
        var gameManager = GameManager.Instance;

        // 优先使用地图中存储的玩家卡牌背包（已包含初始牌 + 选牌 + 奖励牌）
        if (gameManager != null && gameManager.playerCardBag != null && gameManager.playerCardBag.Count > 0)
        {
            var deck = new List<CardInstance>();
            foreach (string savedCardId in gameManager.playerCardBag)
            {
                CardInstance card = CreateCardInstanceFromSavedId(savedCardId);
                if (card != null)
                    deck.Add(card);
                else
                    Debug.LogWarning($"[BattleManager] 未找到卡牌ID: {savedCardId}");
            }

            if (deck.Count > 0)
                return deck;
        }

        // 无地图数据时使用演示牌组（测试用）
        if (_useDemoMixedDeck)
        {
            return new[]
            {
                new CardInstance("fire_hilt_strike"),
                new CardInstance("water_blade"),
                new CardInstance("poison_blade"),
                new CardInstance("water_curtain"),
                new CardInstance("color_blank_strike"),
                new CardInstance("poison_sneak_needle"),
                new CardInstance("fire_double_strike"),
                new CardInstance("water_gather"),
                new CardInstance("poison_smoke_bomb"),
                new CardInstance("color_neutral_arrow"),
                new CardInstance("fire_bloodletting"),
                new CardInstance("water_surge"),
                new CardInstance("poison_roll"),
                new CardInstance("fire_perfect_strike"),
                new CardInstance("water_wave_guard"),
            };
        }

        return CardDeckLibrary.GetCardsByDeckPreset(_startingDeck)
            .Select(card => new CardInstance(card.cardId));
    }

    private CardInstance CreateCardInstanceFromSavedId(string savedCardId)
    {
        if (string.IsNullOrEmpty(savedCardId))
            return null;

        string cardId = savedCardId;
        bool isUpgraded = false;
        if (cardId.EndsWith("+"))
        {
            isUpgraded = true;
            cardId = cardId.Substring(0, cardId.Length - 1);
        }

        CardData cardData = CardDeckLibrary.GetCardById(cardId);
        return cardData != null ? new CardInstance(cardId, isUpgraded) : null;
    }

    private void OnTurnStarted(int turn)
    {
        _cardEffectResolver?.ClearElementAttachmentAtTurnStart();
        _playerState?.TickVulnerable();

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
        if (_multiEnemyManager != null)
        {
            foreach (EnemyUnit enemy in _multiEnemyManager.GetAliveEnemies())
            {
                if (enemy.state == null) continue;

                int poisonDamage = enemy.state.TriggerPoisonTick();
                if (poisonDamage <= 0) continue;

                enemy.DealDamage(poisonDamage);
                LogBattleEvent($"{enemy.DisplayName} 中毒结算：受到 {poisonDamage} 点伤害，剩余中毒 {enemy.state.PoisonStacks}。");
            }

            if (_multiEnemyManager.AllDead)
                EndBattle("win");
        }
        else if (_enemyState != null && _enemyHP != null)
        {
            int poisonDamage = _enemyState.TriggerPoisonTick();
            if (poisonDamage > 0)
            {
                _enemyHP.TakeDamage(poisonDamage);
                LogBattleEvent($"敌方中毒结算：造成 {poisonDamage} 点伤害，剩余中毒 {_enemyState.PoisonStacks}。");
                if (_enemyHP.CurrentHP <= 0)
                    EndBattle("win");
            }
        }

        if (_playerState != null && _playerHP != null)
        {
            int poisonDamage = _playerState.TickPoison();
            if (poisonDamage > 0)
            {
                _playerHP.TakeDamage(poisonDamage);
                LogBattleEvent($"我方中毒结算：受到 {poisonDamage} 点伤害，剩余中毒 {_playerState.PoisonStacks}。");
                if (_playerHP.CurrentHP <= 0)
                    EndBattle("lose");
            }
        }

        OnBattleInfoChanged?.Invoke();
    }

    private void OnEnemyTurnStart()
    {
        LogBattleEvent("敌人行动开始。");
        StartCoroutine(EnemyAIAttack());
    }

    private void OnEnemyTurnEnd()
    {
        // 决定下一次意图，供玩家在己方回合查看
        if (_multiEnemyManager != null)
            _multiEnemyManager.AllDecideNextIntent();
        else
            _enemyController?.DecideNextIntent();

        // 通知 UI 更新（包含意图文字）
        OnBattleInfoChanged?.Invoke();

        LogBattleEvent("敌人行动结束。");
    }

    private IEnumerator EnemyAIAttack()
    {
        if (_enemyObject == null || _playerHP == null)
        {
            _turnManager?.SignalEnemyActionComplete();
            yield break;
        }

        if (_multiEnemyManager != null)
        {
            foreach (EnemyUnit enemy in _multiEnemyManager.GetAliveEnemies())
            {
                if (enemy.controller == null || !enemy.controller.enabled) continue;

                enemy.controller.ExecuteTurn();
                LogBattleEvent($"{enemy.DisplayName} 执行 {enemy.controller.GetCurrentIntent()}（数值 {enemy.controller.GetIntentValue()}）。");
            }
        }
        else if (_enemyController != null)
        {
            _enemyController.ExecuteTurn();
            LogBattleEvent($"敌人执行 {_enemyController.GetCurrentIntent()}（数值 {_enemyController.GetIntentValue()}）。");
        }
        else if (_enemyAI != null)
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
        if (_multiEnemyManager != null)
        {
            DealDamageToTarget(_multiEnemyManager.DefaultTarget, damage);
            return;
        }

        if (_enemyHP == null || _isBattleOver) return;

        _enemyHP.TakeDamage(damage);
        LogBattleEvent($"敌人受到 {damage} 点伤害，HP {_enemyHP.CurrentHP}/{_enemyHP.MaxHP}。");

        OnBattleInfoChanged?.Invoke();

        if (_enemyHP.CurrentHP <= 0)
        {
            EndBattle("win");
        }
    }

    /// <summary>
    /// 多敌人模式下对指定敌人造成伤害；单敌人模式也允许直接结算该目标。
    /// </summary>
    public void DealDamageToTarget(EnemyUnit target, int damage)
    {
        if (target == null || damage <= 0 || _isBattleOver) return;

        if (_multiEnemyManager != null)
            _multiEnemyManager.DealDamageToTarget(target, damage);
        else
            target.DealDamage(damage);

        LogBattleEvent($"{target.DisplayName} 受到 {damage} 点伤害，HP {target.hp?.CurrentHP ?? 0}/{target.hp?.MaxHP ?? 0}。");
        OnBattleInfoChanged?.Invoke();

        if (_multiEnemyManager != null && _multiEnemyManager.AllDead)
            EndBattle("win");
        else if (_multiEnemyManager == null && target.hp != null && target.hp.CurrentHP <= 0)
            EndBattle("win");
    }

    public void PlayCard(CardData cardData)
    {
        PlayCard(cardData, ElementType.None);
    }

    public void PlayCard(CardData cardData, ElementType chosenElement)
    {
        if (cardData == null || _deckManager == null) return;
        CardInstance cardInstance = _deckManager.handCards.FirstOrDefault(card => card.cardId == cardData.cardId);
        PlayCard(cardInstance, chosenElement);
    }

    public void PlayCard(CardInstance cardInstance)
    {
        PlayCard(cardInstance, ElementType.None);
    }

    public void PlayCard(CardInstance cardInstance, ElementType chosenElement)
    {
        PlayCard(cardInstance, chosenElement, null);
    }

    public void PlayCard(CardInstance cardInstance, ElementType chosenElement, EnemyUnit target)
    {
        if (_isBattleOver) return;
        if (_turnManager == null || _turnManager.CurrentPhase != BattlePhase.PlayerAction) return;
        if (_deckManager == null || cardInstance == null || !_deckManager.handCards.Contains(cardInstance)) return;

        CardData cardData = cardInstance.GetCardData();
        if (cardData == null) return;

        if (RequiresTargetSelection(cardData) && (target == null || !target.IsAlive))
        {
            LogBattleEvent("请选择一个存活敌人作为攻击目标。");
            return;
        }

        if (!CanAffordCard(cardInstance))
        {
            LogBattleEvent($"资源不足：无法打出 {cardData.cardName}。");
            return;
        }

        _playerEnergy?.SpendEnergy(cardInstance.GetEnergyCost());
        SpendWaterCost(cardInstance);

        LogBattleEvent($"打出 {cardData.cardName}{(cardInstance.isUpgraded ? "+" : string.Empty)}。");
        _cardEffectResolver?.ResolveEffect(cardInstance, chosenElement, target);
        _deckManager.PlayCard(cardInstance);

        OnHandChanged?.Invoke();
        OnBattleInfoChanged?.Invoke();
    }

    public bool CanAffordCard(CardData cardData)
    {
        if (cardData == null) return false;
        return CanAffordCard(new CardInstance(cardData.cardId));
    }

    public bool CanAffordCard(CardInstance cardInstance)
    {
        if (cardInstance == null) return false;

        bool hasEnergy = _playerEnergy == null || _playerEnergy.HasEnoughEnergy(cardInstance.GetEnergyCost());
        bool hasWater = _playerState == null || _playerState.HasEnoughWater(cardInstance.GetWaterCost());
        return hasEnergy && hasWater;
    }

    private void SpendWaterCost(CardInstance cardInstance)
    {
        if (cardInstance == null || _playerState == null)
            return;

        int waterCost = cardInstance.GetWaterCost();
        if (waterCost <= 0)
            return;

        if (!_playerState.SpendWater(waterCost))
            return;

        LogBattleEvent($"消耗 {waterCost} 点水源，当前水源 {_playerState.WaterSource}。");

        if (_playerState.LakeEchoBlockPerWater > 0)
        {
            int block = _playerState.LakeEchoBlockPerWater * waterCost;
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

        // 多敌人战斗必须清空所有敌人才能获胜，防止旧的单敌人结算路径提前结束战斗。
        if (result == "win" && _multiEnemyManager != null && !_multiEnemyManager.AllDead)
        {
            LogBattleEvent($"仍有 {_multiEnemyManager.TotalAliveCount} 个敌人存活，战斗继续。");
            return;
        }

        _isBattleOver = true;

        if (_turnManager != null)
            _turnManager.StopBattle();

        Debug.Log($"[BattleManager] EndBattle({result}), isBattleWin即将设为{result == "win"}，GameManager.Instance={(GameManager.Instance != null ? "存在" : "为空")}");

        SyncGameManagerBattleResult(result);

        if (GameManager.Instance != null)
        {
            if (result == "win")
                GameManager.Instance.currentDraftMode = GameManager.DraftMode.BattleReward;

            bool isFinalVictory = result == "win"
                && GameManager.Instance.currentNodeId == GameManager.NodesPerFloor;
            if (result == "lose" || isFinalVictory)
                RunFlowCoordinator.EndRunFromBattle(result == "win");
        }

        OnBattleOver?.Invoke(result);
        LogBattleEvent(result == "win" ? "战斗胜利。" : "战斗失败。");

        if (result == "win"
            && _returnToMapOnWin
            && GameManager.Instance != null
            && GameManager.Instance.currentNodeId < GameManager.NodesPerFloor
            && !_returningToMap)
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
        GameManager.Instance.lastBattleResult = result;
        Debug.Log($"[BattleManager] SyncGameManagerBattleResult: 已设置 isBattleWin={GameManager.Instance.isBattleWin}");

        if (_playerHP != null)
            GameManager.Instance.playerHp = _playerHP.CurrentHP;
        if (_playerEnergy != null)
            GameManager.Instance.currentEnergy = _playerEnergy.CurrentEnergy;
        if (_playerBlock != null)
            GameManager.Instance.playerBlock = _playerBlock.CurrentBlock;

        // 同步最大血量与最大能量
        var maxHpComp = _playerObject?.GetComponent<playerMaxHP>();
        if (maxHpComp != null)
            GameManager.Instance.playerMaxHp = maxHpComp.maxHP;
        var maxEnergyComp = _playerObject?.GetComponent<maxEnergy>();
        if (maxEnergyComp != null)
            GameManager.Instance.maxEnergy = maxEnergyComp.energyMax;
    }

    private IEnumerator ReturnToMapAfterDelay()
    {
        _returningToMap = true;
        yield return new WaitForSeconds(_returnToMapDelay);

        // 新增：设置选牌场景的工作模式为战斗奖励
        if (GameManager.Instance != null)
        {
            GameManager.Instance.currentDraftMode = GameManager.DraftMode.BattleReward;
        }

        SceneManager.LoadScene(_rewardSceneName);

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

    /// <summary>
    /// 根据 GameManager 中的节点类型和关卡层数选择合适的敌人数据
    /// </summary>
    private void SelectEnemyData()
    {
        if (_enemyController == null)
        {
            Debug.LogWarning("[BattleManager] EnemyController 为空，无法选择敌人数据");
            return;
        }

        var gm = GameManager.Instance;

        // Boss 判断：有 GameManager 时读节点类型；无则随机
        bool isBoss = gm != null && gm.currentNodeType == "Boss";

        if (isBoss && _bossEnemyData != null)
        {
            ConfigureEnemyController(_bossEnemyData);
        }
        else if (_normalEnemyPool != null && _normalEnemyPool.Length > 0)
        {
            int index = UnityEngine.Random.Range(0, _normalEnemyPool.Length);
            ConfigureEnemyController(_normalEnemyPool[index]);
        }
        else if (_bossEnemyData != null)
        {
            // 兜底：没有普通敌人池时用 Boss
            ConfigureEnemyController(_bossEnemyData);
        }
    }

    private void ConfigureEnemyController(EnemyData data)
    {
        if (data == null || _enemyObject == null) return;

        EnemyController controller = CreateMonsterController(data.enemyName);
        if (controller == null) return;

        controller.SetEnemyData(data);
        _enemyController = controller;
        _enemyObject.GetComponent<EnemyIntentUI>()?.SetController(controller);

        if (data.enemyName == "我的刀盾")
            EnableDaoDunEncounter(data);
    }

    public bool RequiresTargetSelection(CardData cardData)
    {
        return cardData != null
            && cardData.HasCardType(CardType.Attack)
            && _multiEnemyManager != null
            && _multiEnemyManager.GetAliveEnemies().Count > 1;
    }

    private void EnableDaoDunEncounter(EnemyData data)
    {
        if (_isDaoDunEncounter || _enemyObject == null) return;

        _multiEnemyManager = GetComponent<MultiEnemyManager>();
        if (_multiEnemyManager == null)
            _multiEnemyManager = gameObject.AddComponent<MultiEnemyManager>();

        Vector3 center = _enemyObject.transform.position;
        float spacing = 3f;

        _enemyObject.transform.position = center + Vector3.left * spacing;
        _multiEnemyManager.RegisterEnemy(_enemyObject, data);

        for (int i = 1; i < 3; i++)
        {
            GameObject clone = Instantiate(_enemyObject,
                center + Vector3.right * spacing * (i - 1),
                _enemyObject.transform.rotation);
            clone.name = data.enemyName;

            EnemyUnit unit = EnemyUnit.FromGameObject(clone, data);
            unit?.controller?.SetEnemyData(data);
            _multiEnemyManager.RegisterEnemy(clone, data);
        }

        _isDaoDunEncounter = true;
        LogBattleEvent("遭遇我的刀盾 x3。每只独立随机攻击或格挡。");
    }

    private EnemyController CreateMonsterController(string enemyName)
    {
        EnemyController current = _enemyController;
        System.Type targetType = enemyName switch
        {
            "咕咕嘎嘎" => typeof(GuGuGaGa),
            "我的刀盾" => typeof(WoDeDaoDun),
            "你已急哭" => typeof(NiYiJiKu),
            "刘华强" => typeof(LiuHuaQiang),
            "爻一爻" => typeof(YaoYiYao),
            "带派雨姐" => typeof(DaiPaiYuJie),
            "川普" => typeof(ChuanPu),
            "可爱奶龙" => typeof(KeAiNaiLong),
            "大奶龙" => typeof(DaNaiLong),
            "奶蝠" => typeof(NaiFu),
            "疯狂星期四" => typeof(FengKuangXingQiSi),
            "疯狂戴夫" => typeof(FengKuangDaiFu),
            _ => typeof(EnemyController)
        };

        if (current != null && current.GetType() == targetType)
            return current;

        if (current != null)
            current.enabled = false;

        return (EnemyController)_enemyObject.AddComponent(targetType);
    }

    /// <summary>
    /// 等一帧后刷新 UI，确保 BattleUI 已完成初始化并订阅了事件
    /// </summary>
    private IEnumerator DelayedRefreshUI()
    {
        yield return null; // 等待一帧，让 BattleUI 完成初始化
        NotifyBattleInfoChanged();
    }

    private void EnsureGameManager()
    {
        if (GameManager.Instance != null) return;
        var go = new GameObject("GameManager");
        go.AddComponent<GameManager>();
        DontDestroyOnLoad(go);
        Debug.Log("[BattleManager] GameManager 不存在，已自动创建（直接测试 BattleScene 时的兜底）");
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
