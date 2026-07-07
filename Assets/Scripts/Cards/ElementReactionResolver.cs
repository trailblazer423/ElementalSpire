using ElementalSpire.Cards;
using UnityEngine;

public sealed class ElementReactionResult
{
    public bool Triggered { get; set; }
    public string ReactionName { get; set; } = string.Empty;
    public float DamageMultiplier { get; set; } = 1f;
    public int PoisonMultiplier { get; set; } = 1;
    public ElementType PreviousElement { get; set; } = ElementType.None;
    public ElementType NewElement { get; set; } = ElementType.None;
}

/// <summary>
/// 处理元素附着和元素反应。中毒层数不会被当作毒元素附着。
/// </summary>
public sealed class ElementReactionResolver
{
    private readonly BattleManager battleManager;
    private readonly PlayerState playerState;

    public ElementReactionResolver(BattleManager battleManager, PlayerState playerState)
    {
        this.battleManager = battleManager;
        this.playerState = playerState;
    }

    public ElementReactionResult ResolveElementAttachment(ElementType newElement, string source)
    {
        var result = new ElementReactionResult
        {
            NewElement = newElement
        };

        if (!CardDeckLibrary.IsMainElement(newElement))
            return result;

        EnemyState enemyState = battleManager?.EnemyState;
        if (enemyState == null)
            return result;

        ElementType previousElement = enemyState.ElementAttachment;
        result.PreviousElement = previousElement;

        if (previousElement == ElementType.None)
        {
            enemyState.SetElementAttachment(newElement);
            battleManager?.LogBattleEvent($"{source}：敌人附着{GetElementName(newElement)}元素。");
            return result;
        }

        if (previousElement == newElement)
        {
            battleManager?.LogBattleEvent($"{source}：敌人已附着{GetElementName(newElement)}元素。");
            return result;
        }

        ResolveReaction(previousElement, newElement, enemyState, result);

        if (!result.Triggered)
        {
            enemyState.SetElementAttachment(newElement);
            battleManager?.LogBattleEvent($"{source}：敌人附着改为{GetElementName(newElement)}元素。");
            return result;
        }

        playerState?.MarkReactionThisTurn();
        enemyState.ClearElementAttachment();
        battleManager?.LogBattleEvent($"元素反应：{GetElementName(previousElement)} + {GetElementName(newElement)} 触发 {result.ReactionName}。");
        return result;
    }

    public void ClearAttachmentAtTurnStart()
    {
        EnemyState enemyState = battleManager?.EnemyState;
        if (enemyState == null || enemyState.ElementAttachment == ElementType.None)
            return;

        string elementName = GetElementName(enemyState.ElementAttachment);
        enemyState.ClearElementAttachment();
        battleManager?.LogBattleEvent($"新回合开始：{elementName}元素附着消失，中毒层数保留。");
    }

    private void ResolveReaction(ElementType previousElement, ElementType newElement, EnemyState enemyState, ElementReactionResult result)
    {
        if ((previousElement == ElementType.Fire && newElement == ElementType.Water)
            || (previousElement == ElementType.Water && newElement == ElementType.Fire))
        {
            result.Triggered = true;
            result.ReactionName = "蒸发";
            result.DamageMultiplier = 1.5f;
            return;
        }

        if (previousElement == ElementType.Fire && newElement == ElementType.Poison)
        {
            result.Triggered = true;
            result.ReactionName = "毒性加深";
            result.PoisonMultiplier = 2;
            return;
        }

        if (previousElement == ElementType.Poison && newElement == ElementType.Fire)
        {
            result.Triggered = true;
            result.ReactionName = "毒性爆发";
            TriggerToxicBurst(enemyState);
            return;
        }

        if ((previousElement == ElementType.Poison && newElement == ElementType.Water)
            || (previousElement == ElementType.Water && newElement == ElementType.Poison))
        {
            result.Triggered = true;
            result.ReactionName = "深度中毒";
            enemyState.ApplyDeepPoison();
        }
    }

    private void TriggerToxicBurst(EnemyState enemyState)
    {
        if (enemyState.PoisonStacks <= 0)
        {
            battleManager?.LogBattleEvent("毒性爆发：敌人没有中毒层数。");
            return;
        }

        int poisonDamage = enemyState.PoisonStacks;
        battleManager?.DealDamageToEnemy(poisonDamage);
        enemyState.SetPoisonStacks(Mathf.FloorToInt(enemyState.PoisonStacks * 0.8f));
        battleManager?.LogBattleEvent($"毒性爆发造成 {poisonDamage} 点伤害，中毒降低到 {enemyState.PoisonStacks}。");
    }

    private string GetElementName(ElementType elementType)
    {
        switch (elementType)
        {
            case ElementType.Fire: return "火";
            case ElementType.Poison: return "毒";
            case ElementType.Water: return "水";
            default: return "无";
        }
    }
}
