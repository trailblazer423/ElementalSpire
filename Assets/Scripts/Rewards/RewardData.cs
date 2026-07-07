using System.Collections;
using System.Collections.Generic;
using System;
/// <summary>
/// 通用奖励数据结构，用于在各模块间传递奖励配置
/// </summary>
[Serializable]
public class RewardData
{
    public string[] cardIds;  // 获得的卡牌ID数组，与卡牌组约定ID对应规则
    public int healAmount;    // 回复的生命值数量
}