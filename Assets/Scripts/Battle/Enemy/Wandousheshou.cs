using UnityEngine;

/// <summary>豌豆射手：瞄准一回合，下一回合射击；伤害最多成长至 25。</summary>
public class WanDouSheShou : EnemyController
{
    private int _attackCount;
    private bool _isAiming;

    protected override void DecideIntent()
    {
        if (!_isAiming)
        {
            currentIntent = EnemyIntent.Charge;
            intentValue = 0;
            intentDescription = "瞄准";
            return;
        }

        currentIntent = EnemyIntent.Attack;
        intentValue = 10 + 5 * Mathf.Min(_attackCount, 3);
        intentDescription = $"豌豆射击 {intentValue}";
    }

    protected override void ExecuteIntent()
    {
        if (currentIntent == EnemyIntent.Charge)
        {
            _isAiming = true;
            Debug.Log("豌豆射手瞄准中。");
            return;
        }

        if (currentIntent != EnemyIntent.Attack || _playerHP == null)
            return;

        _playerHP.TakeDamage(intentValue);
        Debug.Log($"豌豆射手射击！造成 {intentValue} 点伤害（第 {_attackCount + 1} 次攻击）。");
        _attackCount = Mathf.Min(3, _attackCount + 1);
        _isAiming = false;
    }
}
