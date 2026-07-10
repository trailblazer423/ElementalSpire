using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement; // 场景管理命名空间

/// <summary>
/// 音乐设置面板控制器（挂载到面板根物体上）
/// </summary>
public class UIMusicPanelController : MonoBehaviour
{
    [Header("按钮引用")]
    [SerializeField] private Button muteBtn;   // 静音切换按钮
    [SerializeField] private Button nextBtn;   // 下一首按钮
    [SerializeField] private Button closeBtn;  // 关闭面板按钮

    [Header("图标切换（可选）")]
    [SerializeField] private Image muteIcon;   // 静音状态图标（可选）
    [SerializeField] private Sprite muteOnSprite;   // 非静音图标
    [SerializeField] private Sprite muteOffSprite;  // 静音图标

    private void Start()
    {
        // 每次场景加载/面板激活时重新绑定，确保引用最新
        BindButtons();

        // 初始化静音图标状态
        UpdateMuteIcon();

        // ⭐ 新增：监听场景切换事件
        SceneManager.activeSceneChanged += OnSceneChanged;
    }

    private void OnDestroy()
    {
        // ⭐ 新增：移除事件监听，防止内存泄漏
        SceneManager.activeSceneChanged -= OnSceneChanged;
    }

    private void OnEnable()
    {
        // 每次面板显示时更新图标状态（可能在其他地方修改了静音）
        UpdateMuteIcon();
    }

    /// <summary>
    /// ⭐ 新增：场景切换时自动关闭面板
    /// </summary>
    private void OnSceneChanged(Scene oldScene, Scene newScene)
    {
        // 如果面板处于激活状态，则关闭它
        if (gameObject.activeSelf)
        {
            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 为按钮动态添加监听器
    /// </summary>
    private void BindButtons()
    {
        // 先移除所有监听，防止重复绑定（如果多次调用）
        muteBtn.onClick.RemoveAllListeners();
        nextBtn.onClick.RemoveAllListeners();
        closeBtn.onClick.RemoveAllListeners();

        // 绑定功能
        muteBtn.onClick.AddListener(() =>
        {
            // 切换静音
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.ToggleMute();
                UpdateMuteIcon(); // 更新图标
            }
        });

        nextBtn.onClick.AddListener(() =>
        {
            // 切换下一首（随机播放下一首）
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayNextRandom();
            }
        });

        closeBtn.onClick.AddListener(() =>
        {
            // 关闭面板：只隐藏，不影响音乐播放
            gameObject.SetActive(false);
        });
    }

    /// <summary>
    /// 更新静音图标的显示（根据当前静音状态）
    /// </summary>
    private void UpdateMuteIcon()
    {
        if (muteIcon != null && muteOnSprite != null && muteOffSprite != null)
        {
            bool isMuted = AudioManager.Instance != null && AudioManager.Instance.IsMuted;
            muteIcon.sprite = isMuted ? muteOffSprite : muteOnSprite;
        }
    }

    // 如果面板需要从外部打开（例如主菜单的“音乐设置”按钮），
    // 可以提供一个公开方法供调用：
    public void OpenPanel()
    {
        gameObject.SetActive(true);
        UpdateMuteIcon(); // 打开时更新状态
    }
}