using UnityEngine;

/// <summary>
/// 敌人状态 - 管理中毒等战斗状态，挂载在 Enemy 对象上
/// </summary>
public class EnemyState : MonoBehaviour
{
    [Header("中毒")]
    [SerializeField] private int _poison = 0;

    /// <summary>中毒层数</summary>
    public int poison => _poison;

    /// <summary>
    /// 增加中毒层数
    /// </summary>
    public void AddPoison(int amount)
    {
        if (amount > 0)
            _poison += amount;
    }

    /// <summary>
    /// 结算中毒伤害：造成等同于当前层数的伤害，层数减1
    /// </summary>
    public int TriggerPoisonTick()
    {
        if (_poison <= 0) return 0;

        int damage = _poison;
        _poison = Mathf.Max(0, _poison - 1);
        return damage;
    }

    /// <summary>
    /// 减少中毒层数
    /// </summary>
    public void RemovePoison(int amount)
    {
        if (amount > 0)
            _poison = Mathf.Max(0, _poison - amount);
    }

    /// <summary>
    /// 重置中毒
    /// </summary>
    public void ResetPoison()
    {
        _poison = 0;
    }
}
