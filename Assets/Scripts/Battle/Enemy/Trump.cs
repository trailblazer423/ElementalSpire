using UnityEngine;

/// <summary>
/// 川普 - 三种行为各 1/3 概率
/// </summary>
public class Trump : EnemyController
{
    private bool _tariffApplied = false;

    protected override void DecideIntent()
    {
        int choice = Random.Range(0, 3);

        switch (choice)
        {
            case 0:
                currentIntent = EnemyIntent.Defend;
                intentValue = 10;   // 建墙：+10护盾
                break;
            case 1:
                currentIntent = EnemyIntent.Debuff;
                intentValue = 1;    // 关税：减1能量
                break;
            case 2:
                currentIntent = EnemyIntent.Attack;
                intentValue = 8;    // 推特：8点伤害
                break;
        }
    }

    protected override void ExecuteIntent()
    {
        switch (currentIntent)
        {
            case EnemyIntent.Defend:
                _enemyBlock.AddBlock(intentValue);
                Debug.Log($"{enemyData.enemyName} 建墙！获得 {intentValue} 点护盾");
                break;

            case EnemyIntent.Debuff:
                _tariffApplied = true;
                Debug.Log($"{enemyData.enemyName} 加关税！你下回合能量 -1");
                break;

            case EnemyIntent.Attack:
                _playerHP.TakeDamage(intentValue);
                Debug.Log($"{enemyData.enemyName} 推特攻击！造成 {intentValue} 点伤害");
                break;

            default:
                base.ExecuteIntent();
                break;
        }
    }

    public void ApplyTariffIfNeeded()
    {
        if (_tariffApplied && _playerEnergy != null)
        {
            _playerEnergy.CurrentEnergy = Mathf.Max(0, _playerEnergy.CurrentEnergy - 1);
            Debug.Log($"关税生效！当前能量减少1点，剩余 {_playerEnergy.CurrentEnergy}");
            _tariffApplied = false;
        }
    }
}