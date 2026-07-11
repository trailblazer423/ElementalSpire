using UnityEngine;

public class WanDouSheShou : EnemyController
{
    private int _attackCount = 0;

   

    protected override void DecideIntent()
    {
        currentIntent = EnemyIntent.Attack;
        // N 从 0 开始，复活后的新单位会重新从 0 计数。
        intentValue = 10 + 10 * _attackCount;
        intentDescription = $"递增{intentValue}";
    }

    protected override void ExecuteIntent()
    {
        _playerHP.TakeDamage(intentValue);
        Debug.Log($"豌豆射手 射击！造成 {intentValue} 点伤害（攻击{_attackCount}次）");
        _attackCount++;
    }
}
