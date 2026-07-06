using UnityEngine;

public class enemyBlock : MonoBehaviour
{
    private int _currentBlock;

    public int CurrentBlock
    {
        get { return _currentBlock; }
    }

    public void AddBlock(int amount)
    {
        if (amount > 0)
            _currentBlock += amount;
    }

    /// <summary>
    /// 消耗护盾，返回未能抵消的剩余伤害
    /// </summary>
    public int AbsorbDamage(int damage)
    {
        if (damage <= 0) return 0;

        if (_currentBlock >= damage)
        {
            _currentBlock -= damage;
            return 0;
        }
        else
        {
            int remaining = damage - _currentBlock;
            _currentBlock = 0;
            return remaining;
        }
    }

    public void ResetBlock()
    {
        _currentBlock = 0;
    }
}
