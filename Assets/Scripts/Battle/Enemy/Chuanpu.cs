using UnityEngine;

/// <summary>
/// 川普：每回合等概率建墙、加关税或发动推特攻击。
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
                intentDescription = "";
                break;
            case 1:
                currentIntent = EnemyIntent.Debuff;
                intentValue = 1;
                intentDescription = $"能量-{intentValue}";
                break;
            default:
                currentIntent = EnemyIntent.Attack;
                intentValue = 8;
                intentDescription = "";
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
