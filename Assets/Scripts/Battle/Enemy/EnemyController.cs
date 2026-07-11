using UnityEngine;

public enum EnemyIntent
{
    Attack,
    Defend,
    Charge,
    Heal,
    Buff,
    Debuff,
    None
}

public class EnemyController : MonoBehaviour
{
    [Header("敌人数据")]
    public EnemyData enemyData;
    protected PlayerState _playerState;
    protected currentEnergy _playerEnergy;

    [Header("意图")]
    public EnemyIntent currentIntent = EnemyIntent.None;
    public int intentValue = 0;
    [Header("意图描述(如：力量+2 / 易伤+3)")]
    public string intentDescription = "";

    protected enemyHP _enemyHP;
    protected enemyBlock _enemyBlock;
    protected playerHP _playerHP;
    protected EnemyState _enemyState;
    private bool _dataInitialized;

    public bool IsBoss => enemyData != null && enemyData.enemyType == EnemyType.Boss;

    /// <summary>
    /// 由 BattleManager 调用，动态注入敌人数据并完成初始化（HP/意图）。
    /// 解决 Awake/Start 执行顺序不确定导致 enemyData 为 null 的问题。
    /// </summary>
    public void SetEnemyData(EnemyData data)
    {
        if (data == null) return;

        enemyData = data;
        CacheReferences();
        enemyMaxHP maxHPComp = GetComponent<enemyMaxHP>();
        if (maxHPComp != null)
            maxHPComp.maxHP = data.maxHP;
        if (_enemyHP != null)
            _enemyHP.CurrentHP = data.maxHP;

        DecideNextIntent();
        _dataInitialized = true;
        Debug.Log($"[EnemyController] 敌人数据已注入：{data.enemyName} (HP:{data.maxHP})");
    }

    protected virtual void Start()
    {
        CacheReferences();

        if (enemyData != null && !_dataInitialized)
        {
            enemyMaxHP maxHPComp = GetComponent<enemyMaxHP>();
            if (maxHPComp != null)
                maxHPComp.maxHP = enemyData.maxHP;
            if (_enemyHP != null)
                _enemyHP.CurrentHP = enemyData.maxHP;
            DecideNextIntent();
            _dataInitialized = true;
        }
    }

    protected void CacheReferences()
    {
        _enemyHP = GetComponent<enemyHP>();
        _enemyBlock = GetComponent<enemyBlock>();
        _enemyState = GetComponent<EnemyState>();
        _playerHP = GameObject.Find("Player")?.GetComponent<playerHP>();
        _playerEnergy = GameObject.Find("Player")?.GetComponent<currentEnergy>();
        _playerState = GameObject.Find("Player")?.GetComponent<PlayerState>();
    }

    /// <summary>
    /// 执行当前已决定好的意图（Attack/Defend 等）。
    /// 意图由 DecideNextIntent() 预先决定，供玩家查看。
    /// </summary>
    public void ExecuteTurn()
    {
        if (_playerHP == null || enemyData == null) return;

        Debug.Log($"{enemyData.enemyName} 执行: {currentIntent}，数值: {intentValue}");
        ExecuteIntent();
    }

    /// <summary>
    /// 决定下一次行动的意图，但不执行。
    /// 在敌方回合结束后调用，让玩家在己方回合能看到敌人即将做什么。
    /// </summary>
    public void DecideNextIntent()
    {
        if (enemyData == null) return;
        DecideIntent();
    }

    protected virtual void DecideIntent()
    {
        if (enemyData == null) return;

        // 每次决定新意图时清空描述，由具体子类设置
        intentDescription = "";

        float random = Random.value;

        if (random < enemyData.attackChance)
        {
            currentIntent = EnemyIntent.Attack;
            intentValue = enemyData.baseAttack;
        }
        else
        {
            currentIntent = EnemyIntent.Defend;
            intentValue = enemyData.baseDefend;
        }
    }

    protected virtual void ExecuteIntent()
    {
        switch (currentIntent)
        {
            case EnemyIntent.Attack:
                Attack();
                break;
            case EnemyIntent.Defend:
                Defend();
                break;
            case EnemyIntent.Charge:
                Charge();
                break;
            case EnemyIntent.Heal:
                Heal();
                break;
            case EnemyIntent.Buff:
                Buff();
                break;
            case EnemyIntent.Debuff:
                Debuff();
                break;
            default:
                break;
        }
    }

    public virtual void Attack()
    {
        int damage = intentValue > 0 ? intentValue : enemyData.baseAttack;

        // 敌人力量加成
        if (_enemyState != null && _enemyState.Power > 0)
            damage += _enemyState.Power;

        // 敌人虚弱减伤
        if (_enemyState != null && _enemyState.Weakness > 0)
            damage = Mathf.Max(0, damage - _enemyState.Weakness);

        // 集成深度中毒系统：中毒时伤害降低
        if (_enemyState != null && _enemyState.TryConsumeDeepPoison())
        {
            damage = Mathf.Max(1, Mathf.FloorToInt(damage * 0.75f));
            Debug.Log($"【敌人行动】深度中毒生效，本次攻击降低为 {damage}");
        }

        if (_playerHP != null)
            _playerHP.TakeDamage(damage);
        Debug.Log($"{enemyData.enemyName} 攻击！造成 {damage} 点伤害");
    }

    public virtual void Defend()
    {
        int block = intentValue > 0 ? intentValue : enemyData.baseDefend;
        _enemyBlock.AddBlock(block);
        Debug.Log($"{enemyData.enemyName} 防御！获得 {block} 点护盾");
    }

    public virtual void Charge()
    {
        Debug.Log($"{enemyData.enemyName} 蓄力！");
    }

    public virtual void Heal()
    {
        if (_enemyHP == null) return;
        int healAmount = 5;
        _enemyHP.CurrentHP = Mathf.Min(_enemyHP.CurrentHP + healAmount, _enemyHP.MaxHP);
        Debug.Log($"{enemyData.enemyName} 回复 {healAmount} 点生命");
    }

    public virtual void Buff()
    {
        // 默认：给自己加力量
        if (_enemyState != null)
        {
            int powerAmount = intentValue > 0 ? intentValue : 2;
            _enemyState.AddPower(powerAmount);
            Debug.Log($"{enemyData.enemyName} 获得强化！力量 +{powerAmount}");
        }
        else
        {
            Debug.Log($"{enemyData.enemyName} 获得强化！");
        }
    }

    /// <summary>
    /// 虚方法 - 默认对玩家施加减益效果
    /// 子类可重写以实现具体效果（虚弱/易伤/中毒等）
    /// </summary>
    public virtual void Debuff()
    {
        Debug.Log($"{enemyData.enemyName} 对玩家施加减益！");
    }

    public EnemyIntent GetCurrentIntent()
    {
        return currentIntent;
    }

    public int GetIntentValue()
    {
        return intentValue;
    }
}
