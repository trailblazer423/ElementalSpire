using ElementalSpire.Cards;
using UnityEngine;

/// <summary>
/// 敌人状态 - 管理中毒、元素附着、力量、虚弱等状态。
/// </summary>
public class EnemyState : MonoBehaviour
{
    [Header("中毒")]
    [SerializeField] private int poisonStacks = 0;

    [Header("力量/虚弱")]
    [SerializeField] private int power = 0;
    [SerializeField] private int weakness = 0;

    [Header("元素附着")]
    [SerializeField] private ElementType elementAttachment = ElementType.None;
    [SerializeField] private bool deepPoison = false;

    public int PoisonStacks => poisonStacks;
    public int Power => power;
    public int Weakness => weakness;
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

    public void AddPower(int amount)
    {
        if (amount > 0)
            power += amount;
    }

    public void AddWeakness(int amount)
    {
        if (amount > 0)
            weakness += amount;
    }

    public void ResetPower()
    {
        power = 0;
    }

    public void ResetWeakness()
    {
        weakness = 0;
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
        power = 0;
        weakness = 0;
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
