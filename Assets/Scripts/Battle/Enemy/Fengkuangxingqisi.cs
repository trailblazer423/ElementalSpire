using UnityEngine;

/// <summary>
/// 疯狂星期四 - 前3回合虚弱，第4回合爆发
/// </summary>
public class FengKuangXingQiSi : EnemyController
{
    private int _turnCount = 0;
    private int _burstCount = 0;   // 第几次疯狂星期四

    protected override void DecideIntent()
    {
        _turnCount++;
        int phase = (_turnCount - 1) % 4;   // 0,1,2,3 循环

        if (phase < 3)
        {
            // 回合1-3：虚弱
            currentIntent = EnemyIntent.Debuff;
            intentValue = 1;
            intentDescription = $"虚弱+{intentValue}";
        }
        else
        {
            // 回合4：疯狂星期四
            _burstCount++;
            int damage = 25 + 25 * (_burstCount - 1);
            currentIntent = EnemyIntent.Attack;
            intentValue = damage;
            intentDescription = "爆发！";
        }
    }

    protected override void ExecuteIntent()
    {
        switch (currentIntent)
        {
            case EnemyIntent.Debuff:
                if (_playerState != null)
                {
                    _playerState.AddWeakness(intentValue);
                    Debug.Log($"{enemyData.enemyName} 给你施加 {intentValue} 层虚弱！当前虚弱 {_playerState.Weakness}/{PlayerState.MaxWeaknessStacks}");
                }
                break;

            case EnemyIntent.Attack:
                _playerHP.TakeDamage(intentValue);
                Debug.Log($"🔥 疯狂星期四！造成 {intentValue} 点伤害！（第{_burstCount}次爆发）");
                break;

            default:
                base.ExecuteIntent();
                break;
        }
    }
}
