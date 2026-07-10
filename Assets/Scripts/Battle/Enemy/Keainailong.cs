using UnityEngine;

/// <summary>
/// 可爱奶龙 - 每回合给敌方全体上 5 点护盾
/// </summary>
public class KeAiNaiLong : EnemyController
{
    protected override void DecideIntent()
    {
        currentIntent = EnemyIntent.Defend;
        intentValue = 5;
    }

    protected override void ExecuteIntent()
    {
        // 给敌方全体上护盾
        // 当前版本：只给"自己阵营"上护盾
        // 后续多敌人时改为给所有敌人上护盾
        _enemyBlock.AddBlock(intentValue);
        Debug.Log($"{enemyData.enemyName} 大肚肚！给全体上 {intentValue} 点护盾");
    }
}