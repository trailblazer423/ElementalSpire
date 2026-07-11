using UnityEngine;

/// <summary>
/// 奶蝠 - 每回合造成 5 + 5×N 点伤害（N = 回合数）
/// </summary>
public class NaiFu : EnemyController
{
    private int _turnCount = 0;

    protected override void DecideIntent()
    {
        _turnCount++;
        int damage = 5 + 5 * _turnCount;
        currentIntent = EnemyIntent.Attack;
        intentValue = damage;
        intentDescription = $"递增{damage}";
    }

    protected override void ExecuteIntent()
    {
        _playerHP.TakeDamage(intentValue);
        Debug.Log($"{enemyData.enemyName} 开杀！造成 {intentValue} 点伤害（第{_turnCount}回合）");
    }
}
