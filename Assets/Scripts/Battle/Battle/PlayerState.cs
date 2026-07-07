using UnityEngine;

/// <summary>
/// 玩家状态 - 管理力量等战斗状态，挂载在 Player 对象上
/// </summary>
public class PlayerState : MonoBehaviour
{
    [Header("力量")]
    [SerializeField] private int _power = 0;

    /// <summary>力量值（每点力量使每段攻击伤害+1）</summary>
    public int power => _power;

    /// <summary>
    /// 增加力量
    /// </summary>
    public void AddPower(int amount)
    {
        if (amount > 0)
            _power += amount;
    }

    /// <summary>
    /// 减少力量
    /// </summary>
    public void RemovePower(int amount)
    {
        if (amount > 0)
            _power = Mathf.Max(0, _power - amount);
    }

    /// <summary>
    /// 重置力量
    /// </summary>
    public void ResetPower()
    {
        _power = 0;
    }

    /// <summary>
    /// 计算受力量加成后的伤害（每段）
    /// </summary>
    public int GetDamageWithPower(int baseDamage)
    {
        return baseDamage + _power;
    }
}
