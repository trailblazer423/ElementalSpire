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
        intentDescription = $"易伤+{intentValue}";
    }

    protected override void ExecuteIntent()
    {
        if (_playerState != null)
        {
            _playerState.AddVulnerable(intentValue);
            Debug.Log($"向日葵照射！给玩家上 {intentValue} 层易伤，当前易伤 {_playerState.Vulnerable}/{PlayerState.MaxVulnerableStacks}。");
        }
    }
}
