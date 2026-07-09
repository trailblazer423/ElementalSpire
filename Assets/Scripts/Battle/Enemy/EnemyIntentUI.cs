using UnityEngine;

/// <summary>
/// 敌人意图 UI - 挂在 Enemy 对象上，供 BattleUI 读取意图数据。
/// 实际 UI 显示在 BattleUI 的屏幕空间 Canvas 上。
/// 需要 EnemyController 组件提供意图数据。
/// </summary>
[RequireComponent(typeof(EnemyController))]
public class EnemyIntentUI : MonoBehaviour
{
    private EnemyController _controller;

    void Awake()
    {
        _controller = GetComponent<EnemyController>();
    }

    /// <summary>
    /// 获取当前意图显示文本和颜色，供 BattleUI 使用。
    /// </summary>
    public void GetIntentDisplay(out string text, out Color color)
    {
        if (_controller == null)
        {
            text = "";
            color = Color.gray;
            return;
        }

        EnemyIntent intent = _controller.GetCurrentIntent();
        int value = _controller.GetIntentValue();

        switch (intent)
        {
            case EnemyIntent.Attack:
                text = $"⚔ 攻击 {value}";
                color = new Color(0.9f, 0.2f, 0.1f);
                break;
            case EnemyIntent.Defend:
                text = $"🛡 防御 {value}";
                color = new Color(0.2f, 0.7f, 0.9f);
                break;
            case EnemyIntent.Charge:
                text = "⚡ 蓄力";
                color = new Color(1f, 0.8f, 0.1f);
                break;
            case EnemyIntent.Heal:
                text = $"💚 回复 {value}";
                color = new Color(0.2f, 0.9f, 0.3f);
                break;
            case EnemyIntent.Buff:
                text = "⬆ 强化";
                color = new Color(0.7f, 0.3f, 0.9f);
                break;
            default:
                text = "";
                color = Color.gray;
                break;
        }
    }
}
