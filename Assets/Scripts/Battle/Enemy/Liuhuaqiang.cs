using UnityEngine;

/// <summary>
/// 刘华强 - 根据玩家血量改变伤害
/// </summary>
public class LiuHuaQiang : EnemyController
{
    protected override void DecideIntent()
    {
        currentIntent = EnemyIntent.Attack;

        float hpPercent = (float)_playerHP.CurrentHP / _playerHP.MaxHP;

        if (hpPercent >= 0.5f)
        {
            intentValue = 4;   // 减半
        }
        else
        {
            intentValue = 16;  // 加倍
        }
    }
}