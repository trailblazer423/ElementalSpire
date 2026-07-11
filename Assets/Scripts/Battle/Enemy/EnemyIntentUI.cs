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

    public void SetController(EnemyController controller)
    {
        _controller = controller;
    }

    /// <summary>
    /// 获取当前意图显示文本和颜色，供 BattleUI 使用。
    /// </summary>
    public void GetIntentDisplay(out string text, out Color color)
    {
        EnsureControllerReference();

        if (_controller == null)
        {
            text = "";
            color = Color.gray;
            return;
        }

        EnemyIntent intent = _controller.GetCurrentIntent();
        int value = _controller.GetIntentValue();
        string description = _controller.intentDescription;

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
                text = string.IsNullOrEmpty(description) ? "⚡ 蓄力" : $"⚡ {description}";
                color = new Color(1f, 0.8f, 0.1f);
                break;
            case EnemyIntent.Heal:
                text = $"💚 回复 {value}";
                color = new Color(0.2f, 0.9f, 0.3f);
                break;
            case EnemyIntent.Buff:
                text = string.IsNullOrEmpty(description) ? $"⬆ 强化 {value}" : $"⬆ {description}";
                color = new Color(0.7f, 0.3f, 0.9f);
                break;
            case EnemyIntent.Debuff:
                text = string.IsNullOrEmpty(description) ? $"▼ 减益 {value}" : $"▼ {description}";
                color = new Color(1f, 0.6f, 0.0f);
                break;
            default:
                text = "";
                color = Color.gray;
                break;
        }
    }

    private void EnsureControllerReference()
    {
        if (_controller != null && _controller.enabled && _controller.enemyData != null)
            return;

        foreach (EnemyController candidate in GetComponents<EnemyController>())
        {
            if (!candidate.enabled || candidate.enemyData == null) continue;

            _controller = candidate;
            return;
        }
    }
}
