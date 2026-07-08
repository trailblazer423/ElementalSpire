using UnityEngine;

public class BossController : EnemyController
{
    private int _chargeCount = 0;
    private int _maxCharge = 2;

    protected override void DecideIntent()
    {
        float random = Random.value;

        if (_chargeCount >= _maxCharge)
        {
            // 蓄力满了，强制重击
            currentIntent = EnemyIntent.Attack;
            intentValue = enemyData.baseAttack + _chargeCount * 3;
            _chargeCount = 0;
        }
        else if (random < 0.3f)
        {
            // 蓄力
            currentIntent = EnemyIntent.Charge;
            intentValue = _chargeCount + 1;
        }
        else if (random < 0.6f && _chargeCount > 0)
        {
            // 重击
            currentIntent = EnemyIntent.Attack;
            intentValue = enemyData.baseAttack + _chargeCount * 3;
            _chargeCount = 0;
        }
        else if (random < 0.8f)
        {
            // 防御（Boss 也会防守）
            currentIntent = EnemyIntent.Defend;
            intentValue = enemyData.baseDefend + 2;
        }
        else
        {
            // 普通攻击
            currentIntent = EnemyIntent.Attack;
            intentValue = enemyData.baseAttack;
        }
    }

    protected override void ExecuteIntent()
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

    public override void Attack()
    {
        int damage = intentValue > 0 ? intentValue : enemyData.baseAttack;
        _playerHP.TakeDamage(damage);
        Debug.Log($"Boss 攻击！造成 {damage} 点伤害（蓄力层数 {_chargeCount}）");
        _chargeCount = 0;
    }

    public override void Defend()
    {
        int block = intentValue > 0 ? intentValue : enemyData.baseDefend + 2;
        _enemyBlock.AddBlock(block);
        Debug.Log($"Boss 防御！获得 {block} 点护盾");
    }

    public override void Charge()
    {
        _chargeCount = Mathf.Min(_chargeCount + 1, _maxCharge);
        Debug.Log($"Boss 蓄力！当前 {_chargeCount}/{_maxCharge} 层");
    }

    public override void Heal()
    {
        int healAmount = 8;
        _enemyHP.CurrentHP = Mathf.Min(_enemyHP.CurrentHP + healAmount, _enemyHP.MaxHP);
        Debug.Log($"Boss 回复 {healAmount} 点生命！");
    }

    public override void Buff()
    {
        Debug.Log("Boss 获得力量加成！");
    }
}