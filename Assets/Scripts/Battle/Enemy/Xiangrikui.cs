using UnityEngine;

/// <summary>
/// 向日葵 - 每回合给玩家上易伤
/// </summary>
public class XiangRiKui : EnemyController
{
    protected override void DecideIntent()
    {
        currentIntent = EnemyIntent.Debuff;
        intentValue = 2;
    }

    protected override void ExecuteIntent()
    {
        if (_playerState != null)
        {
            // 易伤是独立状态（待实现）
            Debug.Log($"向日葵 照射！给玩家上 {intentValue} 层易伤");
        }
    }
}
