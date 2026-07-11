using UnityEngine;

/// <summary>
/// 玩家状态 - 管理力量、水源和持续能力，挂载在 Player 对象上。
/// </summary>
public class PlayerState : MonoBehaviour
{
    [Header("力量")]
    [SerializeField] private int _power = 0;

    [Header("水源")]
    [SerializeField] private int _waterSource = 0;

    [Header("能力状态")]
    [SerializeField] private int _demonFormPowerPerTurn = 0;
    [SerializeField] private bool _hellionActive = false;
    [SerializeField] private bool _barricadeActive = false;
    [SerializeField] private int _lakeEchoBlockPerWater = 0;
    [SerializeField] private bool _waterResonanceActive = false;
    [SerializeField] private int _triCoreLimit = 0;
    [SerializeField] private int _triCoreUsed = 0;

    private bool _waterResonanceUsedThisTurn = false;
    private bool _reactionThisTurn = false;

    [Header("怪物施加的状态")]
    [SerializeField] private int _poisonStacks = 0;
    [SerializeField] private int _weakness = 0;
    [SerializeField] private int _vulnerable = 0;
    [SerializeField] private int _nextTurnEnergyPenalty = 0;

    public int power => _power;
    public int PoisonStacks => _poisonStacks;
    public int Weakness => _weakness;
    public int Vulnerable => _vulnerable;
    public int WaterSource => _waterSource;
    public int DemonFormPowerPerTurn => _demonFormPowerPerTurn;
    public bool HellionActive => _hellionActive;
    public bool BarricadeActive => _barricadeActive;
    public int LakeEchoBlockPerWater => _lakeEchoBlockPerWater;
    public bool WaterResonanceActive => _waterResonanceActive;
    public bool WaterResonanceUsedThisTurn => _waterResonanceUsedThisTurn;
    public bool ReactionThisTurn => _reactionThisTurn;
    public int TriCoreLimit => _triCoreLimit;
    public int TriCoreUsed => _triCoreUsed;

    public void AddPower(int amount)
    {
        if (amount > 0)
            _power += amount;
    }

    public void AddPoison(int amount)
    {
        if (amount > 0)
            _poisonStacks += amount;
    }

    public int TickPoison()
    {
        if (_poisonStacks <= 0) return 0;

        int damage = _poisonStacks;
        _poisonStacks = Mathf.Max(0, _poisonStacks - 1);
        return damage;
    }

    public void AddWeakness(int amount)
    {
        if (amount > 0)
            _weakness += amount;
    }

    public void AddVulnerable(int amount)
    {
        if (amount > 0)
            _vulnerable += amount;
    }

    public void TickVulnerable()
    {
        _vulnerable = Mathf.Max(0, _vulnerable - 1);
    }

    public void AddNextTurnEnergyPenalty(int amount)
    {
        if (amount > 0)
            _nextTurnEnergyPenalty += amount;
    }

    public int ConsumeNextTurnEnergyPenalty()
    {
        int penalty = _nextTurnEnergyPenalty;
        _nextTurnEnergyPenalty = 0;
        return penalty;
    }

    public void RemovePower(int amount)
    {
        if (amount > 0)
            _power = Mathf.Max(0, _power - amount);
    }

    public void ResetPower()
    {
        _power = 0;
    }

    public int GetDamageWithPower(int baseDamage)
    {
        return baseDamage + _power;
    }

    public void AddWater(int amount)
    {
        if (amount > 0)
            _waterSource += amount;
    }

    public bool HasEnoughWater(int amount)
    {
        return amount <= 0 || _waterSource >= amount;
    }

    public bool SpendWater(int amount)
    {
        if (amount <= 0) return true;
        if (_waterSource < amount) return false;

        _waterSource -= amount;
        return true;
    }

    public void AddDemonFormPowerPerTurn(int amount)
    {
        if (amount > 0)
            _demonFormPowerPerTurn += amount;
    }

    public void SetHellionActive()
    {
        _hellionActive = true;
    }

    public void SetBarricadeActive()
    {
        _barricadeActive = true;
    }

    public void AddLakeEchoBlockPerWater(int amount)
    {
        if (amount > 0)
            _lakeEchoBlockPerWater += amount;
    }

    public void SetWaterResonanceActive()
    {
        _waterResonanceActive = true;
    }

    public void ResetTurnFlags()
    {
        _waterResonanceUsedThisTurn = false;
        _reactionThisTurn = false;
    }

    public void MarkWaterResonanceUsed()
    {
        _waterResonanceUsedThisTurn = true;
    }

    public void MarkReactionThisTurn()
    {
        _reactionThisTurn = true;
    }

    public void AddTriCoreLimit(int amount)
    {
        if (amount > 0)
            _triCoreLimit += amount;
    }

    public bool TryUseTriCore()
    {
        if (_triCoreUsed >= _triCoreLimit)
            return false;

        _triCoreUsed++;
        return true;
    }

    public void ResetCombatState()
    {
        _power = 0;
        _waterSource = 0;
        _demonFormPowerPerTurn = 0;
        _hellionActive = false;
        _barricadeActive = false;
        _lakeEchoBlockPerWater = 0;
        _waterResonanceActive = false;
        _triCoreLimit = 0;
        _triCoreUsed = 0;
        _poisonStacks = 0;
        _weakness = 0;
        _vulnerable = 0;
        _nextTurnEnergyPenalty = 0;
        ResetTurnFlags();
    }
}
