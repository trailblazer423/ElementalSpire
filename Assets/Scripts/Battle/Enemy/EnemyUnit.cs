using UnityEngine;

/// <summary>
/// 敌人单元 - 将单个敌人的所有组件打包为一个逻辑单元。
/// 供 MultiEnemyManager 管理和 CardEffectResolver 选择目标使用。
/// </summary>
[System.Serializable]
public class EnemyUnit
{
    public GameObject gameObject;
    public EnemyController controller;
    public enemyHP hp;
    public enemyBlock block;
    public EnemyState state;
    public EnemyIntentUI intentUI;
    public EnemyData data;

    /// <summary>这个敌人当前是否存活</summary>
    public bool IsAlive => hp != null && hp.CurrentHP > 0;

    /// <summary>敌人显示名称</summary>
    public string DisplayName
    {
        get
        {
            if (controller != null && controller.enemyData != null)
                return controller.enemyData.enemyName;
            if (data != null)
                return data.enemyName;
            return gameObject != null ? gameObject.name : "???";
        }
    }

    /// <summary>
    /// 从 GameObject 创建 EnemyUnit，自动查找所有战斗组件。
    /// </summary>
    public static EnemyUnit FromGameObject(GameObject go, EnemyData overrideData = null)
    {
        if (go == null) return null;

        var unit = new EnemyUnit
        {
            gameObject = go,
            controller = GetActiveController(go),
            hp = go.GetComponent<enemyHP>(),
            block = go.GetComponent<enemyBlock>(),
            state = go.GetComponent<EnemyState>(),
            intentUI = go.GetComponent<EnemyIntentUI>(),
        };

        if (unit.controller != null && unit.controller.enemyData != null)
            unit.data = unit.controller.enemyData;
        else
            unit.data = overrideData;

        return unit;
    }

    private static EnemyController GetActiveController(GameObject go)
    {
        foreach (EnemyController candidate in go.GetComponents<EnemyController>())
        {
            if (candidate.enabled && candidate.enemyData != null)
                return candidate;
        }

        return go.GetComponent<EnemyController>();
    }

    /// <summary>对该敌人造成伤害（护盾抵扣后）</summary>
    public void DealDamage(int damage)
    {
        if (hp == null) return;
        hp.TakeDamage(damage);
    }

    /// <summary>对该敌人施加中毒</summary>
    public void ApplyPoison(int stacks)
    {
        if (state == null || stacks <= 0) return;
        state.AddPoisonStacks(stacks);
    }

    /// <summary>对该敌人施加力量</summary>
    public void ApplyPower(int amount)
    {
        if (state == null || amount <= 0) return;
        state.AddPower(amount);
    }

    /// <summary>对该敌人施加虚弱</summary>
    public void ApplyWeakness(int amount)
    {
        if (state == null || amount <= 0) return;
        state.AddWeakness(amount);
    }

    /// <summary>为该敌人添加护盾</summary>
    public void AddBlock(int amount)
    {
        if (block == null || amount <= 0) return;
        block.AddBlock(amount);
    }

    /// <summary>重置该敌人的战斗状态</summary>
    public void ResetCombatState()
    {
        state?.ResetCombatState();
    }
}
