using UnityEngine;

public class currentEnergy : MonoBehaviour
{
    private int _currentEnergy;

    private maxEnergy _maxEnergyComponent;

    void Awake()
    {
        _maxEnergyComponent = GetComponent<maxEnergy>();
        RefillEnergy();
    }

    /// <summary>
    /// 回合开始时回满能量
    /// </summary>
    public void RefillEnergy(int penalty = 0)
    {
        int max = _maxEnergyComponent != null ? _maxEnergyComponent.energyMax : 3;
        _currentEnergy = Mathf.Max(0, max - Mathf.Max(0, penalty));
    }

    /// <summary>
    /// 消耗能量，返回是否成功
    /// </summary>
    public bool SpendEnergy(int amount)
    {
        if (amount <= 0) return true;
        if (_currentEnergy < amount) return false;

        _currentEnergy -= amount;
        return true;
    }

    /// <summary>
    /// 获得能量（不会超过上限）
    /// </summary>
    public void AddEnergy(int amount)
    {
        if (amount <= 0) return;

        int max = _maxEnergyComponent != null ? _maxEnergyComponent.energyMax : 3;
        _currentEnergy = Mathf.Min(_currentEnergy + amount, max);
    }

    /// <summary>
    /// 检查是否有足够的能量
    /// </summary>
    public bool HasEnoughEnergy(int amount)
    {
        return _currentEnergy >= amount;
    }

    public int CurrentEnergy
    {
        get { return _currentEnergy; }
        set { _currentEnergy = Mathf.Clamp(value, 0, MaxEnergy); }
    }

    public int MaxEnergy
    {
        get { return _maxEnergyComponent != null ? _maxEnergyComponent.energyMax : 3; }
    }
}
