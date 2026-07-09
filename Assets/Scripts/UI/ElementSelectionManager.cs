using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ElementSelectionManager : MonoBehaviour
{
    // 在Inspector中拖拽引用四个按钮
    public Button fireButton;
    public Button waterButton;
    public Button poisonButton;
    public Button continueButton;

    [Tooltip("选中时的按钮颜色（视觉反馈）")]
    public Color selectedColor = Color.green;
    public Color normalColor = Color.white;

    private List<ElementType> selectedElements = new List<ElementType>();
    private Dictionary<ElementType, Button> buttonMap;

    public enum ElementType { Fire, Water, Poison }

    void Start()
    {
        // 建立映射
        buttonMap = new Dictionary<ElementType, Button>
        {
            { ElementType.Fire, fireButton },
            { ElementType.Water, waterButton },
            { ElementType.Poison, poisonButton }
        };

        // 初始隐藏继续按钮
        continueButton.gameObject.SetActive(false);

        // 所有按钮恢复默认颜色
        foreach (var btn in buttonMap.Values)
            SetButtonNormal(btn);
    }

    // 由属性按钮脚本调用
    public void ToggleElement(ElementType type)
    {
        if (selectedElements.Contains(type))
        {
            // 取消选中
            selectedElements.Remove(type);
            SetButtonNormal(buttonMap[type]);
        }
        else
        {
            // 已选满两个则不允许再选
            if (selectedElements.Count >= 2) return;
            selectedElements.Add(type);
            SetButtonSelected(buttonMap[type]);
        }
        UpdateUI();
    }

    // 继续按钮点击
    public void Continue()
    {
        Debug.Log($"已选元素：{string.Join(", ", selectedElements)}");
        // 此处可执行场景切换等操作
    }

    // ----- 私有辅助方法 -----
    private void SetButtonSelected(Button btn)
    {
        var colors = btn.colors;
        colors.normalColor = selectedColor;
        btn.colors = colors;
    }

    private void SetButtonNormal(Button btn)
    {
        var colors = btn.colors;
        colors.normalColor = normalColor;
        btn.colors = colors;
    }

    private void UpdateUI()
    {
        bool isFull = selectedElements.Count >= 2;

        // 更新可交互性：若已选两个，未被选中的按钮不可交互（已选中的可点击取消）
        foreach (var kvp in buttonMap)
        {
            bool isSelected = selectedElements.Contains(kvp.Key);
            kvp.Value.interactable = !isFull || isSelected;
        }

        // 显示/隐藏继续按钮
        continueButton.gameObject.SetActive(isFull);
    }
}