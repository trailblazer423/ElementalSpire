using UnityEngine;

public class playerHP : MonoBehaviour
{
    private int _currentHP;

    private playerMaxHP _maxHPComponent;
    private playerBlock _blockComponent;
    private PlayerState _playerState;

    void Awake()
    {
        _maxHPComponent = GetComponent<playerMaxHP>();
        _blockComponent = GetComponent<playerBlock>();
        _playerState = GetComponent<PlayerState>();

        if (_maxHPComponent != null)
        {
            _currentHP = _maxHPComponent.maxHP;
        }
    }

    /// <summary>
    /// 受到伤害，先扣除护盾，再扣除生命值
    /// </summary>
    /// <param name="damage">原始伤害值</param>
    /// <param name="applyVulnerable">是否受易伤加成（敌方攻击=true，中毒/自伤=false）</param>
    public void TakeDamage(int damage, bool applyVulnerable = true)
    {
        if (damage <= 0) return;

        int hpBefore = _currentHP;

        // 只有敌方攻击才受易伤加成，中毒/自伤不受影响
        if (applyVulnerable)
            damage += _playerState != null ? _playerState.Vulnerable : 0;

        // 护盾吸收伤害
        int remaining = _blockComponent != null
            ? _blockComponent.AbsorbDamage(damage)
            : damage;

        // 剩余伤害扣血
        if (remaining > 0)
        {
            _currentHP = Mathf.Max(0, _currentHP - remaining);
        }

        int actualDamage = hpBefore - _currentHP;
        if (actualDamage > 0)
            BattleStatistics.EnsureExists().RecordDamageTaken(actualDamage);
    }

    public int CurrentHP
    {
        get { return _currentHP; }
        set
        {
            int max = _maxHPComponent != null ? _maxHPComponent.maxHP : 20;
            _currentHP = Mathf.Clamp(value, 0, max);
        }
    }

    public int MaxHP
    {
        get { return _maxHPComponent != null ? _maxHPComponent.maxHP : 20; }
    }
}
