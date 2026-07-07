using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RewardManager : MonoBehaviour
{
    // 全局单例，全项目通过 RewardManager.Instance 访问
    public static RewardManager Instance;

    private void Awake()
    {
        // 单例校验：全局只保留一个实例，避免重复创建报错
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
    /// 统一奖励发放入口，传入奖励数据自动处理所有类型
    /// </summary>
    public void GrantReward(RewardData reward)
    {
        if (reward == null) return;


        // 发放卡牌奖励
        if (reward.cardIds != null && reward.cardIds.Length > 0)
        {
            AddCards(reward.cardIds);
        }

        // 发放生命回复奖励
        if (reward.healAmount > 0)
        {
            HealPlayer(reward.healAmount);
        }

        // 后续扩展：触发奖励UI弹窗、播放获得奖励音效
    }


    /// <summary>
    /// 向玩家背包添加多张卡牌
    /// </summary>
    public void AddCards(string[] cardIds)
    {
        foreach (string cardId in cardIds)
        {
            // 避免重复添加同一张卡牌
            if (!GameManager.Instance.playerCardBag.Contains(cardId))
            {
                GameManager.Instance.playerCardBag.Add(cardId);
            }
        }
        Debug.Log($"获得卡牌：{string.Join("、", cardIds)}");
    }

    /// <summary>
    /// 回复玩家生命值，不超过上限
    /// </summary>
    public void HealPlayer(int amount)
    {
        int currentHp = GameManager.Instance.playerHp;
        int maxHp = GameManager.Instance.playerMaxHp;
        // 回血后不超过最大生命值
        currentHp = Mathf.Min(currentHp + amount, maxHp);
        GameManager.Instance.playerHp = currentHp;
        Debug.Log($"回复生命 +{amount}，当前生命：{currentHp}/{maxHp}");
    }
}