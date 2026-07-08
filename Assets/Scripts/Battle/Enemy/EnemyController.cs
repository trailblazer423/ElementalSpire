using UnityEngine;

public enum EnemyIntent
{
    Attack,
    Defend,
    Charge,
    Heal,
    Buff,
    None
}

public class EnemyController : MonoBehaviour
{
    [Header("敌人数据")]
    public EnemyData enemyData;

    [Header("意图")]
    public EnemyIntent currentIntent = EnemyIntent.None;
    public int intentValue = 0;

    protected enemyHP _enemyHP;
    protected enemyBlock _enemyBlock;
    protected playerHP _playerHP;
    protected EnemyState _enemyState;

    public bool IsBoss => enemyData != null && enemyData.enemyType == EnemyType.Boss;

    void Start()
    {
        _enemyHP = GetComponent<enemyHP>();
        _enemyBlock = GetComponent<enemyBlock>();
        _enemyState = GetComponent<EnemyState>();
        _playerHP = GameObject.Find("Player")?.GetComponent<playerHP>();

        if (enemyData != null)
        {
            enemyMaxHP maxHPComp = GetComponent<enemyMaxHP>();
            if (maxHPComp != null)
            {
                maxHPComp.maxHP = enemyData.maxHP;
            }

            if (_enemyHP != null)
                _enemyHP.CurrentHP = enemyData.maxHP;

            // 预决定第一个意图，让玩家在首回合就能看到
            DecideNextIntent();
        }
        else
        {
            Debug.LogWarning($"[EnemyController] {gameObject.name} 的 enemyData 未赋值！请在 Inspector 中指定 EnemyData 资产。");
        }
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
            default:
                break;
        }
    }

    public virtual void Attack()
    {
        int damage = intentValue > 0 ? intentValue : enemyData.baseAttack;

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
        Debug.Log($"{enemyData.enemyName} 获得强化！");
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