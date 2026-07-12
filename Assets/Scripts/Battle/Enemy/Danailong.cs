using UnityEngine;

/// <summary>
/// 大奶龙 - 每回合给玩家上一层虚弱
/// </summary>
public class DaNaiLong : EnemyController
{
    protected override void DecideIntent()
    {
        currentIntent = EnemyIntent.Debuff;
        intentValue = 1;
        intentDescription = $"虚弱+{intentValue}";
    }

    protected override void ExecuteIntent()
    {
        if (_playerState != null)
        {
            _playerState.AddWeakness(intentValue);
            Debug.Log($"{enemyData.enemyName} 唐笑！给玩家施加 {intentValue} 层虚弱，当前虚弱 {_playerState.Weakness}/{PlayerState.MaxWeaknessStacks}");
        }
        else
        {
            Debug.LogWarning($"{enemyData.enemyName} 试图上虚弱，但 PlayerState 为空！");
        }
    }
}
