using UnityEngine;

/// <summary>
/// 坚果 - 每回合给 Boss 上护盾
/// </summary>
public class JianGuo : EnemyController
{
    protected override void DecideIntent()
    {
        currentIntent = EnemyIntent.Defend;
        intentValue = 10;
    }

    protected override void ExecuteIntent()
    {
        // 给 Boss 上护盾（需要找到 Boss 的 enemyBlock）
        FengKuangDaiFu boss = FindObjectOfType<FengKuangDaiFu>();
        if (boss != null)
        {
            enemyBlock bossBlock = boss.GetComponent<enemyBlock>();
            if (bossBlock != null)
            {
                bossBlock.AddBlock(intentValue);
                Debug.Log($"坚果 给 Boss 上 {intentValue} 点护盾");
            }
        }
    }
}