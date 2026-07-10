using UnityEngine;

public class playerHP : MonoBehaviour
{
    private int _currentHP;
    public int Weakness = 0;
    private playerMaxHP _maxHPComponent;
    private playerBlock _blockComponent;

    void Awake()
    {
        _maxHPComponent = GetComponent<playerMaxHP>();
        _blockComponent = GetComponent<playerBlock>();

        if (_maxHPComponent != null)
        {
            _currentHP = _maxHPComponent.maxHP;
        }
    }

    /// <summary>
    /// 受到伤害，先扣除护盾，再扣除生命值
    /// </summary>
    public void TakeDamage(int damage)
    {
        if (damage <= 0) return;

        // 护盾吸收伤害
        int remaining = _blockComponent != null
            ? _blockComponent.AbsorbDamage(damage)
            : damage;

        // 剩余伤害扣血
        if (remaining > 0)
        {
            _currentHP = Mathf.Max(0, _currentHP - remaining);
        }
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
    public int GetWeakness()
    {
        return Weakness;
    }
}
