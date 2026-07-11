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
        MultiEnemyManager multiEnemyManager = FindObjectOfType<MultiEnemyManager>();
        if (multiEnemyManager != null)
            multiEnemyManager.AddBlockToAll(intentValue);
        else
            _enemyBlock?.AddBlock(intentValue);
        Debug.Log($"{enemyData.enemyName} 大肚肚！给全体上 {intentValue} 点护盾");
    }
}
