using UnityEngine;

public class GuGuGaGa : EnemyController
{
    private int _turnCount = 0;

    protected override void DecideIntent()
    {
        _turnCount++;
        int phase = (_turnCount - 1) % 3;

        switch (phase)
        {
            case 0:
                currentIntent = EnemyIntent.Buff;
                intentValue = 2;  // 固定加 2 点力量
                intentDescription = $"力量+{intentValue}";
                break;
            case 1:
            case 2:
                int enemyPower = _enemyState != null ? _enemyState.Power : 0;
                currentIntent = EnemyIntent.Attack;
                intentValue = 6 + enemyPower;
                intentDescription = "";
                break;
        }
    }

    protected override void ExecuteIntent()
    {
        switch (currentIntent)
        {
            case EnemyIntent.Buff:
                _enemyState?.AddPower(intentValue);
                int currentPower = _enemyState?.Power ?? 0;
                Debug.Log($"{enemyData.enemyName} 蓄力！力量 +{intentValue}，当前力量 {currentPower}");
                break;

            case EnemyIntent.Attack:
                _playerHP.TakeDamage(intentValue);
                Debug.Log($"{enemyData.enemyName} 攻击！造成 {intentValue} 点伤害");
                break;

            default:
                base.ExecuteIntent();
                break;
        }
    }
}