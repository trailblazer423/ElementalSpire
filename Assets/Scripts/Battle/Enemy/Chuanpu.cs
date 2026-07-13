using UnityEngine;

/// <summary>
/// 蔡徐坤：每回合随机使用攻击、防御或削减下回合能量。
/// </summary>
public class ChuanPu : EnemyController
{
    protected override void DecideIntent()
    {
        switch (Random.Range(0, 3))
        {
            case 0:
                currentIntent = EnemyIntent.Defend;
                intentValue = 10;
                intentDescription = "你干嘛~：获得护盾";
                break;
            case 1:
                currentIntent = EnemyIntent.Debuff;
                intentValue = 1;
                intentDescription = $"鸡你太美：下回合能量-{intentValue}";
                break;
            default:
                currentIntent = EnemyIntent.Attack;
                intentValue = 8;
                intentDescription = "铁山靠：造成伤害";
                break;
        }
    }

    protected override void ExecuteIntent()
    {
        if (currentIntent == EnemyIntent.Debuff)
        {
            if (_playerState != null)
            {
                _playerState.AddNextTurnEnergyPenalty(intentValue);
                Debug.Log($"{enemyData.enemyName} 加关税！玩家下回合可用能量减少 {intentValue} 点。");
            }
            return;
        }

        base.ExecuteIntent();
    }
}
