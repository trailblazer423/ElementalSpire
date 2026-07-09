using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RewardManager : MonoBehaviour
{
    // 全局单例，全局可通过 RewardManager.Instance 访问
    public static RewardManager Instance;

    private void Awake()
    {
        // 单例校验：全局只有一个实例，避免重复创建
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// 统一发放奖励入口，传入奖励数据自动分发处理
    /// </summary>
    public void GrantReward(RewardData reward)
    {
        if (reward == null) return;


        // 发放卡牌奖励
        if (reward.cardIds != null && reward.cardIds.Length > 0)
        {
            AddCards(reward.cardIds);
        }

        // 执行生命恢复
        if (reward.healAmount > 0)
        {
            HealPlayer(reward.healAmount);
        }

        // 后续可扩展：弹出UI面板，展示获得奖励效果
    }


    /// <summary>
    /// 向玩家背包添加多张卡牌
    /// </summary>
    public void AddCards(string[] cardIds)
    {
        foreach (string cardId in cardIds)
        {
            // 避免重复添加同一张卡
            if (!GameManager.Instance.playerCardBag.Contains(cardId))
            {
                GameManager.Instance.playerCardBag.Add(cardId);
            }
        }
        Debug.Log($"获得卡牌：{string.Join("，", cardIds)}");
    }

    /// <summary>
    /// 恢复玩家生命值，受最大生命限制
    /// </summary>
    public void HealPlayer(int amount)
    {
        int currentHp = GameManager.Instance.playerHp;
        int maxHp = GameManager.Instance.playerMaxHp;
        // 回血后不超过最大生命值
        currentHp = Mathf.Min(currentHp + amount, maxHp);
        GameManager.Instance.playerHp = currentHp;
        Debug.Log($"恢复生命 +{amount}，当前血量：{currentHp}/{maxHp}");
    }
}