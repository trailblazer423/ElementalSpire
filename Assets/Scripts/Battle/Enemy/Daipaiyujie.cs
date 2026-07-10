using UnityEngine;

/// <summary>
/// 带派雨姐 - 每回合给玩家叠毒
/// </summary>
public class DaiPaiYuJie : EnemyController
{
    protected override void DecideIntent()
    {
        currentIntent = EnemyIntent.Debuff;
        intentValue = 6;   // 每回合 +6 层中毒
    }

    protected override void ExecuteIntent()
    {
        if (_playerState != null)
        {
            _playerState.AddPoison(intentValue);
            Debug.Log($"{enemyData.enemyName} 的带派大脚！玩家中毒层数 +{intentValue}，当前 {_playerState.PoisonStacks}");
        }
        else
        {
            Debug.LogWarning($"{enemyData.enemyName} 试图上毒，但 PlayerState 为空！");
        }
    }
}