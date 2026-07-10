using UnityEngine;

public class GuGuGaGa : EnemyController
{
    private int _turnCount = 0;
    private int _strength = 0;

    protected override void DecideIntent()
    {
        _turnCount++;
        int phase = (_turnCount - 1) % 3;

        switch (phase)
        {
            case 0:
                currentIntent = EnemyIntent.Buff;
                intentValue = 2;  // 固定加 2 点力量
                break;
            case 1:
            case 2:
                currentIntent = EnemyIntent.Attack;
                intentValue = 6 + _strength;  // ✅ 当前力量已经加到伤害里
                break;
        }
    }

    protected override void ExecuteIntent()
    {
        switch (currentIntent)
        {
            case EnemyIntent.Buff:
                // ✅ 这里把力量和数值分开更新
                _strength += intentValue;
                Debug.Log($"{enemyData.enemyName} 蓄力！力量 +{intentValue}，当前力量 {_strength}");
                break;

            case EnemyIntent.Attack:
                // ✅ 这里直接打出去
                _playerHP.TakeDamage(intentValue);
                Debug.Log($"{enemyData.enemyName} 攻击！造成 {intentValue} 点伤害");
                break;

            default:
                base.ExecuteIntent();
                break;
        }
    }
}