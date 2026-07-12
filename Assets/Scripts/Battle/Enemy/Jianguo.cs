/// <summary>
/// 坚果：仅提供“坚硬外壳”被动；伤害减免由疯狂戴夫统一处理。
/// </summary>
public class JianGuo : EnemyController
{
    protected override void DecideIntent()
    {
        currentIntent = EnemyIntent.None;
        intentValue = 0;
        intentDescription = "坚硬外壳";
    }

    protected override void ExecuteIntent()
    {
        // 坚果不再每回合给 Boss 护盾。
    }
}
