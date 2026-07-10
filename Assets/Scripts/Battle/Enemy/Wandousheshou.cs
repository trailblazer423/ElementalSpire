using UnityEngine;

public class WanDouSheShou : EnemyController
{
    private int _attackCount = 0;
    private int _reviveCount = 0;   // 复活次数

   

    protected override void DecideIntent()
    {
        currentIntent = EnemyIntent.Attack;
        // 伤害 = 10 + 10 × 攻击次数 + 10 × 复活次数
        intentValue = 10 + 10 * _attackCount + 10 * _reviveCount;
    }

    protected override void ExecuteIntent()
    {
        _playerHP.TakeDamage(intentValue);
        Debug.Log($"豌豆射手 射击！造成 {intentValue} 点伤害（攻击{_attackCount}次，复活{_reviveCount}次）");
        _attackCount++;
    }
    public void SetReviveCount(int count)
    {
        _reviveCount = count;
    }
}