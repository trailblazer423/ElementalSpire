using UnityEngine;

public class enemyHP : MonoBehaviour
{
    private int _currentHP;

    private enemyMaxHP _maxHPComponent;
    private enemyBlock _blockComponent;

    void Awake()
    {
        _maxHPComponent = GetComponent<enemyMaxHP>();
        _blockComponent = GetComponent<enemyBlock>();

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

        FengKuangDaiFu daiFu = FindObjectOfType<FengKuangDaiFu>();
        if (daiFu != null && daiFu.ShouldReducePlantDamage(gameObject))
            damage = Mathf.CeilToInt(damage * 0.5f);

        int remaining = _blockComponent != null
            ? _blockComponent.AbsorbDamage(damage)
            : damage;

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
            int max = _maxHPComponent != null ? _maxHPComponent.maxHP : 15;
            _currentHP = Mathf.Clamp(value, 0, max);
        }
    }

    public int MaxHP
    {
        get { return _maxHPComponent != null ? _maxHPComponent.maxHP : 15; }
    }
}
