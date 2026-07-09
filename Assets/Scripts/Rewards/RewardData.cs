using System.Collections;
using System.Collections.Generic;
using System;
/// <summary>
/// 通用奖励数据结构，用于各模块间传递奖励信息
/// </summary>
[Serializable]
public class RewardData
{
    public string[] cardIds;  // 获得的卡牌ID数组，与卡组约定ID对应格式
    public int healAmount;    // 恢复生命值数量
}