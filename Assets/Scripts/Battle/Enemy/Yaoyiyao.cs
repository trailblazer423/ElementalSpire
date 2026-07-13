using UnityEngine;

/// <summary>
/// 爻一爻 - 每回合卜卦，阳卦伤害+力量，阴卦伤害+格挡，连续3次同卦触发大招
/// </summary>
public class YaoYiYao : EnemyController
{
    private int _consecutiveCount = 0;
    private bool _lastWasYang = false;
    private bool _isBurstTurn = false;   // 标记当前回合是否是大招回合

    /// <summary>供意图 UI 读取本次卦象与连卦进度。</summary>
    public bool IsYangIntent => _lastWasYang;
    public int ConsecutiveCount => _consecutiveCount;
    public bool IsBurstTurn => _isBurstTurn;

    protected override void DecideIntent()
    {
        int enemyPower = _enemyState != null ? _enemyState.Power : 0;

        // 默认：卜卦
        bool isYang = Random.value < 0.5f;

        // 更新连续计数
        if (_consecutiveCount > 0 && isYang == _lastWasYang)
            _consecutiveCount++;
        else
            _consecutiveCount = 1;

        _lastWasYang = isYang;

        // 连续3次同卦 → 大招回合
        if (_consecutiveCount >= 3)
        {
            _isBurstTurn = true;
            currentIntent = EnemyIntent.Attack;
            intentValue = 25 + enemyPower;   // 大招伤害
            string trigram = isYang ? "阳" : "阴";
            intentDescription = $"三连{trigram}卦！\n⚔ 大招 {intentValue}";
            _consecutiveCount = 0;           // 大招释放后重置计数
            return;
        }

        // 普通回合
        _isBurstTurn = false;

        if (isYang)
        {
            // 阳卦：造成 6 + 力量 伤害，然后力量 +3
            currentIntent = EnemyIntent.Attack;
            intentValue = 6 + enemyPower;
            string warning = _consecutiveCount == 2 ? " → 同卦大招" : "";
            intentDescription = $"阳卦  ⚔ 攻击 {intentValue}\n✦ 力量 +3 · 连阳 {_consecutiveCount}/3{warning}";
        }
        else
        {
            // 阴卦：造成 4 + 力量 伤害，获得 10 格挡
            currentIntent = EnemyIntent.Attack;
            intentValue = 4 + enemyPower;
            string warning = _consecutiveCount == 2 ? " → 同卦大招" : "";
            intentDescription = $"阴卦  ⚔ 攻击 {intentValue}\n🛡 护盾 +10 · 连阴 {_consecutiveCount}/3{warning}";
        }
    }

    protected override void ExecuteIntent()
    {
        // 先执行伤害
        _playerHP.TakeDamage(intentValue);

        // 再执行附加效果
        if (_isBurstTurn)
        {
            Debug.Log($"{enemyData.enemyName} 连续三卦相同！大招！造成 {intentValue} 点伤害");
        }
        else
        {
            // 根据上一次卦象决定附加效果（因为 DecideIntent 里已经存了 _lastWasYang）
            if (_lastWasYang)
            {
                _enemyState?.AddPower(3);
                int currentPower = _enemyState?.Power ?? 0;
                Debug.Log($"{enemyData.enemyName} 卜得阳卦！造成 {intentValue} 点伤害，力量 +3，当前力量 {currentPower}");
            }
            else
            {
                _enemyBlock.AddBlock(10);
                Debug.Log($"{enemyData.enemyName} 卜得阴卦！造成 {intentValue} 点伤害，获得 10 点护盾");
            }
        }
    }
}
