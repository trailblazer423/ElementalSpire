using System.Collections.Generic;
using UnityEngine;

/// <summary>疯狂戴夫：管理三个花盆与植物复活。</summary>
public class FengKuangDaiFu : EnemyController
{
    private const int RespawnCost = 20;

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
            intentDescription = "准备花盆";
            return;
        }

        RespawnMissingPlants();
        currentIntent = EnemyIntent.None;
        intentValue = 0;
        intentDescription = AreAllPlantsAlive()
            ? "花盆运作中"
            : "生命不足，无法补种";
    }

    protected override void ExecuteIntent()
    {
        // 玩家回合可能击杀植物；敌方行动前立即执行对应花盆的补种。
        RespawnMissingPlants();
    }

    private void SpawnAllPlants()
    {
        SpawnPlant(PlantSlot.Peashooter);
        SpawnPlant(PlantSlot.Wallnut);
        SpawnPlant(PlantSlot.Sunflower);
    }

    private void RespawnMissingPlants()
    {
        RemoveDeadPlantReferences();

        foreach (PlantSlot slot in new[] { PlantSlot.Peashooter, PlantSlot.Wallnut, PlantSlot.Sunflower })
        {
            if (HasAlivePlant(slot))
                continue;
            if (_enemyHP == null || _enemyHP.CurrentHP < RespawnCost)
                continue;

            _enemyHP.TakeDamage(RespawnCost);
            SpawnPlant(slot);
            Debug.Log($"疯狂戴夫消耗 {RespawnCost} HP，在花盆中重新种植 {GetPlantData(slot).enemyName}。");
        }
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
            {
                HandlePlantDeath(GetPlantSlot(plant));
                Destroy(plant);
            }
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

    public bool ShouldReducePlantDamage(GameObject plant)
    {
        if (plant == null || HasAlivePlant(PlantSlot.Wallnut) == false)
            return false;

        return plant.GetComponent<WanDouSheShou>() != null || plant.GetComponent<XiangRiKui>() != null;
    }

    private void HandlePlantDeath(PlantSlot slot)
    {
        if (slot == PlantSlot.Wallnut)
        {
            _enemyBlock?.AddBlock(10);
            Debug.Log("坚果死亡：其他植物解除减伤，疯狂戴夫获得 10 点护盾。");
        }
        else if (slot == PlantSlot.Sunflower)
        {
            _playerState?.ClearVulnerable();
            Debug.Log("向日葵死亡：清除玩家全部易伤层数。");
        }
    }

    private PlantSlot GetPlantSlot(GameObject plant)
    {
        if (plant != null)
        {
            if (plant.name == GetPlantData(PlantSlot.Wallnut)?.enemyName) return PlantSlot.Wallnut;
            if (plant.name == GetPlantData(PlantSlot.Sunflower)?.enemyName) return PlantSlot.Sunflower;
        }
        return PlantSlot.Peashooter;
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
