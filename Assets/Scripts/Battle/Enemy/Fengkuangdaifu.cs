using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 疯狂戴夫 - Boss，管理3个植物，按顺序复活
/// </summary>
public class FengKuangDaiFu : EnemyController
{
    [Header("植物预制体")]
    public GameObject wanDouSheShouPrefab;
    public GameObject jianGuoPrefab;
    public GameObject xiangRiKuiPrefab;

    private List<GameObject> _plants = new List<GameObject>();
    private int _peashooterReviveCount = 0;   // 豌豆射手复活次数

    void Start()
    {
        _enemyHP = GetComponent<enemyHP>();
        _enemyBlock = GetComponent<enemyBlock>();
        _playerHP = GameObject.Find("Player")?.GetComponent<playerHP>();
        _playerState = GameObject.Find("Player")?.GetComponent<PlayerState>();

        if (enemyData != null)
        {
            enemyMaxHP maxHPComp = GetComponent<enemyMaxHP>();
            if (maxHPComp != null) maxHPComp.maxHP = enemyData.maxHP;
            _enemyHP.CurrentHP = enemyData.maxHP;
        }

        SpawnAllPlants();
    }

    void SpawnAllPlants()
    {
        // 按顺序生成：坚果（左）→ 豌豆射手（中）→ 向日葵（右）
        if (jianGuoPrefab != null)
        {
            GameObject plant = Instantiate(jianGuoPrefab, transform.position + new Vector3(-2.5f, 0, 0), Quaternion.identity);
            plant.name = "坚果";
            _plants.Add(plant);
        }
        if (wanDouSheShouPrefab != null)
        {
            GameObject plant = Instantiate(wanDouSheShouPrefab, transform.position + new Vector3(0, 0, 0), Quaternion.identity);
            plant.name = "豌豆射手";
            _plants.Add(plant);
        }
        if (xiangRiKuiPrefab != null)
        {
            GameObject plant = Instantiate(xiangRiKuiPrefab, transform.position + new Vector3(2.5f, 0, 0), Quaternion.identity);
            plant.name = "向日葵";
            _plants.Add(plant);
        }
        Debug.Log($"疯狂戴夫 召唤了 {_plants.Count} 个植物！");
    }

    protected override void DecideIntent()
    {
        CheckAndRespawnPlants();

        // 检查是否所有植物都活着
        bool allAlive = true;
        foreach (var plant in _plants)
        {
            if (plant == null) { allAlive = false; break; }
        }

        if (allAlive)
        {
            currentIntent = EnemyIntent.Debuff;
            intentValue = 1;
            Debug.Log("疯狂戴夫：植物全活，上虚弱");
        }
        else
        {
            currentIntent = EnemyIntent.Attack;
            intentValue = 8;
            Debug.Log("疯狂戴夫：有植物死亡，攻击");
        }
    }

    void CheckAndRespawnPlants()
    {
        // 清理已死亡的植物
        _plants.RemoveAll(p => p == null);

        // 检查缺失的植物（按顺序：坚果 → 豌豆射手 → 向日葵）
        bool hasJianGuo = _plants.Exists(p => p != null && p.name == "坚果");
        bool hasWanDou = _plants.Exists(p => p != null && p.name == "豌豆射手");
        bool hasXiangRiKui = _plants.Exists(p => p != null && p.name == "向日葵");

        // 按优先级复活
        if (!hasJianGuo && _enemyHP.CurrentHP >= 40)
        {
            RespawnPlant(jianGuoPrefab, "坚果", new Vector3(-2.5f, 0, 0));
        }
        else if (!hasWanDou && _enemyHP.CurrentHP >= 40)
        {
            RespawnPlant(wanDouSheShouPrefab, "豌豆射手", new Vector3(0, 0, 0));
            _peashooterReviveCount++;
        }
        else if (!hasXiangRiKui && _enemyHP.CurrentHP >= 40)
        {
            RespawnPlant(xiangRiKuiPrefab, "向日葵", new Vector3(2.5f, 0, 0));
        }
    }

    void RespawnPlant(GameObject prefab, string name, Vector3 position)
    {
        if (prefab == null) return;
        _enemyHP.TakeDamage(40);
        GameObject newPlant = Instantiate(prefab, transform.position + position, Quaternion.identity);
        newPlant.name = name;
        _plants.Add(newPlant);

        // 如果是豌豆射手，更新复活次数
        if (name == "豌豆射手")
        {
            WanDouSheShou peashooter = newPlant.GetComponent<WanDouSheShou>();
            if (peashooter != null)
            {
                peashooter.SetReviveCount(_peashooterReviveCount);
            }
        }

        Debug.Log($"疯狂戴夫 消耗40HP复活 {name}！剩余HP {_enemyHP.CurrentHP}");
    }

    public List<GameObject> GetPlants()
    {
        return _plants;
    }

    protected override void ExecuteIntent()
    {
        switch (currentIntent)
        {
            case EnemyIntent.Debuff:
                if (_playerState != null)
                {
                    _playerState.AddWeakness(intentValue);
                    Debug.Log($"疯狂戴夫 Wabibabu！全体虚弱 {intentValue} 层，当前虚弱 {_playerState.Weakness}/5");
                }
                break;

            case EnemyIntent.Attack:
                _playerHP.TakeDamage(intentValue);
                Debug.Log($"疯狂戴夫 攻击！造成 {intentValue} 点伤害");
                break;

            default:
                base.ExecuteIntent();
                break;
        }
    }
}