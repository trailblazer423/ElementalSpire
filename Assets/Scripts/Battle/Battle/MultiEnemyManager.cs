using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 多敌人管理器 - 管理战斗中所有敌人个体。
///
/// 核心接口：
/// - AliveEnemies: 获取所有存活的敌人
/// - DefaultTarget: 默认攻击目标（第一个存活敌人）
/// - DealDamageToTarget(EnemyUnit, int): 对指定目标造成伤害
/// - GetAllControllers(): 获取所有敌人的控制器
/// - SpawnEnemy(): 动态生成新敌人
/// - RemoveDeadEnemy(): 清理死亡敌人
/// </summary>
public class MultiEnemyManager : MonoBehaviour
{
    [Header("敌人列表（运行时维护）")]
    [SerializeField] private List<EnemyUnit> _enemies = new List<EnemyUnit>();

    [Header("敌人数据池（Inspector 配置）")]
    [SerializeField] private EnemyData[] _normalEnemyPool;
    [SerializeField] private EnemyData[] _eliteEnemyPool;
    [SerializeField] private EnemyData _bossEnemyData;

    [Header("生成设置")]
    [SerializeField] private float _spawnSpacingX = 3.0f;  // 多敌人水平间距
    [SerializeField] private Transform _enemySpawnRoot;    // 敌人父节点（可选）

    // ===== 事件 =====
    public System.Action OnEnemiesChanged;  // 敌人列表变化时触发

    // ===== 属性 =====
    public List<EnemyUnit> AllEnemies => _enemies;
    public EnemyUnit DefaultTarget => GetDefaultTarget();
    public int TotalAliveCount => _enemies.Count(e => e.IsAlive);
    public bool AllDead => TotalAliveCount == 0 && _enemies.Count > 0;

    // ===== 向后兼容属性（返回默认目标的组件） =====
    public EnemyController PrimaryController => DefaultTarget?.controller;
    public enemyHP PrimaryHP => DefaultTarget?.hp;
    public enemyBlock PrimaryBlock => DefaultTarget?.block;
    public EnemyState PrimaryState => DefaultTarget?.state;

    /// <summary>
    /// 获取默认攻击目标：第一个存活敌人
    /// </summary>
    public EnemyUnit GetDefaultTarget()
    {
        return _enemies.FirstOrDefault(e => e.IsAlive);
    }

    /// <summary>
    /// 获取所有存活的敌人
    /// </summary>
    public List<EnemyUnit> GetAliveEnemies()
    {
        return _enemies.Where(e => e.IsAlive).ToList();
    }

    /// <summary>
    /// 获取所有 EnemyController（用于全体效果，如 KeAiNaiLong 的群体护盾）
    /// </summary>
    public EnemyController[] GetAllControllers()
    {
        return _enemies
            .Where(e => e.controller != null)
            .Select(e => e.controller)
            .ToArray();
    }

    /// <summary>
    /// 获取特定敌人的索引位置（用于 UI 布局）
    /// </summary>
    public int GetEnemyIndex(EnemyUnit unit)
    {
        return _enemies.IndexOf(unit);
    }

    // ===== 敌人注册/移除 =====

    /// <summary>
    /// 注册一个敌人到管理器中（用于场景中预先放置的敌人）
    /// </summary>
    public EnemyUnit RegisterEnemy(GameObject enemyObject, EnemyData data = null)
    {
        var unit = EnemyUnit.FromGameObject(enemyObject, data);
        if (unit == null) return null;

        // 避免重复注册
        if (_enemies.Exists(e => e.gameObject == enemyObject))
            return _enemies.Find(e => e.gameObject == enemyObject);

        // 如果传入了数据，注入到 controller
        if (data != null && unit.controller != null)
            unit.controller.SetEnemyData(data);

        // 克隆敌人会携带原对象的 UI 引用；这里将其绑定到本单位自己的有效控制器。
        if (unit.intentUI != null && unit.controller != null)
            unit.intentUI.SetController(unit.controller);

        _enemies.Add(unit);
        OnEnemiesChanged?.Invoke();
        Debug.Log($"[MultiEnemyManager] 注册敌人: {unit.DisplayName} (总数: {_enemies.Count})");
        return unit;
    }

    /// <summary>
    /// 动态生成一个新敌人并注册
    /// </summary>
    public EnemyUnit SpawnEnemy(GameObject enemyPrefab, EnemyData data, Vector3 position)
    {
        if (enemyPrefab == null || data == null)
        {
            Debug.LogWarning("[MultiEnemyManager] SpawnEnemy: prefab 或 data 为空");
            return null;
        }

        Transform parent = _enemySpawnRoot != null ? _enemySpawnRoot : transform;
        GameObject instance = Instantiate(enemyPrefab, position, Quaternion.identity, parent);
        instance.name = data.enemyName;

        // 设置数据
        var controller = instance.GetComponent<EnemyController>();
        if (controller != null)
            controller.SetEnemyData(data);

        return RegisterEnemy(instance, data);
    }

    /// <summary>
    /// 从敌人列表中移除已死亡或已销毁的敌人
    /// </summary>
    public void RemoveDeadEnemy(EnemyUnit unit)
    {
        if (unit == null || !_enemies.Contains(unit)) return;

        _enemies.Remove(unit);
        Debug.Log($"[MultiEnemyManager] 移除敌人: {unit.DisplayName} (剩余: {_enemies.Count})");
        OnEnemiesChanged?.Invoke();
    }

    /// <summary>
    /// 清理所有已销毁的敌人引用
    /// </summary>
    public void CleanupDestroyedEnemies()
    {
        _enemies.RemoveAll(e => e.gameObject == null || e.hp == null);
        OnEnemiesChanged?.Invoke();
    }

    // ===== 对敌操作 =====

    /// <summary>
    /// 对指定敌人造成伤害
    /// </summary>
    public void DealDamageToTarget(EnemyUnit target, int damage)
    {
        if (target == null || !target.IsAlive) return;
        target.DealDamage(damage);
    }

    /// <summary>
    /// 对默认目标造成伤害（向后兼容）
    /// </summary>
    public void DealDamageToDefault(int damage)
    {
        DealDamageToTarget(DefaultTarget, damage);
    }

    /// <summary>
    /// 对所有存活敌人造成伤害
    /// </summary>
    public void DealDamageToAll(int damage)
    {
        foreach (var enemy in GetAliveEnemies())
            enemy.DealDamage(damage);
    }

    /// <summary>
    /// 对指定敌人施加中毒
    /// </summary>
    public void ApplyPoisonToTarget(EnemyUnit target, int stacks)
    {
        target?.ApplyPoison(stacks);
    }

    /// <summary>
    /// 对默认目标施加中毒（向后兼容）
    /// </summary>
    public void ApplyPoisonToDefault(int stacks)
    {
        ApplyPoisonToTarget(DefaultTarget, stacks);
    }

    /// <summary>
    /// 对指定敌人施加力量
    /// </summary>
    public void ApplyPowerToTarget(EnemyUnit target, int amount)
    {
        target?.ApplyPower(amount);
    }

    /// <summary>
    /// 对指定敌人添加护盾
    /// </summary>
    public void AddBlockToTarget(EnemyUnit target, int amount)
    {
        target?.AddBlock(amount);
    }

    /// <summary>
    /// 对所有存活敌人添加护盾（如 KeAiNaiLong 的效果）
    /// </summary>
    public void AddBlockToAll(int amount)
    {
        foreach (var enemy in GetAliveEnemies())
            enemy.AddBlock(amount);
    }

    /// <summary>
    /// 结算所有敌人的中毒
    /// </summary>
    /// <returns>中毒致死的敌人列表</returns>
    public List<EnemyUnit> TickAllPoisons()
    {
        var deadFromPoison = new List<EnemyUnit>();
        foreach (var enemy in GetAliveEnemies())
        {
            if (enemy.state == null) continue;

            int poisonDamage = enemy.state.TriggerPoisonTick();
            if (poisonDamage > 0)
            {
                enemy.DealDamage(poisonDamage);
                Debug.Log($"[毒] {enemy.DisplayName} 中毒 {poisonDamage} 点，余毒 {enemy.state.PoisonStacks}");

                if (!enemy.IsAlive)
                    deadFromPoison.Add(enemy);
            }
        }
        return deadFromPoison;
    }

    // ===== 敌人数据选择 =====

    /// <summary>
    /// 根据节点类型生成敌人
    /// </summary>
    public void SelectAndSpawnEnemies(GameObject enemyPrefab)
    {
        var gm = GameManager.Instance;
        string nodeType = gm != null ? gm.currentNodeType : "";

        // 自动填充数据池
        AutoFillPools();

        int enemyCount = GetEnemyCountForNode(nodeType);

        for (int i = 0; i < enemyCount; i++)
        {
            EnemyData data = SelectEnemyDataForNode(nodeType, i);
            if (data == null) continue;

            float xOffset = (i - (enemyCount - 1) * 0.5f) * _spawnSpacingX;
            Vector3 spawnPos = transform.position + new Vector3(xOffset, 0, 0);
            SpawnEnemy(enemyPrefab, data, spawnPos);
        }
    }

    private int GetEnemyCountForNode(string nodeType)
    {
        // Boss 战只生成1个 Boss；精英战随机1~2；普通战随机1~3
        return nodeType switch
        {
            "Boss" => 1,
            "Elite" => UnityEngine.Random.Range(1, 3),   // 1~2个
            _ => UnityEngine.Random.Range(1, 4),          // 1~3个
        };
    }

    private EnemyData SelectEnemyDataForNode(string nodeType, int index)
    {
        if (nodeType == "Boss" && _bossEnemyData != null)
            return _bossEnemyData;

        if (nodeType == "Elite" && _eliteEnemyPool != null && _eliteEnemyPool.Length > 0)
            return _eliteEnemyPool[UnityEngine.Random.Range(0, _eliteEnemyPool.Length)];

        if (_normalEnemyPool != null && _normalEnemyPool.Length > 0)
            return _normalEnemyPool[UnityEngine.Random.Range(0, _normalEnemyPool.Length)];

        // 兜底
        return _eliteEnemyPool?.FirstOrDefault() ?? _bossEnemyData;
    }

    private void AutoFillPools()
    {
        EnemyData[] allData = Resources.LoadAll<EnemyData>("EnemyData");
        if (allData == null || allData.Length == 0) return;

        if (_normalEnemyPool == null || _normalEnemyPool.Length == 0)
            _normalEnemyPool = System.Array.FindAll(allData, d => d.enemyType == EnemyType.Normal);
        if (_eliteEnemyPool == null || _eliteEnemyPool.Length == 0)
            _eliteEnemyPool = System.Array.FindAll(allData, d => d.enemyType == EnemyType.Elite);
        if (_bossEnemyData == null)
            _bossEnemyData = System.Array.Find(allData, d => d.enemyType == EnemyType.Boss);
    }

    /// <summary>
    /// 让所有存活敌人决定下回合意图
    /// </summary>
    public void AllDecideNextIntent()
    {
        foreach (var enemy in GetAliveEnemies())
        {
            if (enemy.controller != null && enemy.controller.enabled)
                enemy.controller.DecideNextIntent();
        }
    }

    // ===== 调试 =====
#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;

        for (int i = 0; i < _enemies.Count; i++)
        {
            var e = _enemies[i];
            if (e?.gameObject == null) continue;

            UnityEditor.Handles.Label(
                e.gameObject.transform.position + Vector3.up * 1.5f,
                $"[{i}] {e.DisplayName} HP:{e.hp?.CurrentHP}/{e.hp?.MaxHP} " +
                $"{(e.IsAlive ? "存活" : "死亡")} {(e == DefaultTarget ? "<<<" : "")}");
        }
    }
#endif
}
