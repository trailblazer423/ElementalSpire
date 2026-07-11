using UnityEngine;

/// <summary>
/// 战斗场景多敌人配置器 - 挂载在 BattleManager 或场景中的任意对象上。
/// 提供快捷配置：在 Inspector 中设置后自动完成多敌人模式初始化。
///
/// 使用方式：
/// 1. 将该脚本挂载到场景中
/// 2. 将敌人的 Prefab 拖入 _enemyPrefab 字段
/// 3. （可选）调整出生点间距等参数
/// 4. 运行即可自动生成多个敌人
/// </summary>
[RequireComponent(typeof(BattleManager))]
public class MultiEnemyBattleSetup : MonoBehaviour
{
    [Header("多敌人设置")]
    [SerializeField] private GameObject _enemyPrefab;
    [SerializeField] private Transform _enemySpawnRoot;
    [SerializeField] private float _spawnSpacingX = 3.0f;
    [SerializeField] private int _forceEnemyCount = 0;  // 0=按节点类型自动决定

    [Header("调试")]
    [SerializeField] private bool _spawnOnStart = true;

    private BattleManager _battleManager;
    private MultiEnemyManager _multiEnemyManager;

    void Awake()
    {
        _battleManager = GetComponent<BattleManager>();

        if (_spawnOnStart)
            SetupMultiEnemySystem();
    }

    /// <summary>
    /// 手动调用以初始化多敌人系统（也可在 Inspector 中 enable/disable 后调用）
    /// </summary>
    [ContextMenu("初始化多敌人系统")]
    public void SetupMultiEnemySystem()
    {
        if (_enemyPrefab == null)
        {
            Debug.LogError("[MultiEnemyBattleSetup] 未设置敌人 Prefab！请在 Inspector 中拖入。");
            return;
        }

        // 确保 MultiEnemyManager 存在
        _multiEnemyManager = GetComponent<MultiEnemyManager>();
        if (_multiEnemyManager == null)
            _multiEnemyManager = gameObject.AddComponent<MultiEnemyManager>();

        // 设置为 BattleManager 的子字段
        // (通过反射或直接赋值)
        var bmType = typeof(BattleManager);
        var mmField = bmType.GetField("_multiEnemyManager",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var epField = bmType.GetField("_enemyPrefab",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (mmField != null)
            mmField.SetValue(_battleManager, _multiEnemyManager);
        if (epField != null)
            epField.SetValue(_battleManager, _enemyPrefab);

        Debug.Log($"[MultiEnemyBattleSetup] 多敌人系统已初始化。Prefab: {_enemyPrefab.name}, " +
                  $"间距: {_spawnSpacingX}, 强制数量: {_forceEnemyCount}");
    }

#if UNITY_EDITOR
    [ContextMenu("清除所有生成的敌人")]
    public void ClearAllEnemies()
    {
        if (_multiEnemyManager != null)
        {
            var enemies = _multiEnemyManager.AllEnemies;
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                if (enemies[i]?.gameObject != null)
                    Destroy(enemies[i].gameObject);
            }
            enemies.Clear();
            Debug.Log("[MultiEnemyBattleSetup] 已清除所有敌人。");
        }
    }
#endif
}
