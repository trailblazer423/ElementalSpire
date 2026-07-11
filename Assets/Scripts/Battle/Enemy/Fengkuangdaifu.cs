using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 疯狂戴夫：与三株植物共同组成 Boss 遭遇。
/// 三株植物存活时施加 1 层虚弱；任一植物死亡时，戴夫消耗 40 HP 补种。
/// </summary>
public class FengKuangDaiFu : EnemyController
{
    private const int RespawnCost = 40;
    private const int WeaknessAmount = 1;

    private readonly List<GameObject> _plants = new List<GameObject>();
    private BattleManager _battleManager;
    private MultiEnemyManager _multiEnemyManager;
    private bool _plantsSpawned;

    protected override void Start()
    {
        base.Start();
        _battleManager = FindObjectOfType<BattleManager>();
        _multiEnemyManager = _battleManager?.EnsureMultiEnemyManager();
        SpawnAllPlants();
        _plantsSpawned = true;
        DecideNextIntent();
    }

    protected override void DecideIntent()
    {
        if (!_plantsSpawned)
        {
            currentIntent = EnemyIntent.None;
            intentValue = 0;
            intentDescription = "准备花盆";
            return;
        }

        bool respawned = RespawnOneMissingPlant();
        if (!AreAllPlantsAlive())
        {
            currentIntent = EnemyIntent.None;
            intentValue = 0;
            intentDescription = _enemyHP != null && _enemyHP.CurrentHP < RespawnCost ? "生命不足，无法补种" : "补种植物";
            return;
        }

        currentIntent = EnemyIntent.Debuff;
        intentValue = WeaknessAmount;
        intentDescription = respawned ? "补种后 Wabibabu：虚弱+1" : "Wabibabu：虚弱+1";
    }

    protected override void ExecuteIntent()
    {
        if (currentIntent != EnemyIntent.Debuff || _playerState == null)
            return;

        // 植物会在玩家回合中死亡；敌方行动开始前再次检查，确保本回合立即补种。
        if (!AreAllPlantsAlive())
        {
            RespawnOneMissingPlant();
            if (!AreAllPlantsAlive())
                return;
        }

        _playerState.AddWeakness(intentValue);
        Debug.Log($"疯狂戴夫 Wabibabu！玩家虚弱 +{intentValue}。");
    }

    private void SpawnAllPlants()
    {
        SpawnPlant(PlantSlot.Peashooter);
        SpawnPlant(PlantSlot.Wallnut);
        SpawnPlant(PlantSlot.Sunflower);
    }

    private bool RespawnOneMissingPlant()
    {
        RemoveDeadPlantReferences();

        foreach (PlantSlot slot in new[] { PlantSlot.Peashooter, PlantSlot.Wallnut, PlantSlot.Sunflower })
        {
            if (HasAlivePlant(slot))
                continue;

            if (_enemyHP == null || _enemyHP.CurrentHP < RespawnCost)
                return false;

            _enemyHP.TakeDamage(RespawnCost);
            SpawnPlant(slot);
            Debug.Log($"疯狂戴夫消耗 {RespawnCost} HP，补种 {GetPlantData(slot).enemyName}。");
            return true;
        }

        return false;
    }

    private void SpawnPlant(PlantSlot slot)
    {
        EnemyData data = GetPlantData(slot);
        if (data == null)
        {
            Debug.LogError($"[疯狂戴夫] 找不到 {slot} 的 EnemyData。");
            return;
        }

        GameObject plant = new GameObject(data.enemyName);
        plant.transform.position = transform.position + GetPlantOffset(slot);

        // 植物由运行时创建，必须补齐可渲染节点；根物体位于战斗逻辑层（z = -10），
        // 子节点放到与主敌人相同的显示层（z = 0）。
        GameObject body = new GameObject("Body", typeof(SpriteRenderer), typeof(EnemyVisual));
        body.transform.SetParent(plant.transform, false);
        body.transform.localPosition = new Vector3(0f, 0f, 10f);
        body.transform.localScale = GetPlantVisualScale(slot);
        body.GetComponent<SpriteRenderer>().sortingOrder = 1;
        body.GetComponent<EnemyVisual>().SetColor(GetPlantColor(slot));

        plant.AddComponent<enemyMaxHP>();
        plant.AddComponent<enemyBlock>();
        plant.AddComponent<enemyHP>();
        plant.AddComponent<EnemyState>();

        EnemyController controller = plant.AddComponent(GetPlantControllerType(slot)) as EnemyController;
        EnemyIntentUI intentUi = plant.AddComponent<EnemyIntentUI>();
        controller.SetEnemyData(data);
        intentUi.SetController(controller);

        _plants.Add(plant);
        _multiEnemyManager?.RegisterEnemy(plant, data);
        _battleManager?.NotifyBattleInfoChanged();
    }

    private void RemoveDeadPlantReferences()
    {
        _plants.RemoveAll(plant =>
        {
            bool dead = plant == null || plant.GetComponent<enemyHP>() == null || plant.GetComponent<enemyHP>().CurrentHP <= 0;
            if (dead && plant != null)
                Destroy(plant);
            return dead;
        });
    }

    private bool AreAllPlantsAlive()
    {
        return HasAlivePlant(PlantSlot.Peashooter)
            && HasAlivePlant(PlantSlot.Wallnut)
            && HasAlivePlant(PlantSlot.Sunflower);
    }

    private bool HasAlivePlant(PlantSlot slot)
    {
        string plantName = GetPlantData(slot)?.enemyName;
        return _plants.Exists(plant => plant != null
            && plant.name == plantName
            && plant.GetComponent<enemyHP>() != null
            && plant.GetComponent<enemyHP>().CurrentHP > 0);
    }

    private EnemyData GetPlantData(PlantSlot slot)
    {
        string resourceName = slot switch
        {
            PlantSlot.Peashooter => "WandousheshouData",
            PlantSlot.Wallnut => "JianguoData",
            _ => "XiangrikuiData"
        };
        return Resources.Load<EnemyData>("EnemyData/" + resourceName);
    }

    private static Vector3 GetPlantOffset(PlantSlot slot)
    {
        return slot switch
        {
            PlantSlot.Wallnut => new Vector3(-2.5f, 0f, 0f),
            PlantSlot.Peashooter => Vector3.zero,
            _ => new Vector3(2.5f, 0f, 0f)
        };
    }

    private static System.Type GetPlantControllerType(PlantSlot slot)
    {
        return slot switch
        {
            PlantSlot.Peashooter => typeof(WanDouSheShou),
            PlantSlot.Wallnut => typeof(JianGuo),
            _ => typeof(XiangRiKui)
        };
    }

    private static Color GetPlantColor(PlantSlot slot)
    {
        return slot switch
        {
            PlantSlot.Peashooter => new Color(0.25f, 0.8f, 0.3f),
            PlantSlot.Wallnut => new Color(0.58f, 0.32f, 0.13f),
            _ => new Color(1f, 0.78f, 0.08f)
        };
    }

    private static Vector3 GetPlantVisualScale(PlantSlot slot)
    {
        return slot == PlantSlot.Wallnut
            ? new Vector3(2.4f, 2.4f, 1f)
            : new Vector3(2f, 2f, 1f);
    }

    public List<GameObject> GetPlants() => _plants;

    private enum PlantSlot
    {
        Peashooter,
        Wallnut,
        Sunflower
    }
}
