using UnityEngine;

public class playerStrength : MonoBehaviour
{
    public int Strength = 0;

    public void AddStrength(int amount)
    {
        Strength += amount;
        Debug.Log($"力量 +{amount}，当前力量: {Strength}");
    }
}