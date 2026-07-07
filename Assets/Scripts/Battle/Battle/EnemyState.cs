using ElementalSpire.Cards;
using UnityEngine;

/// <summary>
/// 敌人状态 - 中毒层数和元素附着互相独立。
/// </summary>
public class EnemyState : MonoBehaviour
{
    [Header("中毒")]
    [SerializeField] private int poisonStacks = 0;

    [Header("元素附着")]
    [SerializeField] private ElementType elementAttachment = ElementType.None;
    [SerializeField] private bool deepPoison = false;

    public int PoisonStacks => poisonStacks;
    public ElementType ElementAttachment => elementAttachment;
    public bool DeepPoison => deepPoison;

    public void AddPoisonStacks(int amount)
    {
        if (amount > 0)
            poisonStacks += amount;
    }

    public int TriggerPoisonTick()
    {
        if (poisonStacks <= 0) return 0;

        int damage = poisonStacks;
        poisonStacks = Mathf.Max(0, poisonStacks - 1);
        return damage;
    }

    public void RemovePoisonStacks(int amount)
    {
        if (amount > 0)
            poisonStacks = Mathf.Max(0, poisonStacks - amount);
    }

    public void SetPoisonStacks(int amount)
    {
        poisonStacks = Mathf.Max(0, amount);
    }

    public void ResetPoison()
    {
        poisonStacks = 0;
    }

    public void SetElementAttachment(ElementType elementType)
    {
        elementAttachment = IsAttachableElement(elementType) ? elementType : ElementType.None;
    }

    public void ClearElementAttachment()
    {
        elementAttachment = ElementType.None;
    }

    public void ApplyDeepPoison()
    {
        deepPoison = true;
    }

    public bool TryConsumeDeepPoison()
    {
        if (!deepPoison) return false;

        deepPoison = false;
        return true;
    }

    public void ResetCombatState()
    {
        poisonStacks = 0;
        elementAttachment = ElementType.None;
        deepPoison = false;
    }

    private bool IsAttachableElement(ElementType elementType)
    {
        return elementType == ElementType.Fire
            || elementType == ElementType.Poison
            || elementType == ElementType.Water;
    }
}
