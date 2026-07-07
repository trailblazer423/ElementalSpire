using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using ElementalSpire.Cards;

public class BattleManager : MonoBehaviour
{
    public static BattleManager Instance;

    public GameObject playerObject;
    public GameObject enemyObject;

    private playerHP _playerHP;
    private enemyHP _enemyHP;
    private currentEnergy _playerEnergy;
    private playerBlock _playerBlock;
    private playerStrength _playerStrength;

    // ===== 牌堆系统（使用CardData） =====
    private List<CardData> drawPile = new List<CardData>();
    private List<CardData> hand = new List<CardData>();
    private List<CardData> discardPile = new List<CardData>();

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        _playerHP = playerObject.GetComponent<playerHP>();
        _enemyHP = enemyObject.GetComponent<enemyHP>();
        _playerEnergy = playerObject.GetComponent<currentEnergy>();
        _playerBlock = playerObject.GetComponent<playerBlock>();
        _playerStrength = playerObject.GetComponent<playerStrength>();

        StartBattle();
    }

    public void StartBattle()
    {
        Debug.Log("===== 战斗开始！=====");

        // 从卡牌库获取火元素牌组
        drawPile = CardDeckLibrary
            .GetCardsByDeckPreset(DeckPreset.Fire)
            .ToList();

        ShuffleDrawPile();

        _playerHP.CurrentHP = _playerHP.MaxHP;
        _enemyHP.CurrentHP = _enemyHP.MaxHP;
        _playerBlock.ResetBlock();
        _playerStrength.Strength = 0;

        StartNewTurn();

        Debug.Log($"玩家血量: {_playerHP.CurrentHP}/{_playerHP.MaxHP}");
        Debug.Log($"敌人血量: {_enemyHP.CurrentHP}/{_enemyHP.MaxHP}");
        Debug.Log($"牌组: 火元素，共 {drawPile.Count} 张牌");
        Debug.Log("按 1 打出第一张手牌，按 2 结束回合");
    }

    void StartNewTurn()
    {
        _playerEnergy.RefillEnergy();
        DrawCards(5);
    }

    void DrawCards(int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (drawPile.Count == 0)
            {
                if (discardPile.Count == 0) break;
                Debug.Log("抽牌堆为空，洗入弃牌堆！");
                drawPile.AddRange(discardPile);
                discardPile.Clear();
                ShuffleDrawPile();
            }

            if (drawPile.Count > 0)
            {
                CardData card = drawPile[0];
                drawPile.RemoveAt(0);
                hand.Add(card);
                Debug.Log($"抽到: {card.cardName} ({card.elementType})");
            }
        }

        Debug.Log($"抽了 {count} 张牌，手牌现在有 {hand.Count} 张");
    }

    void ShuffleDrawPile()
    {
        for (int i = 0; i < drawPile.Count; i++)
        {
            int randomIndex = Random.Range(i, drawPile.Count);
            CardData temp = drawPile[i];
            drawPile[i] = drawPile[randomIndex];
            drawPile[randomIndex] = temp;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            PlayCard(0);
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            EndPlayerTurn();
        }
    }

    public void PlayCard(int handIndex)
    {
        if (handIndex >= hand.Count)
        {
            Debug.Log("手牌中没有这张牌！");
            return;
        }

        CardData card = hand[handIndex];

        if (!_playerEnergy.SpendEnergy(card.cost))
        {
            Debug.Log($"能量不足！需要 {card.cost} 点能量");
            return;
        }

        // ===== 根据卡牌类型执行效果 =====
        int totalDamage = 0;

        if (card.HasCardType(CardType.Attack))
        {
            // 攻击牌：从描述提取伤害（先用默认值，后续可以完善解析）
            int baseDamage = GetDamageFromDescription(card.description);
            totalDamage = baseDamage + _playerStrength.Strength;
            _enemyHP.TakeDamage(totalDamage);
            Debug.Log($"打出 [{card.elementType}] {card.cardName}，消耗 {card.cost} 能量");
            Debug.Log($"造成 {totalDamage} 点伤害（基础{baseDamage} + 力量{_playerStrength.Strength}）！");
        }
        else if (card.HasCardType(CardType.Defense))
        {
            // 防御牌：获得护盾
            int blockAmount = GetBlockFromDescription(card.description);
            _playerBlock.AddBlock(blockAmount);
            Debug.Log($"打出 [{card.elementType}] {card.cardName}，消耗 {card.cost} 能量");
            Debug.Log($"获得 {blockAmount} 点护盾！");
        }
        else
        {
            // 技能牌或其他
            Debug.Log($"打出 [{card.elementType}] {card.cardName}，消耗 {card.cost} 能量");
            Debug.Log($"效果: {card.description}");
        }

        Debug.Log($"敌人剩余血量: {_enemyHP.CurrentHP}");
        Debug.Log($"玩家护盾: {_playerBlock.CurrentBlock}");

        // 卡牌进入弃牌堆或消耗区
        if (card.exhaust)
        {
            Debug.Log($"{card.cardName} 被消耗！");
        }
        else
        {
            discardPile.Add(card);
        }
        hand.RemoveAt(handIndex);

        if (_enemyHP.CurrentHP <= 0)
        {
            Debug.Log("🎉 胜利！敌人已被击败！");
        }
    }

    // ===== 从描述中提取伤害值（简单实现） =====
    private int GetDamageFromDescription(string description)
    {
        // 从描述中提取数字，例如 "造成9点伤害" -> 9
        string[] parts = description.Split(new char[] { ' ', '点' });
        foreach (string part in parts)
        {
            if (int.TryParse(part, out int value))
            {
                return value;
            }
        }
        return 6; // 默认伤害
    }

    // ===== 从描述中提取护盾值 =====
    private int GetBlockFromDescription(string description)
    {
        string[] parts = description.Split(new char[] { ' ', '点' });
        foreach (string part in parts)
        {
            if (int.TryParse(part, out int value))
            {
                return value;
            }
        }
        return 5; // 默认护盾
    }

    public void EndPlayerTurn()
    {
        Debug.Log("===== 玩家回合结束 =====");

        // 手牌全部进入弃牌堆
        discardPile.AddRange(hand);
        hand.Clear();
        Debug.Log($"手牌 {hand.Count} 张进入弃牌堆");

        // 敌人行动
        EnemyAI enemyAI = enemyObject.GetComponent<EnemyAI>();
        if (enemyAI != null)
        {
            enemyAI.ExecuteTurn();
        }
        else
        {
            int enemyDamage = 4;
            _playerHP.TakeDamage(enemyDamage);
            Debug.Log($"敌人造成 {enemyDamage} 点伤害！");
        }

        _playerBlock.ResetBlock();

        if (_playerHP.CurrentHP <= 0)
        {
            Debug.Log("💀 失败！玩家已死亡！");
            return;
        }

        if (_enemyHP.CurrentHP <= 0)
        {
            Debug.Log("🎉 胜利！敌人已被击败！");
            return;
        }

        StartNewTurn();
        Debug.Log($"===== 新回合开始 =====");
        Debug.Log($"能量: {_playerEnergy.CurrentEnergy}，手牌: {hand.Count} 张");
        Debug.Log("按 1 打出第一张手牌，按 2 结束回合");
    }
}