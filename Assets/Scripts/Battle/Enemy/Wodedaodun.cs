using UnityEngine;

/// <summary>
/// 我的刀盾 - 随机攻击或防御
/// </summary>
public class WoDeDaoDun : EnemyController
{
    protected override void DecideIntent()
    {
        // 随机 0.5 / 0.5
        if (Random.value < 0.5f)
        {
            currentIntent = EnemyIntent.Attack;
            intentValue = 6;
        }
        else
        {
            currentIntent = EnemyIntent.Defend;
            intentValue = 5;
        }
    }
}