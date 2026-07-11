using ElementalSpire.TextBattlePrototype;
using UnityEngine;

/// <summary>
/// 你已急哭 - 固定顺序：虚弱 → 攻击
/// </summary>
public class NiYiJiKu : EnemyController
{
    private int _turnCount = 0;

    protected override void DecideIntent()
    {
        _turnCount++;
        int phase = (_turnCount - 1) % 2;

        switch (phase)
        {
            case 0:
                currentIntent = EnemyIntent.Debuff;
                intentValue = 2;
                intentDescription = $"虚弱+{intentValue}";
                break;
            case 1:
                currentIntent = EnemyIntent.Attack;
                intentValue = 8;
                intentDescription = "";
                break;
        }
    }

    protected override void ExecuteIntent()
    {
        switch (currentIntent)
        {
            case EnemyIntent.Debuff:
                ApplyWeakness();
                break;
            case EnemyIntent.Attack:
                Attack();
                break;
            default:
                base.ExecuteIntent();
                break;
        }
    }

    private void ApplyWeakness()
    {
        if (_playerState != null)
        {
            _playerState.AddWeakness(intentValue);
            Debug.Log($"{enemyData.enemyName} 大喊：你已急哭！给玩家施加 {intentValue} 层虚弱，当前虚弱 {_playerState.Weakness}/5");
        }
        else
        {
            Debug.LogWarning($"{enemyData.enemyName} 试图施加虚弱，但 PlayerState 为空！");
        }
    }

    public override void Attack()
    {
        int damage = intentValue;
        _playerHP.TakeDamage(damage);
        Debug.Log($"{enemyData.enemyName} 攻击！造成 {damage} 点伤害");
    }
}